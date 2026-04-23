using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RayTracer : MonoBehaviour
{
    public enum TraceMode
    {
        ReflectionOnly,
        RefractionOnly,
        FullOptics
    }

    [Serializable]
    public struct InteractionSample
    {
        public Vector3 point;
        public float n1;
        public float n2;
        public float incidentAngle;
        public float refractedAngle;
        public bool totalInternalReflection;
        public bool reflected;
    }

    [Header("Source")]
    public Transform emitter;

    [Header("Mode")]
    public TraceMode traceMode = TraceMode.FullOptics;

    [Header("Ray Params")]
    [Min(1)] public int maxInteractions = 12;
    [Min(0.1f)] public float maxDistance = 60f;
    [Min(1f)] public float defaultRefractiveIndex = 1.0f;
    [Min(0.0001f)] public float rayOffset = 0.002f;

    [Header("Layers")]
    public LayerMask opticalMask = ~0;

    [Header("Rendering")]
    public bool updateEveryFrame = true;
    public bool dimLineByIntensity = true;
    public bool darkenColorByIntensity = true;
    [Range(0f, 1f)] public float minVisibleAlpha = 0.05f;

    [Header("Debug")]
    public bool drawDebugNormals = false;
    [Min(0.01f)] public float normalLength = 0.35f;

    [Header("Results")]
    [Range(0f, 1f)] public float currentIntensity = 1f;
    public float lastIncidentAngle = 0f;
    public float lastRefractedAngle = 0f;
    public bool lastWasTotalInternalReflection = false;

    [HideInInspector] public List<InteractionSample> lastSamples = new List<InteractionSample>();

    private readonly Collider[] overlapBuffer = new Collider[24];
    private readonly List<Vector3> pointsBuffer = new List<Vector3>(32);
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        TraceRay();
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            TraceRay();
        }
    }

    [ContextMenu("Trace Ray")]
    public void TraceRay()
    {
        if (emitter == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        pointsBuffer.Clear();
        lastSamples.Clear();

        Vector3 origin = emitter.position;
        Vector3 direction = emitter.forward.normalized;

        OpticalMedium currentMedium = FindMediumAtPoint(origin, null);
        float currentN = currentMedium != null ? currentMedium.refractiveIndex : defaultRefractiveIndex;
        float intensity = 1f;

        pointsBuffer.Add(origin);

        for (int i = 0; i < maxInteractions; i++)
        {
            bool hasHit = false;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up;
            float hitDistance = 0f;
            Collider hitCollider = null;

            // Scene hit.
            if (Physics.Raycast(origin, direction, out RaycastHit worldHit, maxDistance, opticalMask, QueryTriggerInteraction.Ignore))
            {
                hasHit = true;
                hitPoint = worldHit.point;
                hitNormal = worldHit.normal;
                hitDistance = worldHit.distance;
                hitCollider = worldHit.collider;
            }

            // Exit hit from current medium (priority when closer).
            if (currentMedium != null && TryGetMediumExitHit(currentMedium, origin, direction, maxDistance, out Vector3 exitPoint, out Vector3 exitNormal, out float exitDistance, out Collider exitCollider))
            {
                if (!hasHit || exitDistance < hitDistance)
                {
                    hasHit = true;
                    hitPoint = exitPoint;
                    hitNormal = exitNormal;
                    hitDistance = exitDistance;
                    hitCollider = exitCollider;
                }
            }

            if (!hasHit)
            {
                pointsBuffer.Add(origin + direction * maxDistance);
                break;
            }

            if (hitDistance <= rayOffset * 0.5f)
            {
                origin += direction * (rayOffset * 2f);
                i--;
                continue;
            }

            if (currentMedium != null && currentMedium.absorption > 0f)
            {
                intensity *= Mathf.Exp(-currentMedium.absorption * hitDistance);
            }

            pointsBuffer.Add(hitPoint);

            OpticalMedium boundaryMedium = hitCollider != null ? hitCollider.GetComponent<OpticalMedium>() : null;

            Vector3 orientedNormal = hitNormal;
            if (Vector3.Dot(direction, orientedNormal) > 0f)
            {
                orientedNormal = -orientedNormal;
            }

            if (drawDebugNormals)
            {
                Debug.DrawRay(hitPoint, orientedNormal * normalLength, Color.green);
            }

            float n1 = currentN;
            float n2 = n1;
            bool hasRefractionBoundary = boundaryMedium != null;
            bool exitingCurrentMedium = false;

            if (hasRefractionBoundary)
            {
                exitingCurrentMedium = currentMedium != null && boundaryMedium == currentMedium;
                if (exitingCurrentMedium)
                {
                    OpticalMedium mediumAfter = FindMediumAtPoint(hitPoint + direction * (rayOffset * 4f), boundaryMedium);
                    n2 = mediumAfter != null ? mediumAfter.refractiveIndex : Mathf.Max(1f, boundaryMedium.externalRefractiveIndex);
                }
                else
                {
                    n2 = boundaryMedium.refractiveIndex;
                }
            }

            float incidentAngle = Vector3.Angle(-direction, orientedNormal);
            float refractedAngle = 0f;
            bool tir = false;
            bool reflected = false;

            if (traceMode == TraceMode.ReflectionOnly)
            {
                reflected = true;
                direction = Vector3.Reflect(direction, orientedNormal).normalized;
            }
            else if (!hasRefractionBoundary)
            {
                if (traceMode == TraceMode.RefractionOnly)
                {
                    break;
                }

                reflected = true;
                direction = Vector3.Reflect(direction, orientedNormal).normalized;
            }
            else
            {
                Vector3 refracted = Refract(direction, orientedNormal, n1, n2, out tir);
                if (tir)
                {
                    reflected = true;
                    direction = Vector3.Reflect(direction, orientedNormal).normalized;
                }
                else
                {
                    direction = refracted;
                    refractedAngle = Vector3.Angle(direction, -orientedNormal);
                    currentN = n2;
                    currentMedium = exitingCurrentMedium ? FindMediumAtPoint(hitPoint + direction * (rayOffset * 4f), boundaryMedium) : boundaryMedium;
                }
            }

            lastSamples.Add(new InteractionSample
            {
                point = hitPoint,
                n1 = n1,
                n2 = hasRefractionBoundary ? n2 : n1,
                incidentAngle = incidentAngle,
                refractedAngle = refractedAngle,
                totalInternalReflection = tir,
                reflected = reflected,
            });

            lastIncidentAngle = incidentAngle;
            lastRefractedAngle = refractedAngle;
            lastWasTotalInternalReflection = tir;

            origin = hitPoint + direction * rayOffset;

            if (intensity <= 0.01f)
            {
                break;
            }
        }

        lineRenderer.positionCount = pointsBuffer.Count;
        for (int i = 0; i < pointsBuffer.Count; i++)
        {
            lineRenderer.SetPosition(i, pointsBuffer[i]);
        }

        if (dimLineByIntensity)
        {
            Color baseColor = lineRenderer.startColor;
            float k = Mathf.Clamp01(intensity);
            Color c = darkenColorByIntensity
                ? new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, Mathf.Max(minVisibleAlpha, k))
                : new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Max(minVisibleAlpha, k));
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
        }

        currentIntensity = intensity;
    }

    public bool TryGetFirstRefractionSample(out InteractionSample sample)
    {
        for (int i = 0; i < lastSamples.Count; i++)
        {
            InteractionSample s = lastSamples[i];
            if (!s.reflected || s.totalInternalReflection)
            {
                sample = s;
                return true;
            }
        }

        sample = default;
        return false;
    }

    public float GetCriticalAngle(float n1, float n2)
    {
        if (n1 <= n2)
        {
            return -1f;
        }

        return Mathf.Asin(n2 / n1) * Mathf.Rad2Deg;
    }

    private OpticalMedium FindMediumAtPoint(Vector3 point, OpticalMedium excluded)
    {
        int count = Physics.OverlapSphereNonAlloc(point, rayOffset * 2f, overlapBuffer, opticalMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] == null)
            {
                continue;
            }

            OpticalMedium medium = overlapBuffer[i].GetComponent<OpticalMedium>();
            if (medium != null && medium != excluded)
            {
                return medium;
            }
        }

        return null;
    }

    private bool TryGetMediumExitHit(
        OpticalMedium medium,
        Vector3 origin,
        Vector3 direction,
        float maxDist,
        out Vector3 point,
        out Vector3 normal,
        out float distance,
        out Collider hitCollider)
    {
        point = default;
        normal = default;
        distance = 0f;
        hitCollider = null;

        if (medium == null || !medium.TryGetComponent(out Collider collider))
        {
            return false;
        }

        if (collider.Raycast(new Ray(origin, direction), out RaycastHit directHit, maxDist) && directHit.distance > rayOffset * 0.5f)
        {
            point = directHit.point;
            normal = directHit.normal;
            distance = directHit.distance;
            hitCollider = directHit.collider;
            return true;
        }

        if (collider is not BoxCollider box)
        {
            return false;
        }

        Transform t = box.transform;
        Vector3 localOrigin = t.InverseTransformPoint(origin) - box.center;
        Vector3 localDir = t.InverseTransformDirection(direction).normalized;
        Vector3 half = box.size * 0.5f;

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;
        int exitAxis = -1;
        float exitSign = 0f;

        if (!UpdateSlab(localOrigin.x, localDir.x, -half.x, half.x, ref tMin, ref tMax, 0, ref exitAxis, ref exitSign)) return false;
        if (!UpdateSlab(localOrigin.y, localDir.y, -half.y, half.y, ref tMin, ref tMax, 1, ref exitAxis, ref exitSign)) return false;
        if (!UpdateSlab(localOrigin.z, localDir.z, -half.z, half.z, ref tMin, ref tMax, 2, ref exitAxis, ref exitSign)) return false;

        if (tMax <= rayOffset * 0.5f || tMax > maxDist)
        {
            return false;
        }

        Vector3 localPoint = localOrigin + localDir * tMax + box.center;
        Vector3 worldPoint = t.TransformPoint(localPoint);

        Vector3 localNormal = Vector3.zero;
        if (exitAxis == 0) localNormal = new Vector3(exitSign, 0f, 0f);
        if (exitAxis == 1) localNormal = new Vector3(0f, exitSign, 0f);
        if (exitAxis == 2) localNormal = new Vector3(0f, 0f, exitSign);

        point = worldPoint;
        normal = t.TransformDirection(localNormal).normalized;
        distance = Vector3.Distance(origin, worldPoint);
        hitCollider = collider;
        return true;
    }

    private static bool UpdateSlab(
        float ro,
        float rd,
        float min,
        float max,
        ref float tMin,
        ref float tMax,
        int axis,
        ref int exitAxis,
        ref float exitSign)
    {
        const float eps = 1e-6f;
        if (Mathf.Abs(rd) < eps)
        {
            return ro >= min && ro <= max;
        }

        float t1 = (min - ro) / rd;
        float t2 = (max - ro) / rd;
        float near = Mathf.Min(t1, t2);
        float far = Mathf.Max(t1, t2);

        if (near > tMin)
        {
            tMin = near;
        }

        if (far < tMax)
        {
            tMax = far;
            exitAxis = axis;
            exitSign = t1 > t2 ? -1f : 1f;
        }

        return tMin <= tMax;
    }

    private static Vector3 Refract(Vector3 incident, Vector3 normal, float n1, float n2, out bool totalInternalReflection)
    {
        incident.Normalize();
        normal.Normalize();

        float eta = n1 / n2;
        float cosI = -Vector3.Dot(normal, incident);
        float sinT2 = eta * eta * (1f - cosI * cosI);

        if (sinT2 > 1f)
        {
            totalInternalReflection = true;
            return Vector3.zero;
        }

        float cosT = Mathf.Sqrt(1f - sinT2);
        Vector3 refracted = eta * incident + (eta * cosI - cosT) * normal;

        totalInternalReflection = false;
        return refracted.normalized;
    }
}
