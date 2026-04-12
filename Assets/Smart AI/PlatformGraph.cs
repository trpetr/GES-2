using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlatformGraph : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float scanInterval = 2f;
    [SerializeField] private bool autoScanOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private List<PlatformNode> allPlatforms = new List<PlatformNode>();
    private Dictionary<PlatformNode, List<PlatformNode>> adjacencyList = new Dictionary<PlatformNode, List<PlatformNode>>();

    private JumpValidator jumpValidator;
    private float lastScanTime;
    private bool isGraphBuilt = false;
    private Enemy currentEnemy;

    public bool IsGraphBuilt => isGraphBuilt;
    public List<PlatformNode> AllPlatforms => allPlatforms;

    void Awake()
    {
        jumpValidator = GetComponent<JumpValidator>();
        if (jumpValidator == null)
        {
            jumpValidator = gameObject.AddComponent<JumpValidator>();
        }
    }

    void Start()
    {
        if (autoScanOnStart)
        {
            ScanAndBuildGraph();
        }
    }

    void Update()
    {
        if (Time.time - lastScanTime >= scanInterval)
        {
            RefreshGraph();
        }
    }

    public void UpdateEnemyReference(Enemy enemy)
    {
        currentEnemy = enemy;
        jumpValidator.Initialize(enemy);

        if (isGraphBuilt)
        {
            BuildGraph();
        }
    }

    public void ScanAndBuildGraph()
    {
        FindAllPlatforms();
        BuildGraph();
        isGraphBuilt = true;
        lastScanTime = Time.time;

        if (showDebugInfo)
        {
            Debug.Log($"PlatformGraph: Found {allPlatforms.Count} platforms, built graph with {adjacencyList.Count} nodes");
        }
    }

    private void FindAllPlatforms()
    {
        allPlatforms.Clear();

        PlatformNode[] foundPlatforms = FindObjectsOfType<PlatformNode>();
        allPlatforms.AddRange(foundPlatforms);

        Collider2D[] colliders = FindObjectsOfType<Collider2D>();
        foreach (var collider in colliders)
        {
            if (((1 << collider.gameObject.layer) & platformLayer) != 0)
            {
                if (collider.GetComponent<PlatformNode>() == null)
                {
                    PlatformNode newNode = collider.gameObject.AddComponent<PlatformNode>();
                    allPlatforms.Add(newNode);
                }
            }
        }

        allPlatforms = allPlatforms.OrderBy(p => p.Bounds.center.y).ThenBy(p => p.Bounds.center.x).ToList();
    }

    private void BuildGraph()
    {
        if (currentEnemy == null)
        {
            if (showDebugInfo) Debug.LogWarning("PlatformGraph: No enemy reference, skipping graph build");
            return;
        }

        adjacencyList.Clear();

        foreach (var platform in allPlatforms)
        {
            platform.reachablePlatforms.Clear();
            List<PlatformNode> reachable = new List<PlatformNode>();

            foreach (var other in allPlatforms)
            {
                if (platform == other) continue;

                if (jumpValidator.CanJump(platform, other))
                {
                    reachable.Add(other);
                }
            }

            platform.reachablePlatforms = reachable;
            adjacencyList[platform] = reachable;
        }

        if (showDebugInfo)
        {
            int totalEdges = adjacencyList.Values.Sum(list => list.Count);
            Debug.Log($"Graph built: {allPlatforms.Count} nodes, {totalEdges} edges");
        }
    }

    private void RefreshGraph()
    {
        if (Time.time - lastScanTime >= scanInterval)
        {
            bool hasDynamicChanges = false;

            foreach (var platform in allPlatforms)
            {
                if (platform.IsDynamic)
                {
                    hasDynamicChanges = true;
                    break;
                }
            }

            if (hasDynamicChanges && currentEnemy != null)
            {
                BuildGraph();
            }

            lastScanTime = Time.time;
        }
    }

    public List<PlatformNode> FindPath(Vector2 startPosition, PlatformNode targetPlatform)
    {
        if (!isGraphBuilt)
        {
            Debug.LogWarning("Graph not built yet!");
            return null;
        }

        if (currentEnemy == null)
        {
            Debug.LogWarning("No enemy reference!");
            return null;
        }

        PlatformNode startPlatform = GetNearestPlatform(startPosition);
        if (startPlatform == null || targetPlatform == null)
        {
            if (showDebugInfo) Debug.LogWarning("Start or target platform not found!");
            return null;
        }

        return FindPath(startPlatform, targetPlatform);
    }

    public List<PlatformNode> FindPath(PlatformNode start, PlatformNode target)
    {
        if (start == target)
        {
            return new List<PlatformNode> { start };
        }

        Queue<PlatformNode> queue = new Queue<PlatformNode>();
        Dictionary<PlatformNode, PlatformNode> cameFrom = new Dictionary<PlatformNode, PlatformNode>();
        HashSet<PlatformNode> visited = new HashSet<PlatformNode>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            PlatformNode current = queue.Dequeue();

            if (current == target)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in current.reachablePlatforms)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"No path found from {start.name} to {target.name}");
        }
        return null;
    }

    private List<PlatformNode> ReconstructPath(Dictionary<PlatformNode, PlatformNode> cameFrom, PlatformNode current)
    {
        List<PlatformNode> path = new List<PlatformNode>();
        path.Add(current);

        while (cameFrom[current] != null)
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    public PlatformNode GetNearestPlatform(Vector2 position)
    {
        PlatformNode nearest = null;
        float minDistance = float.MaxValue;

        foreach (var platform in allPlatforms)
        {
            float distance = Vector2.Distance(position, platform.GetLandingPoint());
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = platform;
            }
        }

        return nearest;
    }

    public PlatformNode GetPlatformAtPosition(Vector2 position)
    {
        foreach (var platform in allPlatforms)
        {
            if (platform.IsPositionOnPlatform(position))
            {
                return platform;
            }
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !isGraphBuilt) return;

        foreach (var pair in adjacencyList)
        {
            if (pair.Key == null) continue;

            Gizmos.color = Color.cyan;
            foreach (var neighbor in pair.Value)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(pair.Key.GetLandingPoint(), neighbor.GetLandingPoint());
                }
            }
        }
    }
}