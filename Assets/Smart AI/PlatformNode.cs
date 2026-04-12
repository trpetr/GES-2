using UnityEngine;
using System.Collections.Generic;

public class PlatformNode : MonoBehaviour
{
    [Header("Platform Data")]
    [SerializeField] private bool isDynamic = false;
    [SerializeField] private float jumpPointSearchStep = 0.5f;

    private Collider2D platformCollider;
    private Bounds cachedBounds;
    private float lastUpdateTime;

    public List<PlatformNode> reachablePlatforms = new List<PlatformNode>();

    public float LeftEdge => cachedBounds.min.x;
    public float RightEdge => cachedBounds.max.x;
    public float TopSurface => cachedBounds.max.y;
    public float BottomEdge => cachedBounds.min.y;
    public Vector2 Center => cachedBounds.center;
    public bool IsDynamic => isDynamic;
    public Bounds Bounds => cachedBounds;

    void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        if (platformCollider == null)
        {
            Debug.LogError($"PlatformNode on {gameObject.name} requires a Collider2D component!");
            enabled = false;
            return;
        }

        UpdateBounds();
    }

    void Update()
    {
        if (isDynamic)
        {
            UpdateBounds();
        }
    }

    private void UpdateBounds()
    {
        cachedBounds = platformCollider.bounds;
        lastUpdateTime = Time.time;
    }

    public bool IsPositionOnPlatform(Vector2 position)
    {
        return position.x >= LeftEdge &&
               position.x <= RightEdge &&
               Mathf.Abs(position.y - TopSurface) <= 0.2f;
    }

    public Vector2 GetLandingPoint()
    {
        return new Vector2(Center.x, TopSurface + 0.1f);
    }

    public Vector2 GetJumpPoint(Vector2 targetDirection)
    {
        float edgeX = targetDirection.x > 0 ? RightEdge : LeftEdge;
        return new Vector2(edgeX, TopSurface + 0.1f);
    }

    public Vector2 GetBestJumpPoint(PlatformNode targetPlatform, float moveSpeed, float jumpForce, LayerMask groundLayer, LayerMask obstacleLayer)
    {
        float direction = Mathf.Sign(targetPlatform.Center.x - Center.x);

        List<float> potentialX = new List<float>();

        float startX = LeftEdge;
        float endX = RightEdge;

        for (float x = startX; x <= endX; x += jumpPointSearchStep)
        {
            potentialX.Add(x);
        }

        potentialX.Add(LeftEdge);
        potentialX.Add(RightEdge);

        if (direction > 0)
            potentialX.Sort((a, b) => b.CompareTo(a));
        else
            potentialX.Sort();

        foreach (float x in potentialX)
        {
            Vector2 point = new Vector2(x, TopSurface + 0.1f);

            if (!IsPointReachable(point, direction, groundLayer))
                continue;

            if (CanJumpFromPoint(point, targetPlatform, moveSpeed, jumpForce, obstacleLayer))
            {
                Debug.Log($"Best jump point found at {point}");
                return point;
            }
        }

        float edgeX = direction > 0 ? RightEdge : LeftEdge;
        Debug.LogWarning($"No optimal jump point found, using edge at {edgeX}");
        return new Vector2(edgeX, TopSurface + 0.1f);
    }

    private bool IsPointReachable(Vector2 targetPoint, float direction, LayerMask groundLayer)
    {
        return true;
    }

    private bool CanJumpFromPoint(Vector2 fromPoint, PlatformNode targetPlatform, float moveSpeed, float jumpForce, LayerMask obstacleLayer)
    {
        Vector2 toPoint = targetPlatform.GetLandingPoint();

        float gravity = Mathf.Abs(Physics2D.gravity.y);
        Vector2 velocity = new Vector2(
            Mathf.Sign(toPoint.x - fromPoint.x) * moveSpeed,
            jumpForce
        );
        Vector2 position = fromPoint;

        float timeStep = 0.02f;
        int maxSteps = 200;

        for (int i = 0; i < maxSteps; i++)
        {
            position += velocity * timeStep;
            velocity.y -= gravity * timeStep;

            RaycastHit2D hit = Physics2D.Linecast(position, position + velocity * timeStep, obstacleLayer);
            if (hit.collider != null && hit.collider.GetComponent<PlatformNode>() == null)
            {
                return false;
            }

            if (position.y <= toPoint.y && velocity.y < 0)
            {
                float xDiff = Mathf.Abs(position.x - toPoint.x);
                return xDiff < 0.5f;
            }
        }

        return false;
    }

    public bool IsPositionAbovePlatform(Vector2 position, PlatformNode belowPlatform, LayerMask groundLayer)
    {
        if (belowPlatform == null) return false;

        float tolerance = 0.2f;
        if (position.x < belowPlatform.LeftEdge - tolerance || position.x > belowPlatform.RightEdge + tolerance)
            return false;

        if (position.y <= belowPlatform.TopSurface) return false;

        Vector2 origin = new Vector2(position.x, position.y);
        float distance = position.y - belowPlatform.TopSurface;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, groundLayer);
        if (hit.collider != null)
        {
            PlatformNode hitPlatform = hit.collider.GetComponent<PlatformNode>();
            return hitPlatform == belowPlatform;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (platformCollider == null) return;

        Gizmos.color = Color.green;
        Bounds bounds = platformCollider.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (reachablePlatforms != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var neighbor in reachablePlatforms)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(GetLandingPoint(), neighbor.GetLandingPoint());
                }
            }
        }
    }
}