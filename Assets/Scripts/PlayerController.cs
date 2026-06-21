using UnityEngine;
using UnityEngine.InputSystem; // новый Input System

// Ходьба на WASD + обзор мышью.
// Вешается на объект Player (у которого есть Character Controller).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 6f;
    [Tooltip("Как быстро набирается скорость (ед/сек²). Больше = резче старт.")]
    public float acceleration = 80f;
    [Tooltip("Как быстро гасится скорость при отпускании клавиш (ед/сек²). Больше = резче стоп.")]
    public float deceleration = 100f;
    public float gravity = -20f;

    [Header("Дэш (рывок) — клавиша Left Shift")]
    [Tooltip("Скорость во время рывка.")]
    public float dashSpeed = 25f;
    [Tooltip("Длительность рывка в секундах (= окно неуязвимости).")]
    public float dashDuration = 0.15f;
    [Tooltip("Пауза между рывками в секундах.")]
    public float dashCooldown = 1f;
    [Tooltip("Звук рывка (необязательно; нужен AudioSource на игроке).")]
    public AudioClip dashSound;
    [Tooltip("Кратковременное расширение FOV при рывке для ощущения скорости (0 = выключить).")]
    public float dashFovKick = 8f;

    [Header("Обзор мышью")]
    public float mouseSensitivity = 0.1f;
    [Tooltip("Перетащи сюда Main Camera (дочернюю к игроку). Если пусто — найдётся автоматически.")]
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch; // наклон камеры вверх/вниз
    private Vector3 horizontalVelocity; // текущая скорость по полу (для плавного разгона)

    // состояние дэша
    private bool isDashing;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private Vector3 dashDirection;
    private Health health;
    private AudioSource audioSource;
    private Camera cam;
    private float baseFov;

    // публичные значения для других систем (UI и т.д.)
    public bool IsDashing => isDashing;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownLeft);

    void Start()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();

        // прячем курсор и фиксируем в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // запоминаем камеру и её базовый FOV (для рывка)
        if (cameraTransform != null)
            cam = cameraTransform.GetComponent<Camera>();
        if (cam != null)
            baseFov = cam.fieldOfView;
    }

    void Update()
    {
        Look();
        HandleDash();
        Move();
        UpdateFov();

        // ESC — освободить курсор (удобно при тесте в редакторе)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
    }

    void Look()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // поворот тела игрока влево/вправо
        transform.Rotate(Vector3.up * mouseDelta.x);

        // наклон камеры вверх/вниз с ограничением, чтобы не перевернуться
        pitch -= mouseDelta.y;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        if (cameraTransform != null)
            cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void Move()
    {
        Vector2 input = ReadMoveInput();

        if (isDashing)
        {
            // во время рывка двигаемся строго в заданном направлении с дэш-скоростью
            horizontalVelocity = dashDirection * dashSpeed;
        }
        else
        {
            // направление относительно взгляда (нормализуем, чтобы по диагонали не было быстрее)
            Vector3 inputDir = transform.right * input.x + transform.forward * input.y;
            inputDir = Vector3.ClampMagnitude(inputDir, 1f);

            // плавно подводим текущую скорость к целевой
            Vector3 targetVelocity = inputDir * moveSpeed;
            float rate = (inputDir.sqrMagnitude > 0.01f) ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);
        }

        // простая гравитация, чтобы прилипать к полу
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // собираем итоговое перемещение: горизонталь + вертикаль
        Vector3 move = horizontalVelocity;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    // ---------- ДЭШ ----------

    void HandleDash()
    {
        // тикаем кулдаун
        if (dashCooldownLeft > 0f)
            dashCooldownLeft -= Time.deltaTime;

        // старт по Left Shift, если не в дэше и кулдаун прошёл
        var kb = Keyboard.current;
        bool dashPressed = kb != null && kb.leftShiftKey.wasPressedThisFrame;
        if (dashPressed && !isDashing && dashCooldownLeft <= 0f)
            StartDash();

        // отсчёт длительности дэша
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
                EndDash();
        }
    }

    void StartDash()
    {
        // направление: куда жмём; если стоим на месте — вперёд по взгляду
        Vector2 input = ReadMoveInput();
        Vector3 dir = transform.right * input.x + transform.forward * input.y;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;
        dashDirection = dir.normalized;

        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownLeft = dashCooldown;

        // i-frames: на время рывка игрок неуязвим
        if (health != null) health.isInvulnerable = true;

        // звук рывка (если назначен и есть AudioSource)
        if (audioSource != null && dashSound != null)
            audioSource.PlayOneShot(dashSound);
    }

    void EndDash()
    {
        isDashing = false;
        if (health != null) health.isInvulnerable = false;
    }

    // плавно подгоняем FOV: расширен во время рывка, иначе — к базовому
    void UpdateFov()
    {
        if (cam == null || dashFovKick <= 0f) return;
        float targetFov = baseFov + (isDashing ? dashFovKick : 0f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 12f);
    }

    // чтение WASD (используется и движением, и дэшем)
    Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
        }
        return input;
    }
}
