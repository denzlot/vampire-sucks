using System.Collections.Generic;
using UnityEngine;

// Пул объектов для врагов: вместо Instantiate/Destroy переиспользует экземпляры.
// Снижает нагрузку на GC и убирает фризы при массовом спавне.
// Создаётся автоматически (спавнер вызывает EnsureExists), отдельно ставить в сцену не нужно.
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    // отдельная очередь "спящих" экземпляров на каждый префаб
    private readonly Dictionary<GameObject, Queue<PooledEnemy>> pools = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Гарантирует, что менеджер пула существует в сцене.
    public static EnemyPool EnsureExists()
    {
        if (Instance == null)
            new GameObject("EnemyPool").AddComponent<EnemyPool>();
        return Instance;
    }

    // Достать врага из пула (или создать новый, если свободных нет).
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<PooledEnemy>();
            pools[prefab] = q;
        }

        PooledEnemy pe = null;

        // берём первый живой экземпляр из очереди (пропускаем уничтоженные)
        while (q.Count > 0)
        {
            var candidate = q.Dequeue();
            if (candidate != null) { pe = candidate; break; }
        }

        if (pe == null)
        {
            // свободных нет — создаём новый и помечаем как принадлежащий пулу
            GameObject go = Instantiate(prefab, position, rotation);
            pe = go.GetComponent<PooledEnemy>();
            if (pe == null) pe = go.AddComponent<PooledEnemy>();
            pe.Init(this, prefab);
        }
        else
        {
            pe.transform.SetPositionAndRotation(position, rotation);
        }

        pe.gameObject.SetActive(true);   // разбудит OnEnable у Health/EnemyAI -> сброс состояния
        return pe.gameObject;
    }

    // Вернуть врага в пул (деактивирует и кладёт обратно в очередь).
    public void Return(PooledEnemy pe)
    {
        if (pe == null) return;

        pe.gameObject.SetActive(false);

        if (!pools.TryGetValue(pe.SourcePrefab, out var q))
        {
            q = new Queue<PooledEnemy>();
            pools[pe.SourcePrefab] = q;
        }
        q.Enqueue(pe);
    }
}
