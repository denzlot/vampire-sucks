using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // новый Input System

// Ближняя атака по ЛКМ: короткий луч вперёд из камеры.
// Вешается на объект Player.
public class MeleeAttack : MonoBehaviour
{
    [Header("Параметры удара")]
    public float range = 2f;        // дистанция удара в метрах
    public int damage = 105;         // урон за удар
    public float cooldown = 0.4f;   // пауза между ударами

    [Header("Ссылки")]
    [Tooltip("Откуда бьём. Обычно Main Camera. Если пусто — найдётся автоматически.")]
    public Transform cameraTransform;
    [Tooltip("Куб-рука для простого замаха (необязательно).")]
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
        if (handTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(SwingHand());
        }

        // луч вперёд из центра камеры
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Health hp = hit.collider.GetComponent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
                TriggerHitFeedback(hit, hp);
            }
        }
    }

    // весь "сок" попадания: хитстоп + тряска + вспышка + отскок врага
    void TriggerHitFeedback(RaycastHit hit, Health hp)
    {
        if (hitStopDuration > 0f)
            HitStop.Instance.Freeze(hitStopDuration);

        if (CameraShake.Instance != null)
            CameraShake.Instance.AddTrauma(hitTrauma);

        if (hitEffectPrefab != null)
            Destroy(Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal)), 2f);

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        // оттолкнуть врага от игрока
        EnemyAI enemy = hp.GetComponent<EnemyAI>();
        if (enemy != null)
            enemy.ApplyKnockback(cameraTransform.forward, knockbackForce);
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
