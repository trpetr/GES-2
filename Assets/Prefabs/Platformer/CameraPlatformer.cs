using UnityEngine;

public class CameraPlatformer : MonoBehaviour
{

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Boom Settings")]
    [SerializeField] private float boomLength = 5f;
    [SerializeField] private float boomHeight = 2f;
    [SerializeField] private Vector3 boomOffset = Vector3.zero;

    [Header("Dead Zone")]
    [SerializeField] private bool enableDeadZone = true;
    [SerializeField] private Vector2 deadZoneSize = new Vector2(3f, 2f);
    [SerializeField] private float deadZoneDepth = 1.5f;
    [SerializeField] private bool snapOnStart = true;

    [Header("Lag / Smoothing")]
    [SerializeField] private float positionLagSpeed = 10f;
    [SerializeField] private float rotationLagSpeed = 8f;

    [Header("Collision")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private LayerMask collisionMask = -1;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float minBoomLength = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugLines = true;

    private Transform cameraTransform;
    private Vector3 currentVelocity;
    private Vector3 deadZoneCenter;
    private Vector3 targetOffsetFromCenter;
    private bool isDeadZoneInitialized;
    private RaycastHit[] hitBuffer = new RaycastHit[1];

    private void Awake()
    {
        cameraTransform = transform;
        currentVelocity = Vector3.zero;

#if UNITY_EDITOR
        if (target == null)
            Debug.LogError("CameraBoom: No target assigned!", this);
#endif
    }

    private void Start()
    {
        if (snapOnStart && target != null)
        {
            float height = boomHeight;
            Vector3 offset = boomOffset;

            if (offset != Vector3.zero)
                offset = target.TransformDirection(offset);

            Vector3 pivotPoint = target.position;
            pivotPoint.y += height;
            pivotPoint += offset;

            deadZoneCenter = pivotPoint;
            isDeadZoneInitialized = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;

        float height = boomHeight;
        Vector3 offset = boomOffset;

        if (offset != Vector3.zero)
            offset = target.TransformDirection(offset);

        Vector3 pivotPoint = targetPos;
        pivotPoint.y += height;
        pivotPoint += offset;

        Vector3 adjustedPivot = ApplyDeadZone(pivotPoint);

        Vector3 finalPos;

        if (enableCollision)
        {
            float adjustedLength = GetAdjustedBoomLength(adjustedPivot);
            finalPos = adjustedPivot - target.forward * adjustedLength;
        }
        else
        {
            finalPos = adjustedPivot - target.forward * boomLength;
        }

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            finalPos,
            ref currentVelocity,
            1f / positionLagSpeed,
            Mathf.Infinity,
            Time.deltaTime
        );

        Vector3 lookTarget = targetPos;
        lookTarget.y += height * 0.5f;

        Quaternion targetRot = Quaternion.LookRotation(lookTarget - cameraTransform.position);
        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRot,
            rotationLagSpeed * Time.deltaTime
        );
    }

    private Vector3 ApplyDeadZone(Vector3 currentPivot)
    {
        if (!enableDeadZone || !isDeadZoneInitialized)
        {
            if (!enableDeadZone)
                deadZoneCenter = currentPivot;
            return currentPivot;
        }

        Vector3 offset = currentPivot - deadZoneCenter;

        bool needsUpdate = false;
        Vector3 newCenter = deadZoneCenter;

        if (Mathf.Abs(offset.x) > deadZoneSize.x * 0.5f)
        {
            float sign = Mathf.Sign(offset.x);
            newCenter.x = currentPivot.x - sign * (deadZoneSize.x * 0.5f);
            needsUpdate = true;
        }

        if (Mathf.Abs(offset.y) > deadZoneSize.y * 0.5f)
        {
            float sign = Mathf.Sign(offset.y);
            newCenter.y = currentPivot.y - sign * (deadZoneSize.y * 0.5f);
            needsUpdate = true;
        }

        if (Mathf.Abs(offset.z) > deadZoneDepth * 0.5f)
        {
            float sign = Mathf.Sign(offset.z);
            newCenter.z = currentPivot.z - sign * (deadZoneDepth * 0.5f);
            needsUpdate = true;
        }

        if (needsUpdate)
            deadZoneCenter = newCenter;

        return deadZoneCenter;
    }

    private float GetAdjustedBoomLength(Vector3 pivotPoint)
    {
        Vector3 direction = -target.forward;
        float maxDistance = boomLength;

        int hits = Physics.SphereCastNonAlloc(
            pivotPoint,
            collisionRadius,
            direction,
            hitBuffer,
            maxDistance,
            collisionMask
        );

        if (hits > 0)
        {
            float safeDistance = hitBuffer[0].distance - collisionRadius;
            return safeDistance < minBoomLength ? minBoomLength : safeDistance;
        }

        return maxDistance;
    }

    public void ResetDeadZone()
    {
        if (target != null)
        {
            float height = boomHeight;
            Vector3 offset = boomOffset;

            if (offset != Vector3.zero)
                offset = target.TransformDirection(offset);

            Vector3 pivotPoint = target.position;
            pivotPoint.y += height;
            pivotPoint += offset;

            deadZoneCenter = pivotPoint;
            isDeadZoneInitialized = true;
        }
    }

    public void SetDeadZoneSize(Vector2 newSize, float newDepth)
    {
        deadZoneSize = newSize;
        deadZoneDepth = newDepth;
    }

    public Vector3 GetDeadZoneCenter()
    {
        return deadZoneCenter;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawDebugLines || target == null) return;

        Vector3 pivotPoint = target.position;
        pivotPoint.y += boomHeight;

        if (boomOffset != Vector3.zero)
            pivotPoint += target.TransformDirection(boomOffset);

        if (enableDeadZone && isDeadZoneInitialized)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(deadZoneCenter, new Vector3(deadZoneSize.x, deadZoneSize.y, deadZoneDepth));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(deadZoneCenter, 0.15f);

            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawLine(deadZoneCenter, pivotPoint);
        }

        Vector3 camPos = pivotPoint - target.forward * (enableCollision ? minBoomLength : boomLength);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(pivotPoint, camPos);
        Gizmos.DrawWireSphere(pivotPoint, 0.1f);
        Gizmos.DrawWireSphere(camPos, collisionRadius);

        if (enableCollision)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pivotPoint, -target.forward * boomLength);
        }
    }
#endif
}
