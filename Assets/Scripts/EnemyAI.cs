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

    [Header("Отскок (knockback)")]
    [Tooltip("Как быстро гаснет отскок (больше = короче толчок).")]
    public float knockbackDecay = 40f;

    [Header("Поворот модели")]
    [Tooltip("Коррекция разворота, если модель смотрит не вперёд. Обычно 0, -90 или 90/180. Подбери, чтобы враг смотрел на игрока.")]
    public float modelYawOffset = 0f;

    [Header("Анимация")]
    [Tooltip("Множитель скорости анимации (1 = норма). Для быстрых врагов поставь > 1.")]
    public float animationSpeed = 1f;

    private Animator animator;

    private Transform player;
    private float lastAttackTime;
    private CharacterController cc;
    private Vector3 knockbackVelocity;

    // Вызывается ударом игрока: толкает врага в направлении direction с силой force.
    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0f;
        knockbackVelocity = direction.normalized * force;
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();

        // ищем игрока по тегу "Player"
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    // сбрасываем состояние при каждом респавне из пула
    void OnEnable()
    {
        knockbackVelocity = Vector3.zero;

        // применяем скорость анимации (для каждого типа врага своя)
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.speed = animationSpeed;
    }

    void Update()
    {
        if (player == null) return;

        // отскок поверх обычного движения (и его затухание)
        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 kb = knockbackVelocity * Time.deltaTime;
            if (cc != null) cc.Move(kb);
            else transform.position += kb;
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }

        // направление к игроку только по горизонтали
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // повернуться лицом к игроку (с коррекцией, если модель смотрит вбок)
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(toPlayer) * Quaternion.Euler(0f, modelYawOffset, 0f);

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
                if (hp != null && !hp.isInvulnerable)
                {
                    hp.TakeDamage(damage);
                    // урон по игроку трясёт сильнее, чем наши удары по врагам
                    if (CameraShake.Instance != null)
                        CameraShake.Instance.AddTrauma(0.45f);
                }
            }
        }
    }
}
