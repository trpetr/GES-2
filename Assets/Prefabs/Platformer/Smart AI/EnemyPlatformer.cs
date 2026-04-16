using UnityEngine;
using System.Collections.Generic;

public class EnemyPlatformer : MonoBehaviour
{
    [Header("Настройки ИИ")]
    public float moveSpeed = 5f;
    public float detectionRadius = 100f;
    public LayerMask groundLayer;

    [Tooltip("Если true, враг не будет прыгать/спрыгивать, пока игрок на другой платформе")]
    public bool followPlayerOnlyOnSamePlatform = false;

    [Header("Параметры поиска точки")]
    public float searchStep = 0.2f;
    public float maxSearchDistance = 10f;
    public float jumpXOffset = 2f;

    [Header("Настройки Прыжка")]
    public float jumpHeightMargin = 1.5f;
    public float jumpTimeMultiplier = 2.0f;

    [Header("Персонаж игрока")]
    [SerializeField] private Transform player;

    private PlatformNode currentPlatform;
    private List<PlatformNode> currentPath = new List<PlatformNode>();
    private Rigidbody2D rb;

    private bool isJumping = false;
    private float requiredVx;

    private float baseGravityScale;
    private float? lockedEdgeX = null;
    private PlatformNode lastTargetPlatform;

    // Переменные для отслеживания "застревания" у стены
    private float stuckTimer = 0f;
    private const float stuckThreshold = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        baseGravityScale = rb.gravityScale;

