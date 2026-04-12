using UnityEngine;
using System.Collections.Generic;

public class EnemyPathController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private PlatformGraph platformGraph;

    [Header("Pathfinding Settings")]
    [SerializeField] private bool enablePathfinding = true;
    [SerializeField] private float platformArrivalThreshold = 0.3f;
    [SerializeField] private float pathRecalculationRate = 0.5f;
    [SerializeField] private float playerMoveThreshold = 1.5f;

    private List<PlatformNode> currentPath;
    private int currentTargetIndex;
    private PlatformNode currentTargetPlatform;
    private Vector2 lastPlayerPosition;
    private float lastRecalculationTime;
    private bool isNavigating = false;
    private Transform playerTransform;
    private JumpValidator jumpValidator;

    void Start()
    {
        Debug.Log("=== EnemyPathController Start ===");

        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError("EnemyPathController: Enemy component not found!");
                enabled = false;
                return;
            }
        }

        if (platformGraph == null)
        {
            platformGraph = FindObjectOfType<PlatformGraph>();
            if (platformGraph == null)
            {
                Debug.LogError("EnemyPathController: PlatformGraph not found!");
                enabled = false;
                return;
            }
        }

        jumpValidator = platformGraph.GetComponent<JumpValidator>();

        platformGraph.UpdateEnemyReference(enemy);
        platformGraph.ScanAndBuildGraph();

        var playerObj = FindObjectOfType<Player>();
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            lastPlayerPosition = playerTransform.position;
            Debug.Log($"Player found: {playerTransform.position}");
        }
        else
        {
            Debug.LogWarning("EnemyPathController: Player not found!");
        }

        Invoke(nameof(DelayedPathRecalculation), 0.5f);
    }

    private void DelayedPathRecalculation()
    {
        Debug.Log("DelayedPathRecalculation called");
        if (playerTransform != null)
        {
            RecalculatePath();
        }
    }

    void Update()
    {
        if (!enablePathfinding) return;
        if (playerTransform == null) return;

        if (ShouldRecalculatePath())
        {
            RecalculatePath();
        }

        if (isNavigating && currentPath != null && currentTargetIndex < currentPath.Count)
        {
            FollowCurrentPath();
        }

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Status - isNavigating: {isNavigating}, hasPath: {currentPath != null}, targetIndex: {currentTargetIndex}, customTarget: {enemy?.GetCurrentCustomTarget()?.ToString() ?? "null"}");
        }
    }

    private bool ShouldRecalculatePath()
    {
        if (Time.time - lastRecalculationTime < pathRecalculationRate)
            return false;

        if (!isNavigating || currentPath == null)
            return true;

        if (playerTransform != null)
        {
            float playerMovement = Vector2.Distance(playerTransform.position, lastPlayerPosition);
            if (playerMovement > playerMoveThreshold)
                return true;
        }

        return false;
    }

    private void RecalculatePath()
    {
        Debug.Log("=== RecalculatePath START ===");

        if (playerTransform == null) { Debug.Log("No player"); return; }
        if (!platformGraph.IsGraphBuilt) { Debug.Log("Graph not built"); return; }

        PlatformNode targetPlatform = platformGraph.GetPlatformAtPosition(playerTransform.position);
        Debug.Log($"Target platform (player at {playerTransform.position}): {(targetPlatform != null ? targetPlatform.name : "NULL")}");

        if (targetPlatform == null)
        {
            targetPlatform = platformGraph.GetNearestPlatform(playerTransform.position);
            Debug.Log($"Nearest platform: {(targetPlatform != null ? targetPlatform.name : "NULL")}");
        }

        if (targetPlatform == null)
        {
            Debug.Log("No target platform found!");
            return;
        }

        List<PlatformNode> newPath = platformGraph.FindPath(transform.position, targetPlatform);

        if (newPath != null && newPath.Count > 0)
        {
            string pathStr = "PATH: ";
            foreach (var p in newPath) pathStr += p.name + " → ";
            Debug.Log(pathStr);

            currentPath = newPath;
            currentTargetIndex = 0;
            isNavigating = true;

            Debug.Log($"Navigation started! Path length: {currentPath.Count}");
            SetNextTarget();
        }
        else
        {
            Debug.Log("No path found - disabling navigation");
            isNavigating = false;
            enemy?.ClearCustomTarget();
        }

        lastRecalculationTime = Time.time;
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void FollowCurrentPath()
    {
        if (currentTargetPlatform == null)
        {
            Debug.Log("FollowCurrentPath: target platform is null, setting next");
            SetNextTarget();
            return;
        }

        bool onTargetPlatform = currentTargetPlatform.IsPositionOnPlatform(transform.position);
        if (onTargetPlatform)
        {
            Debug.Log($"Reached platform {currentTargetPlatform.name}!");
            currentTargetIndex++;
            SetNextTarget();
            return;
        }

        if (enemy.HasReachedCustomTarget(platformArrivalThreshold))
        {
            Debug.Log("Reached custom target position!");
            currentTargetIndex++;
            SetNextTarget();
            return;
        }

        if (!enemy.GetCurrentCustomTarget().HasValue && currentTargetPlatform != null)
        {
            Debug.LogWarning("Enemy lost custom target, resetting...");
            SetNextTarget();
        }
    }

    private void SetNextTarget()
    {
        Debug.Log($"=== SetNextTarget called ===");
        Debug.Log($"Current index: {currentTargetIndex}, Path count: {(currentPath != null ? currentPath.Count : 0)}");

        if (currentPath == null)
        {
            Debug.Log("Path is null!");
            isNavigating = false;
            enemy?.ClearCustomTarget();
            return;
        }

        if (currentTargetIndex >= currentPath.Count)
        {
            Debug.Log("Target index out of range - navigation complete!");
            isNavigating = false;
            enemy?.ClearCustomTarget();
            return;
        }

        currentTargetPlatform = currentPath[currentTargetIndex];
        Debug.Log($"Current target platform: {currentTargetPlatform.name} (index {currentTargetIndex})");

        if (currentTargetIndex == currentPath.Count - 1)
        {
            if (playerTransform != null)
            {
                enemy?.SetCustomTargetPosition(playerTransform.position);
                Debug.Log($"Final target: player at {playerTransform.position}");
            }
        }
        else
        {
            PlatformNode nextPlatform = currentPath[currentTargetIndex + 1];

            Vector2 bestJumpPoint = currentTargetPlatform.GetBestJumpPoint(
                nextPlatform,
                enemy.MoveSpeed,
                enemy.JumpForce,
                enemy.GetGroundLayer(),
                enemy.GetObstacleLayer()
            );

            Vector2 directionToNext = (nextPlatform.Center - currentTargetPlatform.Center).normalized;

            bool isFall = (nextPlatform.TopSurface < currentTargetPlatform.TopSurface) &&
                          jumpValidator.CanFall(currentTargetPlatform, nextPlatform);

            if (isFall)
            {
                enemy?.SetCustomTargetPosition(bestJumpPoint);
                Debug.Log($"Fall to {nextPlatform.name}, moving to {bestJumpPoint}");
            }
            else
            {
                float requiredJumpForce = CalculateRequiredJumpForce(currentTargetPlatform, nextPlatform);
                enemy?.SetCustomTargetWithJump(bestJumpPoint, directionToNext, requiredJumpForce);
                Debug.Log($"Jump to {nextPlatform.name} from {bestJumpPoint}, force {requiredJumpForce}");
            }
        }
    }

    private float CalculateRequiredJumpForce(PlatformNode from, PlatformNode to)
    {
        Vector2 fromPoint = from.GetJumpPoint(new Vector2(to.Center.x - from.Center.x, 0));
        Vector2 toPoint = to.GetLandingPoint();
        float verticalDiff = toPoint.y - fromPoint.y;
        if (verticalDiff <= 0) return enemy.JumpForce;
        float extra = verticalDiff * 1.5f;
        return Mathf.Min(enemy.JumpForce + extra, 20f);
    }

    void OnDrawGizmos()
    {
        if (!enablePathfinding) return;

        if (currentPath != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                if (currentPath[i] != null && currentPath[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        currentPath[i].GetLandingPoint(),
                        currentPath[i + 1].GetLandingPoint()
                    );
                }
            }
        }

        if (currentTargetPlatform != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(currentTargetPlatform.GetLandingPoint(), 0.3f);
        }

        if (enemy != null && enemy.GetCurrentCustomTarget().HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.GetCurrentCustomTarget().Value, 0.2f);
            Gizmos.DrawLine(transform.position, enemy.GetCurrentCustomTarget().Value);
        }
    }
}