using System.Collections.Generic;
using UnityEngine;

// "Director"-спавнер в духе Vampire Survivors / Megabonk:
//  • держит на поле ЦЕЛЕВОЕ число живых врагов (target), доспавнивая до него;
//  • цель и интервал спавна растут со временем (кривые сложности);
//  • враги появляются кольцом вокруг игрока (вне поля зрения);
//  • далёкие враги рециклятся (логика в EnemyAI.despawnRadius);
//  • опциональные map-events — всплески врагов в заданные секунды.
// Вешается на пустой объект Spawner в сцене.
public class EnemySpawner : MonoBehaviour
{
    // ── Типы врагов ───────────────────────────────────────────────────────────
    [System.Serializable]
    public class EnemyType
    {
        public GameObject prefab;
        [Tooltip("Вес: чем больше, тем чаще спавнится этот тип.")]
        public float weight = 1f;
    }

    [Header("Что спавнить")]
    public List<EnemyType> enemyTypes = new();
    [Tooltip("Запасной префаб, если список типов пуст.")]
    public GameObject enemyPrefab;

    // ── Кольцо спавна ─────────────────────────────────────────────────────────
    [Header("Кольцо спавна вокруг игрока")]
    public float minRadius = 14f;
    public float maxRadius = 20f;
    public float spawnHeight = 1f;
    [Tooltip("Если пусто — игрок найдётся по тегу Player.")]
    public Transform player;

    // ── Сложность по времени (как 'волны' в VS) ───────────────────────────────
    [Header("Сложность по времени")]
    [Tooltip("Целевое число ЖИВЫХ врагов в зависимости от времени (сек). Спавнер доспавнивает до этой цели.")]
    public AnimationCurve targetAliveOverTime = new AnimationCurve(
        new Keyframe(0f, 12f), new Keyframe(60f, 30f), new Keyframe(180f, 70f), new Keyframe(300f, 120f));
    [Tooltip("Интервал между попытками спавна (сек) в зависимости от времени.")]
    public AnimationCurve spawnIntervalOverTime = new AnimationCurve(
        new Keyframe(0f, 1.2f), new Keyframe(120f, 0.6f), new Keyframe(300f, 0.3f));
    [Tooltip("Жёсткий потолок живых врагов (защита FPS).")]
    public int hardCap = 150;
    [Tooltip("Сколько максимум врагов доспавнивается за одну попытку (чтобы наполнять плавно).")]
    public int maxPerWave = 8;

    // ── Map events (всплески) ─────────────────────────────────────────────────
    [System.Serializable]
    public class SpawnEvent
    {
        [Tooltip("На какой секунде сработает (один раз).")]
        public float time;
        [Tooltip("Кого спавнить. Если пусто — берётся обычный взвешенный выбор.")]
        public GameObject prefab;
        public int count = 12;
        [Tooltip("true — ровным кольцом окружить игрока; false — случайно по кольцу.")]
        public bool encircle = false;
        [HideInInspector] public bool fired;
    }

    [Header("Map events (необязательно)")]
    public List<SpawnEvent> events = new();

    // ── внутреннее ────────────────────────────────────────────────────────────
    private float elapsed;
    private float timer;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null)
            Debug.LogWarning("[EnemySpawner] Игрок не найден! Поставь тег 'Player' на игрока.");

        EnemyPool.EnsureExists();
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // обычный поток: поддерживаем целевое число живых
        float interval = Mathf.Max(0.05f, spawnIntervalOverTime.Evaluate(elapsed));
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            MaintainTarget();
        }

        // всплески
        HandleEvents();
    }

    // Доспавнить врагов до целевого числа (но не больше maxPerWave за раз).
    void MaintainTarget()
    {
        int target = Mathf.RoundToInt(targetAliveOverTime.Evaluate(elapsed));
        target = Mathf.Min(target, hardCap);

        int alive = EnemyPool.Instance != null ? EnemyPool.Instance.ActiveCount : 0;
        int deficit = target - alive;
        if (deficit <= 0) return;

        int toSpawn = Mathf.Min(deficit, maxPerWave);
        for (int i = 0; i < toSpawn; i++)
            SpawnOne(PickPrefab(), RandomRingOffset());
    }

    void HandleEvents()
    {
        foreach (var e in events)
        {
            if (e == null || e.fired || elapsed < e.time) continue;
            e.fired = true;

            int alive = EnemyPool.Instance != null ? EnemyPool.Instance.ActiveCount : 0;
            int canSpawn = Mathf.Max(0, hardCap - alive);
            int n = Mathf.Min(e.count, canSpawn);

            for (int i = 0; i < n; i++)
            {
                GameObject prefab = e.prefab != null ? e.prefab : PickPrefab();
                Vector3 offset = e.encircle ? RingOffsetAt((float)i / n) : RandomRingOffset();
                SpawnOne(prefab, offset);
            }
        }
    }

    // ── помощники ─────────────────────────────────────────────────────────────

    GameObject PickPrefab()
    {
        float total = 0f;
        foreach (var t in enemyTypes)
            if (t != null && t.prefab != null && t.weight > 0f) total += t.weight;

        if (total > 0f)
        {
            float r = Random.Range(0f, total);
            foreach (var t in enemyTypes)
            {
                if (t == null || t.prefab == null || t.weight <= 0f) continue;
                r -= t.weight;
                if (r <= 0f) return t.prefab;
            }
        }
        return enemyPrefab;
    }

    // случайная точка на кольце [minRadius..maxRadius]
    Vector3 RandomRingOffset()
    {
        float safeMin = Mathf.Max(1f, minRadius);
        float safeMax = Mathf.Max(safeMin, maxRadius);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(safeMin, safeMax);
        return new Vector3(Mathf.Cos(angle) * radius, spawnHeight, Mathf.Sin(angle) * radius);
    }

    // точка на кольце по доле круга t (0..1) — для ровного окружения
    Vector3 RingOffsetAt(float t)
    {
        float angle = t * Mathf.PI * 2f;
        float radius = Mathf.Max(1f, maxRadius);
        return new Vector3(Mathf.Cos(angle) * radius, spawnHeight, Mathf.Sin(angle) * radius);
    }

    void SpawnOne(GameObject prefab, Vector3 offset)
    {
        if (prefab == null || EnemyPool.Instance == null) return;
        Vector3 center = player != null ? player.position : transform.position;
        EnemyPool.Instance.Get(prefab, center + offset, Quaternion.identity);
    }
}
