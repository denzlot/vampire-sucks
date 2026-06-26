using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // новый Input System

// Ближняя атака по ЛКМ: короткий луч вперёд из камеры.
// Вешается на объект Player.
public class MeleeAttack : MonoBehaviour
{
    [Header("Параметры удара")]
    public float range = 2f;        // радиус удара в метрах
    public int damage = 105;         // урон за удар
    public float cooldown = 0.4f;   // пауза между ударами

    [Header("Урон по области")]
    [Tooltip("Полуугол сектора перед игроком (в градусах). 90 = бьёт по всей передней полусфере (180° дуга). 180 = по кругу.")]
    [Range(10f, 180f)]
    public float aoeAngle = 100f;
    [Tooltip("По каким слоям искать врагов. Оставь Everything, если не настраивал слои.")]
    public LayerMask hitMask = ~0;

    [Header("Ссылки")]
    [Tooltip("Откуда бьём. Обычно Main Camera. Если пусто — найдётся автоматически.")]
    public Transform cameraTransform;
    [Tooltip("Animator руки (Generic FBX). Главный способ для скелетных моделей.")]
    public Animator handAnimator;
    [Tooltip("Имя состояния (state) в Animator Controller с клипом замаха.")]
    public string swingStateName = "ClawSwipe";
    [Tooltip("Старый Legacy-компонент Animation (если используешь не Animator, а Animation).")]
    public Animation handAnimation;
    [Tooltip("Имя клипа замаха внутри handAnimation (для Legacy-варианта).")]
    public string swingClipName = "ClawSwipe";
    [Tooltip("Скорость проигрывания клипа замаха (1 = как есть, 2 = вдвое быстрее).")]
    public float swingSpeed = 1f;
    [Tooltip("Старый куб-рука для простого замаха. Фолбэк, если ничего не назначено.")]
    public Transform handTransform;

    [Header("Фидбек попадания")]
    [Tooltip("Длительность хитстопа (реальные сек). 0 = выключить.")]
    public float hitStopDuration = 0.05f;
    [Tooltip("Травма камеры при попадании по врагу [0..1]. Квадратируется внутри — 0.35 = ощутимый удар.")]
    [Range(0f, 1f)]
    public float hitTrauma = 0.2f;
    [Tooltip("Сила отскока врага.")]
    public float knockbackForce = 12f;
    [Tooltip("Префаб эффекта попадания (партикл/вспышка). Необязательно.")]
    public GameObject hitEffectPrefab;
    [Tooltip("Звук попадания (нужен AudioSource на игроке).")]
    public AudioClip hitSound;

    private float lastAttackTime = -999f;
    private AudioSource audioSource;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // страховка: если источник забыли добавить — создаём сами
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D-звук
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            Time.time >= lastAttackTime + cooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        // визуальный замах рукой
        PlaySwing();

        Vector3 origin = transform.position;                 // центр сектора — игрок
        Vector3 aim = cameraTransform.forward;               // куда смотрим
        aim.y = 0f;                                           // удар горизонтальный
        if (aim.sqrMagnitude < 0.001f) aim = transform.forward;
        aim.Normalize();

        // собираем всех в радиусе и бьём тех, кто в секторе перед игроком
        Collider[] cols = Physics.OverlapSphere(origin, range, hitMask, QueryTriggerInteraction.Ignore);
        HashSet<Health> alreadyHit = new HashSet<Health>();
        bool anyHit = false;

        foreach (Collider col in cols)
        {
            Health hp = col.GetComponentInParent<Health>();
            if (hp == null) continue;
            if (hp.gameObject == gameObject) continue;        // себя не бьём
            if (!alreadyHit.Add(hp)) continue;                // один враг — один раз за удар

            // фильтр по сектору: цель должна быть перед игроком в пределах aoeAngle
            Vector3 toTarget = hp.transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(aim, toTarget) > aoeAngle)
                continue;

            hp.TakeDamage(damage);
            ApplyHitToEnemy(hp, origin);
            anyHit = true;
        }

        // "сок" — один раз за удар, если хоть кого-то задели
        if (anyHit) TriggerGlobalFeedback();
    }

    // эффект и отскок — каждому задетому врагу
    void ApplyHitToEnemy(Health hp, Vector3 origin)
    {
        Vector3 dir = hp.transform.position - origin;        // толкаем от игрока
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        if (hitEffectPrefab != null)
            Destroy(Instantiate(hitEffectPrefab, hp.transform.position, Quaternion.LookRotation(dir.normalized)), 2f);

        EnemyAI enemy = hp.GetComponent<EnemyAI>();
        if (enemy != null)
            enemy.ApplyKnockback(dir, knockbackForce);
    }

    // общий фидбек удара: хитстоп + тряска камеры + звук (один раз за удар)
    void TriggerGlobalFeedback()
    {
        if (hitStopDuration > 0f && HitStop.Instance != null)
            HitStop.Instance.Freeze(hitStopDuration);

        if (CameraShake.Instance != null)
            CameraShake.Instance.AddTrauma(hitTrauma);

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);
    }

    // запуск замаха: Animator (приоритет) -> Legacy Animation -> старый куб
    void PlaySwing()
    {
        // 1) Animator (Generic FBX) — основной путь
        if (handAnimator != null && !string.IsNullOrEmpty(swingStateName))
        {
            handAnimator.speed = swingSpeed;
            handAnimator.Play(swingStateName, 0, 0f); // всегда с начала, даже при спаме ЛКМ
            return;
        }

        // 2) Legacy Animation
        if (handAnimation != null && !string.IsNullOrEmpty(swingClipName) &&
            handAnimation.GetClip(swingClipName) != null)
        {
            AnimationState st = handAnimation[swingClipName];
            st.wrapMode = WrapMode.Once;
            st.speed = swingSpeed;
            st.time = 0f;
            handAnimation.Play(swingClipName, PlayMode.StopAll);
            return;
        }

        // 3) старый куб-фолбэк
        if (handTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(SwingHand());
        }
    }

    // простой замах: рука дёргается вперёд и возвращается
    IEnumerator SwingHand()
    {
        Vector3 start = handTransform.localPosition;
        Vector3 punch = start + new Vector3(0f, 0f, 0.3f);

        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 8f; handTransform.localPosition = Vector3.Lerp(start, punch, t); yield return null; }
        t = 0f;
        while (t < 1f) { t += Time.deltaTime * 8f; handTransform.localPosition = Vector3.Lerp(punch, start, t); yield return null; }
        handTransform.localPosition = start;
    }
}
