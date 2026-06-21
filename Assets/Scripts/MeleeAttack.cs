using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // новый Input System

// Ближняя атака по ЛКМ: короткий луч вперёд из камеры.
// Вешается на объект Player.
public class MeleeAttack : MonoBehaviour
{
    [Header("Параметры удара")]
    public float range = 2f;        // дистанция удара в метрах
    public int damage = 25;         // урон за удар
    public float cooldown = 0.4f;   // пауза между ударами

    [Header("Ссылки")]
    [Tooltip("Откуда бьём. Обычно Main Camera. Если пусто — найдётся автоматически.")]
    public Transform cameraTransform;
    [Tooltip("Куб-рука для простого замаха (необязательно).")]
    public Transform handTransform;

    private float lastAttackTime = -999f;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
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
                hp.TakeDamage(damage);
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
