using System.Collections.Generic;
using UnityEngine;

public class PlatformNode : MonoBehaviour
{
    [Header("Связи")]
    public List<PlatformNode> reachablePlatforms; // Платформы, куда можно допрыгнуть

    public Bounds Bounds => GetComponent<Collider2D>().bounds;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var platform in reachablePlatforms)
        {
            if (platform != null)
                Gizmos.DrawLine(transform.position, platform.transform.position);
        }
    }
}