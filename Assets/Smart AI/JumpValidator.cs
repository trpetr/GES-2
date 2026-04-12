using UnityEngine;

public class JumpValidator : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int raycastIterations = 5;

    private Enemy enemy;
    private LayerMask groundLayer;

    public void Initialize(Enemy enemyReference)
    {
        enemy = enemyReference;
        groundLayer = enemy.GetGroundLayer();
    }

    public bool CanFall(PlatformNode from, PlatformNode to)
    {
        if (enemy == null) return false;

        if (to.TopSurface >= from.TopSurface) return false;

        Vector2 dropPoint = from.GetJumpPoint(new Vector2(to.Center.x - from.Center.x, 0));

        return from.IsPositionAbovePlatform(dropPoint, to, groundLayer);
    }

    public bool CanJump(PlatformNode from, PlatformNode to)
    {
        if (enemy == null) return false;

        Vector2 fromPoint = from.GetJumpPoint(GetDirection(from, to));
        Vector2 toPoint = to.GetLandingPoint();

        float horizontalDistance = Mathf.Abs(toPoint.x - fromPoint.x);
        float verticalDifference = toPoint.y - fromPoint.y;

        if (verticalDifference < 0)
        {
            return CanFall(from, to);
        }

        float maxJumpHeight = CalculateMaxJumpHeight();
        float maxJumpDistance = CalculateMaxJumpDistance();

        if (horizontalDistance > maxJumpDistance) return false;
        if (verticalDifference > maxJumpHeight) return false;

        return IsPathClear(fromPoint, toPoint);
    }

    private float CalculateMaxJumpHeight()
    {
        Rigidbody2D rb = enemy.GetRigidbody();
        float gravity = Mathf.Abs(Physics2D.gravity.y) * (rb?.gravityScale ?? 1f);
        float jumpHeight = (enemy.JumpForce * enemy.JumpForce) / (2f * gravity);
        return Mathf.Max(jumpHeight, 1.5f);
    }

    private float CalculateMaxJumpDistance()
    {
        Rigidbody2D rb = enemy.GetRigidbody();
        float gravity = Mathf.Abs(Physics2D.gravity.y) * (rb?.gravityScale ?? 1f);
        float jumpTime = 2f * enemy.JumpForce / gravity;
        float maxDistance = enemy.MoveSpeed * jumpTime;
        return Mathf.Max(maxDistance, 2f);
    }

    private Vector2 GetDirection(PlatformNode from, PlatformNode to)
    {
        return new Vector2(
            to.Center.x - from.Center.x,
            to.Center.y - from.Center.y
        ).normalized;
    }

    private bool IsPathClear(Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        for (int i = 0; i <= raycastIterations; i++)
        {
            float t = i / (float)raycastIterations;
            Vector2 checkPoint = Vector2.Lerp(start, end, t);

            Vector2 rayOrigin = checkPoint + Vector2.up * 0.2f;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, 0.5f, obstacleLayer);
            if (hit.collider != null)
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.red, 0.5f);
                return false;
            }
        }

        RaycastHit2D directHit = Physics2D.Raycast(start, direction, distance, obstacleLayer);
        bool isClear = directHit.collider == null || directHit.collider.GetComponent<PlatformNode>() != null;

        Debug.DrawLine(start, end, isClear ? Color.green : Color.red, 0.5f);

        return isClear;
    }

    void OnDrawGizmosSelected()
    {
        if (enemy == null) return;

        Gizmos.color = Color.cyan;
        float maxDistance = CalculateMaxJumpDistance();
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}