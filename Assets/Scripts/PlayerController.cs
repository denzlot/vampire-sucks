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

    [Header("Обзор мышью")]
    public float mouseSensitivity = 0.1f;
    [Tooltip("Перетащи сюда Main Camera (дочернюю к игроку). Если пусто — найдётся автоматически.")]
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch; // наклон камеры вверх/вниз
    private Vector3 horizontalVelocity; // текущая скорость по полу (для плавного разгона)

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // прячем курсор и фиксируем в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Look();
        Move();

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
        // читаем WASD напрямую с клавиатуры
        Vector2 input = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
        }

        // направление относительно того, куда смотрит игрок (нормализуем, чтобы по диагонали не было быстрее)
        Vector3 inputDir = transform.right * input.x + transform.forward * input.y;
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        // куда хотим: целевая скорость по полу
        Vector3 targetVelocity = inputDir * moveSpeed;

        // плавно подводим текущую скорость к целевой:
        // есть ввод — разгон (acceleration), нет ввода — торможение (deceleration)
        float rate = (inputDir.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);

        // простая гравитация, чтобы прилипать к полу
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // собираем итоговое перемещение: горизонталь + вертикаль
        Vector3 move = horizontalVelocity;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
