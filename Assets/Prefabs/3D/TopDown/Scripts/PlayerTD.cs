using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerTD : MonoBehaviour
{
    [Header("Движение")]
    public float maxWalkSpeed = 6f;
    public float acceleration = 50f;
    public float deceleration = 25f;
    public float groundFriction = 8f;

    [Header("Прыжок")]
    [Range(0, 1)]
    public float airControl = 0.2f;
    public float gravity = 20f;
    public float jumpHeight = 1.5f;
    public bool canJump = true;

    [Header("Настройки спрайта")]
    [Tooltip("Включить отзеркаливание спрайта влево/вправо?")]
    public bool flipEnabled = true;
    [Tooltip("Объект со спрайтом (если это дочерний объект)")]
    public Transform visualTransform;

    [Header("Настройки вращения (3D)")]
    [Tooltip("Включает поворот всего объекта в сторону движения (для 3D моделей)")]
    public bool rotateToMovement = false;
    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool facingRight = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Если визуальный объект не назначен, используем текущий transform
        if (visualTransform == null) visualTransform = transform;
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(moveX, 0, moveZ).normalized;

        if (controller.isGrounded)
        {
            if (inputDir.magnitude > 0)
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, inputDir * maxWalkSpeed, acceleration * Time.deltaTime);

                // 1. Поворот для 3D (если нужно)
                if (rotateToMovement)
                {
                    ApplyRotation(inputDir);
                }

                // 2. Инверсия для 2D спрайта
                if (flipEnabled)
                {
                    HandleFlip(moveX);
                }
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * groundFriction * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && canJump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
            else
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            if (inputDir.magnitude > 0)
            {
                Vector3 airSteer = inputDir * maxWalkSpeed;
                currentVelocity = Vector3.Lerp(currentVelocity, airSteer, airControl * Time.deltaTime * 5f);

                if (flipEnabled) HandleFlip(moveX);
            }
        }

        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = verticalVelocity * Time.deltaTime;

        controller.Move(finalMove);
    }

    void HandleFlip(float horizontalInput)
    {
        // Инвертируем только если есть ввод по горизонтали
        if (horizontalInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = visualTransform.localScale;
        scale.x *= -1;
        visualTransform.localScale = scale;
    }

    void ApplyRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}