        // Автоматически убираем трение, чтобы не цепляться за стены физически
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("SlipperyEnemy");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        rb.sharedMaterial = slipperyMat;
    }

    void Update()
    {
        if (player == null) return;

        // Сброс состояния прыжка при приземлении
        if (isJumping && rb.velocity.y <= 0.01f && IsGrounded())
        {
            isJumping = false;
            lockedEdgeX = null;
            rb.gravityScale = baseGravityScale;
        }

        if (Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            UpdateCurrentPlatform();
            PlatformNode targetPlatform = FindPlatformUnderObject(player);

            if (targetPlatform != lastTargetPlatform)
            {
                lockedEdgeX = null;
                lastTargetPlatform = targetPlatform;
            }

            if (targetPlatform == currentPlatform)
            {
                lockedEdgeX = null;
                MoveTowards(player.position.x);
            }
            else if (targetPlatform != null && !followPlayerOnlyOnSamePlatform)
            {
                currentPath = BuildPath(currentPlatform, targetPlatform);

                if (isJumping)
                {
                    rb.velocity = new Vector2(requiredVx, rb.velocity.y);
                }
                else
                {
                    MoveAlongPath();
                }
            }
            else
            {
                StopMoving();
            }
        }
        else
        {
            StopMoving();
        }

        // Проверка на застревание у стены (если не в прыжке)
        HandleStuckCheck();
    }

    void HandleStuckCheck()
    {
        if (isJumping) return;

        // Если ИИ пытается двигаться (velocity.x должен быть высокий), 
        // но по факту почти не движется (rb.velocity.x маленький)
        if (Mathf.Abs(rb.velocity.x) < 0.5f && Mathf.Abs(moveSpeed) > 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckThreshold)
            {
                // Если упёрлись — обнуляем X, чтобы гравитация потянула вниз
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void StopMoving()
    {
        if (!isJumping)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void MoveAlongPath()
    {
        if (currentPath == null || currentPath.Count < 2 || isJumping) return;
        PlatformNode nextStep = currentPath[1];

        if (nextStep.Bounds.min.y > currentPlatform.Bounds.max.y)
            HandleJumpUp(nextStep);
        else
            HandleJumpDown(nextStep);
    }

    void HandleJumpUp(PlatformNode target)
    {
        if (lockedEdgeX == null)
        {
            lockedEdgeX = (transform.position.x < target.transform.position.x)
                            ? target.Bounds.min.x : target.Bounds.max.x;
        }

        float targetEdgeX = lockedEdgeX.Value;
        float currentSurfaceY = currentPlatform.Bounds.max.y;
        Vector2 jumpPoint = Vector2.zero;
        bool pointFound = false;

        float directionAway = (targetEdgeX <= target.Bounds.center.x) ? -1f : 1f;

        for (float d = jumpXOffset; d < maxSearchDistance; d += searchStep)
        {
            float checkX = targetEdgeX + (d * directionAway);
            Vector2 checkPos = new Vector2(checkX, currentSurfaceY);
            Collider2D hit = Physics2D.OverlapPoint(checkPos, groundLayer);
            if (hit != null && hit.gameObject == currentPlatform.gameObject)
            {
                jumpPoint = checkPos;
                pointFound = true;
                break;
            }
        }

        if (pointFound)
        {
            float distToJumpPoint = Mathf.Abs(transform.position.x - jumpPoint.x);
            if (distToJumpPoint < 0.3f)
            {
                float targetX = targetEdgeX + (directionAway * -0.5f);
                CalculateAndApplyJump(jumpPoint, new Vector2(targetX, target.Bounds.max.y));
            }
            else
            {
                MoveTowards(jumpPoint.x);
            }
        }
        else lockedEdgeX = null;
    }

    void HandleJumpDown(PlatformNode target)
    {
        if (lockedEdgeX == null)
        {
            lockedEdgeX = (target.transform.position.x > transform.position.x)
                      ? currentPlatform.Bounds.max.x : currentPlatform.Bounds.min.x;
        }

        float edgeX = lockedEdgeX.Value;
        float directionAway = (edgeX == currentPlatform.Bounds.max.x) ? 1f : -1f;

        if (Mathf.Abs(transform.position.x - edgeX) < 0.3f)
        {
            Vector2 landPoint = new Vector2(edgeX + (directionAway * jumpXOffset * 2), target.Bounds.max.y);
            float clampedX = Mathf.Clamp(landPoint.x, target.Bounds.min.x + 0.5f, target.Bounds.max.x - 0.5f);
            landPoint = new Vector2(clampedX, target.Bounds.max.y);
            CalculateAndApplyJump(transform.position, landPoint);
        }
        else MoveTowards(edgeX);
    }

    void CalculateAndApplyJump(Vector2 start, Vector2 end)
    {
        float timeMult = Mathf.Max(0.1f, jumpTimeMultiplier);
        float gravity = Mathf.Abs(Physics2D.gravity.y * baseGravityScale);
        float heightTarget = Mathf.Max(start.y, end.y) + jumpHeightMargin;
        float h = heightTarget - start.y;

        float baseVy = Mathf.Sqrt(2 * gravity * h);
        float t1 = baseVy / gravity;
        float hPeakToTarget = heightTarget - end.y;
        float t2 = Mathf.Sqrt(2 * hPeakToTarget / gravity);
        float totalTime = t1 + t2;

        float dx = end.x - start.x;
        float baseVx = dx / totalTime;

        rb.gravityScale = baseGravityScale * (timeMult * timeMult);
        requiredVx = baseVx * timeMult;
        float requiredVy = baseVy * timeMult;

        isJumping = true;
        rb.velocity = new Vector2(requiredVx, requiredVy);
        lockedEdgeX = null;
    }

    void MoveTowards(float targetX)
    {
        float dir = Mathf.Sign(targetX - transform.position.x);
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
    }

    bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.7f, groundLayer);
        return hit.collider != null;
    }

    void UpdateCurrentPlatform()
    {
        PlatformNode found = FindPlatformUnderObject(transform);
        if (found != null) currentPlatform = found;
    }

    PlatformNode FindPlatformUnderObject(Transform obj)
    {
        RaycastHit2D hit = Physics2D.Raycast(obj.position, Vector2.down, 1.5f, groundLayer);
        return hit.collider != null ? hit.collider.GetComponent<PlatformNode>() : null;
    }

    List<PlatformNode> BuildPath(PlatformNode start, PlatformNode end)
    {
        if (start == null || end == null) return null;
        Queue<List<PlatformNode>> queue = new Queue<List<PlatformNode>>();
        queue.Enqueue(new List<PlatformNode> { start });
        HashSet<PlatformNode> visited = new HashSet<PlatformNode>();
        while (queue.Count > 0)
        {
            List<PlatformNode> path = queue.Dequeue();
            PlatformNode node = path[path.Count - 1];
            if (node == end) return path;
            foreach (var next in node.reachablePlatforms)
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(new List<PlatformNode>(path) { next });
                }
            }
        }
        return null;
    }
}