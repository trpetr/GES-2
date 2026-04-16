using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPlatformer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float runSpeed = 12f;
    public float acceleration = 40f;
    public float deceleration = 30f;
    public float friction = 20f;

    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public float gravityScale = 3f;
    public float fallMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.5f;

    [Header("Abilities")]
    [Tooltip("Включить/выключить двойной прыжок")]
    public bool canDoubleJump = true;
    private bool doubleJumpUsed; // Флаг: был ли уже использован второй прыжок

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isRunning;
    private bool jumpRequest;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        // Настройки для плавности (на случай, если захочешь вернуть)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Z);

        // Логика прыжка
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                jumpRequest = true;
                doubleJumpUsed = false; // Сбрасываем флаг при прыжке с земли
            }
            else if (canDoubleJump && !doubleJumpUsed)
            {
                jumpRequest = true;
                doubleJumpUsed = true; // Помечаем, что второй прыжок потрачен
            }
        }

        ApplyVariableJumpHeight();
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();

        if (jumpRequest)
        {
            // Обнуляем вертикальную скорость перед прыжком (важно для двойного прыжка в падении)
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpRequest = false;
        }
    }

    void ApplyMovement()
    {
        float targetSpeed = moveInput * (isRunning ? runSpeed : walkSpeed);
        float currentAccel = (Mathf.Abs(rb.velocity.x) > 0.1f && Mathf.Sign(moveInput) != Mathf.Sign(rb.velocity.x) && moveInput != 0)
                            ? deceleration : acceleration;

        if (moveInput != 0)
        {
            float newX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);
            rb.velocity = new Vector2(newX, rb.velocity.y);
        }
        else
        {
            float newX = Mathf.MoveTowards(rb.velocity.x, 0, friction * Time.fixedDeltaTime);
            rb.velocity = new Vector2(newX, rb.velocity.y);
        }
    }

    void ApplyVariableJumpHeight()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Если приземлились, восстанавливаем возможность двойного прыжка
        if (isGrounded)
        {
            doubleJumpUsed = false;
        }
    }
}