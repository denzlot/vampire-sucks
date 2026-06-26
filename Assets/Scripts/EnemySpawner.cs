using System.Collections.Generic;
using UnityEngine;

// Спавнит врагов кольцом вокруг игрока через равные промежутки.
// Вешается на пустой объект Spawner в сцене.
public class EnemySpawner : MonoBehaviour
{
    // Один тип врага и его "вес" (относительная частота появления).
    [System.Serializable]
    public class EnemyType
    {
        [Tooltip("Префаб врага.")]
        public GameObject prefab;
        [Tooltip("Вес: чем больше, тем чаще спавнится этот тип относительно других.")]
        public float weight = 1f;
    }

    [Header("Что спавнить")]
    [Tooltip("Список типов врагов со своими весами. Если пусто — используется одиночный Enemy Prefab ниже.")]
    public List<EnemyType> enemyTypes = new();

    [Tooltip("Запасной одиночный префаб (используется, если список Enemy Types пуст).")]
    public GameObject enemyPrefab;

    [Header("Параметры спавна")]
    public float spawnInterval = 2f;   // как часто появляются
    [Tooltip("Ближняя граница кольца спавна (не ближе этого к игроку).")]
    public float minRadius = 12f;
    [Tooltip("Дальняя граница кольца спавна.")]
    public float maxRadius = 18f;
    public int maxEnemies = 50;        // потолок одновременных врагов
    [Tooltip("Высота точки спавна над игроком (чтобы не уходить в пол).")]
    public float spawnHeight = 1f;

    [Tooltip("Если пусто — игрок найдётся по тегу Player.")]
    public Transform player;

    private float timer;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
            Debug.LogWarning("[EnemySpawner] Игрок не найден! Поставь тег 'Player' на объект игрока, иначе враги спавнятся в точке спавнера.");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            if (CountEnemies() < maxEnemies)
                Spawn();
        }
    }

    int CountEnemies()
    {
        // считаем по тегу "Enemy" (не забудь назначить его врагу/префабу)
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    // Выбирает префаб: взвешенный случайный из enemyTypes, иначе запасной enemyPrefab.
    GameObject PickPrefab()
    {
        // суммарный вес валидных типов
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

        return enemyPrefab;   // запасной вариант
    }

    void Spawn()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Vector3 center = player != null ? player.position : transform.position;

        // защита от нуля: радиус всегда не меньше 1 метра
        float safeMin = Mathf.Max(1f, minRadius);
        float safeMax = Mathf.Max(safeMin, maxRadius);

        // случайное направление по кругу + случайный радиус в кольце [min..max]
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(safeMin, safeMax);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, spawnHeight, Mathf.Sin(angle) * radius);

        // берём врага из пула (создаст менеджер, если его ещё нет)
        EnemyPool.EnsureExists();
        EnemyPool.Instance.Get(prefab, center + offset, Quaternion.identity);
    }
}
