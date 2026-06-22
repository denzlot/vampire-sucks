using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CameraShake — профессиональная тряска камеры.
//
// Принцип работы ("Trauma" by Martin Jonasson & Petri Purho, GDC 2013):
//   • trauma — накапливаемая «травма» [0..1]. При каждом вызове AddTrauma()
//     она суммируется. С течением времени она убывает с настраиваемой скоростью.
//   • shake = trauma²   — квадратичная кривая: слабые удары почти не заметны,
//     а сильные дают мощный пик, после чего быстро спадают.
//   • Perlin Noise вместо Random — движение плавное и непрерывное.
//   • Добавлено угловое дрожание (roll) — самое «кинематографичное».
//
// Использование:
//   CameraShake.Instance.AddTrauma(0.6f);  // 0..1, можно вызывать несколько раз
// ─────────────────────────────────────────────────────────────────────────────
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Затухание")]
    [Tooltip("Скорость убывания травмы в единицах/сек. 1 = полностью за 1 сек.")]
    [Range(0.1f, 5f)]
    public float traumaDecaySpeed = 1.8f;

    [Header("Позиционная тряска")]
    [Tooltip("Максимальное смещение камеры по X/Y при trauma = 1.")]
    public float maxPositionShake = 0.6f;

    [Header("Угловая тряска (roll)")]
    [Tooltip("Максимальный угол поворота (градусы) при trauma = 1.")]
    public float maxAngleShake = 5f;

    [Header("Скорость шума Perlin")]
    [Tooltip("Частота сэмплирования шума. Выше — быстрее дрожит.")]
    [Range(1f, 50f)]
    public float noiseFrequency = 18f;

    // ── private ───────────────────────────────────────────────────────────────
    private Vector3   _baseLocalPos;
    private float     _trauma;        // текущая «травма» [0..1]
    private float     _noiseTime;     // время, пробегающее по оси Perlin

    // Зёрна (seed) — разные для X, Y и Roll, чтобы движения не совпадали
    private float _seedX;
    private float _seedY;
    private float _seedRoll;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance      = this;
        _baseLocalPos = transform.localPosition;
        // Примечание: _baseLocalRot НЕ сохраняем — камера вращается PlayerController'ом
        // каждый кадр, поэтому мы только добавляем roll-смещение поверх текущего угла.

        // Случайные зёрна, чтобы каждый запуск игры выглядел по-разному
        _seedX    = Random.Range(0f, 100f);
        _seedY    = Random.Range(0f, 100f);
        _seedRoll = Random.Range(0f, 100f);
    }

    void LateUpdate()
    {
        // unscaledDeltaTime → тряска работает даже при timeScale = 0 (хитстоп)
        _trauma = Mathf.Clamp01(_trauma - traumaDecaySpeed * Time.unscaledDeltaTime);
        _noiseTime += Time.unscaledDeltaTime * noiseFrequency;

        if (_trauma > 0f)
        {
            // shake — квадратичная кривая: слабые удары почти не заметны
            float shake = _trauma * _trauma;

            // Perlin Noise возвращает [0..1] → ремаппим в [-1..1]
            float x    = (Mathf.PerlinNoise(_seedX,    _noiseTime) - 0.5f) * 2f;
            float y    = (Mathf.PerlinNoise(_seedY,    _noiseTime) - 0.5f) * 2f;
            float roll = (Mathf.PerlinNoise(_seedRoll, _noiseTime) - 0.5f) * 2f;

            // Позиция: смещаем относительно базовой позиции покоя
            transform.localPosition = _baseLocalPos +
                new Vector3(x, y, 0f) * (maxPositionShake * shake);

            // Вращение: добавляем roll ПОВЕРХ текущего угла камеры (не перезаписываем pitch!)
            // PlayerController управляет pitch через localEulerAngles.x — мы его не трогаем.
            Vector3 currentEuler = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(
                currentEuler.x,
                currentEuler.y,
                roll * (maxAngleShake * shake)
            );
        }
        else
        {
            // Сбрасываем только позицию и Z-roll, не трогая pitch (X) и yaw (Y)
            transform.localPosition = _baseLocalPos;
            Vector3 e = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(e.x, e.y, 0f);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Добавляет «травму». Вызывай при любом ударе/взрыве.
    /// Значение накапливается, но не превышает 1.
    /// </summary>
    /// <param name="amount">Сила удара [0..1]. Типичные значения:
    ///   0.2f — мелкое попадание по врагу
    ///   0.5f — удар по игроку
    ///   0.8f — мощный взрыв
    /// </param>
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    /// <summary>
    /// Старый метод для обратной совместимости.
    /// Переводит (dur, mag) в trauma-единицы.
    /// </summary>
    public void Shake(float dur, float mag)
    {
        // mag условно считаем «травмой», dur масштабирует убывание
        float effectiveTrauma = Mathf.Clamp01(mag);
        AddTrauma(effectiveTrauma);
    }
}
