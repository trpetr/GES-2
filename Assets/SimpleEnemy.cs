using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform player;

    // Кэш
    private Rigidbody2D rb;
    private Transform cachedTransform;
    private float colExtent;
    private float detectionRangeSqr;

    // Состояния
    private bool isGrounded;
    private bool canJump = true;
    private float jumpTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedTransform = transform;
        detectionRangeSqr = detectionRange * detectionRange;

        if (player == null)
            player = FindObjectOfType<Player>()?.transform;

        var col = GetComponent<Collider2D>();
        if (col != null) colExtent = col.bounds.extents.y;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Проверка дистанции без квадратного корня
        float dx = player.position.x - cachedTransform.position.x;
        float dy = player.position.y - cachedTransform.position.y;

        if (dx * dx + dy * dy <= detectionRangeSqr)
        {
            // Ground check одной строкой
            Vector2 groundPos = new Vector2(cachedTransform.position.x, cachedTransform.position.y - colExtent - 0.05f);
            isGrounded = Physics2D.Raycast(groundPos, Vector2.down, 0.1f, groundLayer);

            // Движение
            float dir = dx > 0 ? 1f : -1f;
            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);

            // Прыжок если нужно
            if (isGrounded && canJump && (dy > 0.5f || HasObstacleAhead(dir)))
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                canJump = false;
                jumpTimer = 0.5f;
            }

            // Поворот
            if ((dir > 0 && cachedTransform.localScale.x < 0) || (dir < 0 && cachedTransform.localScale.x > 0))
            {
                var scale = cachedTransform.localScale;
                scale.x *= -1;
                cachedTransform.localScale = scale;
            }
        }
        else if (rb.velocity.sqrMagnitude > 0.01f)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // Cooldown прыжка
        if (jumpTimer > 0)
        {
            jumpTimer -= Time.fixedDeltaTime;
            if (jumpTimer <= 0) canJump = true;
        }
    }

    private bool HasObstacleAhead(float direction)
    {
        Vector2 origin = new Vector2(
            cachedTransform.position.x + direction * 0.5f,
            cachedTransform.position.y
        );
        return Physics2D.Raycast(origin, Vector2.right * direction, 0.5f, groundLayer);
    }

    public void SetPlayer(Transform playerTransform) => player = playerTransform;
}