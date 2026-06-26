using UnityEngine;

// Помечает врага как "пуловый": при смерти он не уничтожается, а возвращается в пул.
// Добавляется автоматически менеджером EnemyPool при первом создании экземпляра,
// поэтому вручную вешать на префаб не обязательно (но можно).
[RequireComponent(typeof(Health))]
public class PooledEnemy : MonoBehaviour
{
    // Префаб, из которого создан этот экземпляр (ключ для возврата в нужную очередь).
    public GameObject SourcePrefab { get; private set; }

    [Tooltip("Задержка перед возвратом в пул — под анимацию смерти. 0 = мгновенно.")]
    public float returnDelay = 0f;

    private EnemyPool pool;
    private Health health;
    private bool subscribed;

    // Вызывается пулом при создании экземпляра.
    public void Init(EnemyPool owner, GameObject prefab)
    {
        pool = owner;
        SourcePrefab = prefab;
    }

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null && !subscribed)
        {
            health.destroyOnDeath = false;            // пулим, а не уничтожаем
            health.onDeath.AddListener(OnEnemyDeath);
            subscribed = true;
        }
    }

    void OnEnemyDeath()
    {
        CancelInvoke(nameof(ReturnNow));
        if (returnDelay > 0f) Invoke(nameof(ReturnNow), returnDelay);
        else ReturnNow();
    }

    void ReturnNow()
    {
        if (pool != null) pool.Return(this);
        else gameObject.SetActive(false);   // подстраховка, если пул потерян
    }

    void OnDestroy()
    {
        if (health != null) health.onDeath.RemoveListener(OnEnemyDeath);
    }
}
