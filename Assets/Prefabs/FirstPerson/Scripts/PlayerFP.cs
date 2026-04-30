using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFP : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Max Walk Speed")]
    public float maxWalkSpeed = 6f;
    [Tooltip("Max Acceleration")]
    public float acceleration = 50f;
    [Tooltip("Braking Deceleration")]
    public float deceleration = 25f;
    [Tooltip("Трение поверхности")]
    public float groundFriction = 8f;

    [Header("Air Control")]
    [Range(0, 1)]
    public float airControl = 0.2f;
    public float gravity = 20f;
    public float jumpHeight = 1.5f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Скрываем курсор, как это обычно делает движок при старте FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * moveX + transform.forward * moveZ).normalized;

        if (controller.isGrounded)
        {
            // Логика движения по земле
            if (inputDir.magnitude > 0)
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, inputDir * maxWalkSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * groundFriction * Time.deltaTime);
            }

            // Прыжок
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
            else
            {
                verticalVelocity = -2f; // Небольшое прижатие к земле для стабильности isGrounded
            }
        }
        else
        {
            // Логика управления в воздухе (Air Control)
            if (inputDir.magnitude > 0)
            {
                Vector3 airSteer = inputDir * maxWalkSpeed;
                currentVelocity = Vector3.Lerp(currentVelocity, airSteer, airControl * Time.deltaTime * 5f);
            }
        }

        // Применяем гравитацию
        verticalVelocity -= gravity * Time.deltaTime;

        // Сборка финального вектора движения
        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = verticalVelocity * Time.deltaTime;

        controller.Move(finalMove);
    }
}