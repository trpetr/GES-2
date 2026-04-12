using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 10f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Transform player;

    private Rigidbody2D rb;
    private Transform cachedTransform;
    private float colExtent;
    private float detectionRangeSqr;

    private bool isGrounded;
    private bool canJump = true;
    private float jumpTimer;

    [Header("Pathfinding")]
    private Vector2? customMoveTarget = null;
    private bool useCustomTarget = false;
    private Vector2? customJumpDirection = null;
    private float customJumpForce = 0f;

    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;

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
        if (player == null && !useCustomTarget) return;

        float dx, dy;

        if (useCustomTarget && customMoveTarget.HasValue)
        {
            dx = customMoveTarget.Value.x - cachedTransform.position.x;
            dy = customMoveTarget.Value.y - cachedTransform.position.y;
        }
        else if (player != null)
        {
            dx = player.position.x - cachedTransform.position.x;
            dy = player.position.y - cachedTransform.position.y;
        }
        else
        {
            return;
        }

        float playerDx = (player != null) ? player.position.x - cachedTransform.position.x : dx;
        float playerDy = (player != null) ? player.position.y - cachedTransform.position.y : dy;
        float playerDistanceSqr = playerDx * playerDx + playerDy * playerDy;

        bool shouldMove = useCustomTarget || (playerDistanceSqr <= detectionRangeSqr);

        if (shouldMove)
        {
            Vector2 groundPos = new Vector2(cachedTransform.position.x, cachedTransform.position.y - colExtent - 0.05f);
            isGrounded = Physics2D.Raycast(groundPos, Vector2.down, 0.1f, groundLayer);

            float dir = dx > 0 ? 1f : -1f;

            bool shouldJumpForCustomTarget = false;
            float targetJumpForce = jumpForce;

            if (useCustomTarget && customMoveTarget.HasValue && isGrounded && canJump)
            {
                float horizontalDistance = Mathf.Abs(dx);
                float targetVerticalDiff = customMoveTarget.Value.y - cachedTransform.position.y;
                bool isAtEdge = IsAtPlatformEdge(dir);

                if (customJumpDirection.HasValue)
                {
                    shouldJumpForCustomTarget = true;
                    targetJumpForce = customJumpForce > 0 ? customJumpForce : jumpForce;
                }
                else if (targetVerticalDiff > 0.3f || (isAtEdge && horizontalDistance < 1f))
                {
                    shouldJumpForCustomTarget = true;
                }
            }

            if (isGrounded && canJump && (shouldJumpForCustomTarget || dy > 0.5f || HasObstacleAhead(dir)))
            {
                if (customJumpDirection.HasValue)
                {
                    Vector2 jumpVelocity = new Vector2(
                        customJumpDirection.Value.x * moveSpeed,
                        targetJumpForce
                    );
                    rb.velocity = jumpVelocity;
                }
                else
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                }

                canJump = false;
                jumpTimer = 0.5f;
                customJumpDirection = null;
                customJumpForce = 0f;
            }

            bool reachedHorizontalTarget = useCustomTarget && customMoveTarget.HasValue && Mathf.Abs(dx) < 0.2f;
            if (!reachedHorizontalTarget && !customJumpDirection.HasValue)
            {
                rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
            }
            else if (reachedHorizontalTarget && !shouldJumpForCustomTarget)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

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

        if (jumpTimer > 0)
        {
            jumpTimer -= Time.fixedDeltaTime;
            if (jumpTimer <= 0) canJump = true;
        }
    }

    private bool IsAtPlatformEdge(float direction)
    {
        Vector2 edgeCheckPos = new Vector2(
            cachedTransform.position.x + direction * 0.4f,
            cachedTransform.position.y - colExtent
        );

        RaycastHit2D groundAhead = Physics2D.Raycast(edgeCheckPos, Vector2.down, 0.2f, groundLayer);
        return groundAhead.collider == null;
    }

    private bool HasObstacleAhead(float direction)
    {
        Vector2 origin = new Vector2(
            cachedTransform.position.x + direction * 0.5f,
            cachedTransform.position.y
        );
        return Physics2D.Raycast(origin, Vector2.right * direction, 0.5f, obstacleLayer);
    }

    public void SetCustomTargetPosition(Vector2 targetPosition)
    {
        customMoveTarget = targetPosition;
        useCustomTarget = true;
        customJumpDirection = null;
        customJumpForce = 0f;
    }

    public void SetCustomTargetWithJump(Vector2 targetPosition, Vector2 jumpDirection, float jumpForceValue)
    {
        customMoveTarget = targetPosition;
        useCustomTarget = true;
        customJumpDirection = jumpDirection.normalized;
        customJumpForce = jumpForceValue;
    }

    public void ClearCustomTarget()
    {
        customMoveTarget = null;
        useCustomTarget = false;
        customJumpDirection = null;
        customJumpForce = 0f;
    }

    public bool HasReachedCustomTarget(float threshold = 0.3f)
    {
        if (!useCustomTarget || !customMoveTarget.HasValue) return false;
        return Vector2.Distance(transform.position, customMoveTarget.Value) <= threshold;
    }

    public Vector2? GetCurrentCustomTarget()
    {
        return customMoveTarget;
    }

    public bool IsUsingCustomTarget()
    {
        return useCustomTarget;
    }

    public void SetPlayer(Transform playerTransform) => player = playerTransform;

    public Rigidbody2D GetRigidbody() => rb;

    public LayerMask GetGroundLayer() => groundLayer;

    public LayerMask GetObstacleLayer() => obstacleLayer;

    private void OnDrawGizmosSelected()
    {
        if (customMoveTarget.HasValue && useCustomTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(customMoveTarget.Value, 0.3f);
            Gizmos.DrawLine(transform.position, customMoveTarget.Value);
        }

        if (customJumpDirection.HasValue)
        {
            Gizmos.color = Color.red;
            Vector3 dir = customJumpDirection.Value;
            Gizmos.DrawRay(transform.position, dir * 2f);
        }
    }
}