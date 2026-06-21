using UnityEngine;

// Тряска камеры. Вешается на Main Camera.
// Вызов: CameraShake.Instance.Shake(длительность, сила).
// Язык интенсивности: урон по игроку трясёт сильнее, чем попадание по врагу.
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 baseLocalPos;
    private float duration;
    private float magnitude;
    private float timer;

    void Awake()
    {
        Instance = this;
        baseLocalPos = transform.localPosition;
    }

    public void Shake(float dur, float mag)
    {
        // берём максимум, чтобы новый удар не "срезал" текущую тряску
        duration = Mathf.Max(duration, dur);
        magnitude = Mathf.Max(magnitude, mag);
        timer = duration;
    }

    void LateUpdate()
    {
        // unscaledDeltaTime — чтобы тряска работала даже во время хитстопа (timeScale = 0)
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            float pct = duration > 0f ? timer / duration : 0f;       // затухание к нулю
            transform.localPosition = baseLocalPos + Random.insideUnitSphere * (magnitude * pct);
        }
        else
        {
            transform.localPosition = baseLocalPos;
            magnitude = 0f;
            duration = 0f;
        }
    }
}
