using UnityEngine;

// Простой враг: идёт к игроку и бьёт в упор.
// Вешается на объект Enemy.
public class EnemyAI : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 2.5f;

    [Header("Атака")]
    public float attackRange = 1.5f;   // с какой дистанции бьёт
    public int damage = 10;
    public float attackCooldown = 1f;

    private Transform player;
    private float lastAttackTime;
    private CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        // ищем игрока по тегу "Player"
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        // направление к игроку только по горизонтали
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // повернуться лицом к игроку
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(toPlayer);

        if (distance > attackRange)
        {
            // идём к игроку
            Vector3 step = toPlayer.normalized * moveSpeed * Time.deltaTime;
            if (cc != null)
                cc.Move(step + Vector3.down * 5f * Time.deltaTime); // + лёгкая гравитация
            else
                transform.position += step;
        }
        else
        {
            // в упор — бьём с кулдауном
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                Health hp = player.GetComponent<Health>();
                if (hp != null) hp.TakeDamage(damage);
            }
        }
    }
}
