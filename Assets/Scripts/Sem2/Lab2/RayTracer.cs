using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]  // требует наличия компонента LineRenderer на объекте
public class RayTracer : MonoBehaviour
{
    public enum TraceMode  // режимы трассировки
    {
        ReflectionOnly,    // только отражение
        RefractionOnly,    // только преломление
        FullOptics         // полная оптика (отражение + преломление)
    }

    [Serializable]
    public struct InteractionSample  // структура для записи каждого взаимодействия луча
    {
        public Vector3 point;               // точка пересечения с границей
        public float n1;                    // показатель преломления до границы
        public float n2;                    // показатель преломления после границы
        public float incidentAngle;         // угол падения (градусы)
        public float refractedAngle;        // угол преломления (градусы)
        public bool totalInternalReflection; // было ли полное внутреннее отражение
        public bool reflected;               // отразился ли луч
    }

    [Header("Source")]
    public Transform emitter;  // объект-излучатель (начальная точка и направление луча)

    [Header("Mode")]
    public TraceMode traceMode = TraceMode.FullOptics;  // текущий режим трассировки

    [Header("Ray Params")]
    [Min(1)] public int maxInteractions = 12;           // максимальное количество отражений/преломлений
    [Min(0.1f)] public float maxDistance = 30f;         // максимальная длина луча
    [Min(1f)] public float defaultRefractiveIndex = 1.0f; // показатель преломления по умолчанию (воздух)
    [Min(0.0001f)] public float rayOffset = 0.002f;     // смещение луча от поверхности (чтобы не залипать)

    [Header("Layers")]
    public LayerMask opticalMask = ~0;  // слои, с которыми взаимодействует луч

    [Header("Rendering")]
    public bool updateEveryFrame = true;        // обновлять луч каждый кадр
    public bool dimLineByIntensity = true;      // уменьшать яркость луча при поглощении
    public bool darkenColorByIntensity = true;  // затемнять цвет при поглощении
    [Range(0f, 1f)] public float minVisibleAlpha = 0.05f;  // минимальная видимая прозрачность

    [Header("Debug")]
    public bool drawDebugNormals = false;   // рисовать ли нормали к поверхностям
    [Min(0.01f)] public float normalLength = 0.35f;  // длина рисуемой нормали

    [Header("Results")]
    [Range(0f, 1f)] public float currentIntensity = 1f;  // текущая интенсивность луча
    public float lastIncidentAngle = 0f;       // последний угол падения
    public float lastRefractedAngle = 0f;      // последний угол преломления
    public bool lastWasTotalInternalReflection = false;  // было ли ПВО на последнем шаге

    [HideInInspector] public List<InteractionSample> lastSamples = new List<InteractionSample>(); // история взаимодействий

    private readonly Collider[] overlapBuffer = new Collider[24];  // буфер для Physics.OverlapSphere
    private readonly List<Vector3> pointsBuffer = new List<Vector3>(32);  // буфер точек для LineRenderer
    private LineRenderer lineRenderer;  // компонент для отрисовки луча

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();  // получаем компонент LineRenderer
    }

    private void Start()
    {
        TraceRay();  // запускаем трассировку при старте
    }

    private void Update()
    {
        if (updateEveryFrame)  // если нужно обновлять каждый кадр
        {
            TraceRay();  // пересчитываем луч
        }
    }

    [ContextMenu("Trace Ray")]  // можно вызвать из контекстного меню компонента
    public void TraceRay()  // главный метод трассировки
    {
        if (emitter == null)  // если излучатель не задан
        {
            lineRenderer.positionCount = 0;  // очищаем линию
            return;
        }

        pointsBuffer.Clear();  // очищаем буфер точек
        lastSamples.Clear();   // очищаем историю

        Vector3 origin = emitter.position;  // начальная точка луча
        Vector3 direction = emitter.forward.normalized;  // начальное направление луча

        OpticalMedium currentMedium = FindMediumAtPoint(origin, null);  // находим среду в начальной точке
        float currentN = currentMedium != null ? currentMedium.refractiveIndex : defaultRefractiveIndex;  // показатель преломления текущей среды
        float intensity = 1f;  // начальная интенсивность (максимальная)

        pointsBuffer.Add(origin);  // добавляем начальную точку

        for (int i = 0; i < maxInteractions; i++)  // цикл по всем взаимодействиям
        {
            bool hasHit = false;  // было ли попадание
            Vector3 hitPoint = Vector3.zero;  // точка попадания
            Vector3 hitNormal = Vector3.up;   // нормаль в точке попадания
            float hitDistance = 0f;            // расстояние до попадания
            Collider hitCollider = null;       // коллайдер, в который попали

            // Scene hit - обычный рейкаст по сцене
            if (Physics.Raycast(origin, direction, out RaycastHit worldHit, maxDistance, opticalMask, QueryTriggerInteraction.Ignore))
            {
                hasHit = true;
                hitPoint = worldHit.point;
                hitNormal = worldHit.normal;
                hitDistance = worldHit.distance;
                hitCollider = worldHit.collider;
            }

            // Exit hit from current medium - выход из текущей среды (приоритет если ближе)
            if (currentMedium != null && TryGetMediumExitHit(currentMedium, origin, direction, maxDistance, out Vector3 exitPoint, out Vector3 exitNormal, out float exitDistance, out Collider exitCollider))
            {
                if (!hasHit || exitDistance < hitDistance)  // если выход ближе, чем обычный хит
                {
                    hasHit = true;
                    hitPoint = exitPoint;
                    hitNormal = exitNormal;
                    hitDistance = exitDistance;
                    hitCollider = exitCollider;
                }
            }

            if (!hasHit)  // если никуда не попали
            {
                pointsBuffer.Add(origin + direction * maxDistance);  // добавляем конечную точку на максимальном расстоянии
                break;  // выходим из цикла
            }

            if (hitDistance <= rayOffset * 0.5f)  // если попали слишком близко (залипание)
            {
                origin += direction * (rayOffset * 2f);  // смещаемся вперёд
                i--;  // повторяем итерацию
                continue;
            }

            if (currentMedium != null && currentMedium.absorption > 0f)  // если среда поглощает свет
            {
                intensity *= Mathf.Exp(-currentMedium.absorption * hitDistance);  // экспоненциальное ослабление
            }

            pointsBuffer.Add(hitPoint);  // добавляем точку попадания

            OpticalMedium boundaryMedium = hitCollider != null ? hitCollider.GetComponent<OpticalMedium>() : null;  // среда на границе

            Vector3 orientedNormal = hitNormal;  // ориентируем нормаль
            if (Vector3.Dot(direction, orientedNormal) > 0f)  // если луч идёт с той же стороны, что и нормаль
            {
                orientedNormal = -orientedNormal;  // разворачиваем нормаль (луч падает изнутри)
            }

            if (drawDebugNormals)  // если нужно рисовать нормали для отладки
            {
                Debug.DrawRay(hitPoint, orientedNormal * normalLength, Color.green);  // рисуем нормаль
            }

            float n1 = currentN;  // показатель преломления до границы
            float n2 = n1;         // показатель преломления после границы (пока неизвестен)
            bool hasRefractionBoundary = boundaryMedium != null;  // есть ли на границе среда для преломления
            bool exitingCurrentMedium = false;  // выходим ли из текущей среды

            if (hasRefractionBoundary)  // если есть среда на границе
            {
                exitingCurrentMedium = currentMedium != null && boundaryMedium == currentMedium;  // проверяем, выходим ли мы из текущей среды
                if (exitingCurrentMedium)  // если выходим из текущей среды
                {
                    OpticalMedium mediumAfter = FindMediumAtPoint(hitPoint + direction * (rayOffset * 4f), boundaryMedium);  // ищем среду после границы
                    n2 = mediumAfter != null ? mediumAfter.refractiveIndex : Mathf.Max(1f, boundaryMedium.externalRefractiveIndex);  // показатель после
                }
                else  // если входим в новую среду
                {
                    n2 = boundaryMedium.refractiveIndex;  // берём показатель среды
                }
            }

            float incidentAngle = Vector3.Angle(-direction, orientedNormal);  // угол падения (между лучом и нормалью)
            float refractedAngle = 0f;   // угол преломления (пока 0)
            bool tir = false;             // полное внутреннее отражение (пока false)
            bool reflected = false;       // отразился ли луч (пока false)

            if (traceMode == TraceMode.ReflectionOnly)  // режим только отражение
            {
                reflected = true;
                direction = Vector3.Reflect(direction, orientedNormal).normalized;  // отражаем луч
            }
            else if (!hasRefractionBoundary)  // нет границы для преломления
            {
                if (traceMode == TraceMode.RefractionOnly)  // если режим только преломление, но границы нет
                {
                    break;  // выходим
                }

                reflected = true;
                direction = Vector3.Reflect(direction, orientedNormal).normalized;  // просто отражаем
            }
            else  // есть граница и режим полной оптики
            {
                Vector3 refracted = Refract(direction, orientedNormal, n1, n2, out tir);  // пытаемся преломить
                if (tir)  // если полное внутреннее отражение
                {
                    reflected = true;
                    direction = Vector3.Reflect(direction, orientedNormal).normalized;  // отражаем вместо преломления
                }
                else  // обычное преломление
                {
                    direction = refracted;  // устанавливаем преломлённое направление
                    refractedAngle = Vector3.Angle(direction, -orientedNormal);  // вычисляем угол преломления
                    currentN = n2;  // обновляем текущий показатель преломления
                    currentMedium = exitingCurrentMedium ? FindMediumAtPoint(hitPoint + direction * (rayOffset * 4f), boundaryMedium) : boundaryMedium;  // обновляем текущую среду
                }
            }

            lastSamples.Add(new InteractionSample  // сохраняем информацию о взаимодействии
            {
                point = hitPoint,
                n1 = n1,
                n2 = hasRefractionBoundary ? n2 : n1,
                incidentAngle = incidentAngle,
                refractedAngle = refractedAngle,
                totalInternalReflection = tir,
                reflected = reflected,
            });

            lastIncidentAngle = incidentAngle;  // сохраняем последний угол падения
            lastRefractedAngle = refractedAngle;  // сохраняем последний угол преломления
            lastWasTotalInternalReflection = tir;  // сохраняем было ли ПВО

            origin = hitPoint + direction * rayOffset;  // смещаем начало луча за границу

            if (intensity <= 0.01f)  // если интенсивность слишком маленькая
            {
                break;  // прекращаем трассировку
            }
        }

        lineRenderer.positionCount = pointsBuffer.Count;  // устанавливаем количество точек в LineRenderer
        for (int i = 0; i < pointsBuffer.Count; i++)  // для каждой точки
        {
            lineRenderer.SetPosition(i, pointsBuffer[i]);  // устанавливаем позицию
        }

        if (dimLineByIntensity)  // если нужно затемнять линию по интенсивности
        {
            Color baseColor = lineRenderer.startColor;  // базовый цвет линии
            float k = Mathf.Clamp01(intensity);  // коэффициент интенсивности (0..1)
            Color c = darkenColorByIntensity  // если затемнять цвет
                ? new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, Mathf.Max(minVisibleAlpha, k))  // умножаем RGB на интенсивность
                : new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Max(minVisibleAlpha, k));  // меняем только прозрачность
            lineRenderer.startColor = c;  // применяем цвет к началу линии
            lineRenderer.endColor = c;    // применяем цвет к концу линии
        }

        currentIntensity = intensity;  // сохраняем текущую интенсивность
    }

    public bool TryGetFirstRefractionSample(out InteractionSample sample)  // получить первый образец преломления
    {
        for (int i = 0; i < lastSamples.Count; i++)  // проходим по всем образцам
        {
            InteractionSample s = lastSamples[i];
            if (!s.reflected || s.totalInternalReflection)  // если не отражение или ПВО
            {
                sample = s;  // возвращаем образец
                return true;
            }
        }

        sample = default;  // если не нашли
        return false;
    }

    public float GetCriticalAngle(float n1, float n2)  // вычисление критического угла для ПВО
    {
        if (n1 <= n2)  // полное внутреннее отражение возможно только когда n1 > n2
        {
            return -1f;  // возвращаем -1 (невозможно)
        }

        return Mathf.Asin(n2 / n1) * Mathf.Rad2Deg;  // θ_крит = arcsin(n2/n1)
    }

    private OpticalMedium FindMediumAtPoint(Vector3 point, OpticalMedium excluded)  // поиск среды в точке
    {
        int count = Physics.OverlapSphereNonAlloc(point, rayOffset * 2f, overlapBuffer, opticalMask, QueryTriggerInteraction.Ignore);  // ищем коллайдеры в точке
        for (int i = 0; i < count; i++)  // для каждого найденного коллайдера
        {
            if (overlapBuffer[i] == null)  // если коллайдер невалидный
            {
                continue;
            }

            OpticalMedium medium = overlapBuffer[i].GetComponent<OpticalMedium>();  // получаем компонент OpticalMedium
            if (medium != null && medium != excluded)  // если среда существует и не исключена
            {
                return medium;  // возвращаем её
            }
        }

        return null;  // среды не найдено
    }

    private bool TryGetMediumExitHit(  // поиск точки выхода из среды (для сферических и боксовых коллайдеров)
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

        if (medium == null || !medium.TryGetComponent(out Collider collider))  // если нет коллайдера
        {
            return false;
        }

        // прямой рейкаст через коллайдер
        if (collider.Raycast(new Ray(origin, direction), out RaycastHit directHit, maxDist) && directHit.distance > rayOffset * 0.5f)
        {
            point = directHit.point;
            normal = directHit.normal;
            distance = directHit.distance;
            hitCollider = directHit.collider;
            return true;
        }

        // для BoxCollider - ручной расчёт выхода (более точный)
        if (collider is not BoxCollider box)
        {
            return false;
        }

        Transform t = box.transform;
        Vector3 localOrigin = t.InverseTransformPoint(origin) - box.center;  // начало луча в локальных координатах бокса
        Vector3 localDir = t.InverseTransformDirection(direction).normalized;  // направление в локальных координатах
        Vector3 half = box.size * 0.5f;  // половинные размеры бокса

        float tMin = float.NegativeInfinity;  // минимальный параметр входа
        float tMax = float.PositiveInfinity;  // максимальный параметр выхода
        int exitAxis = -1;  // ось выхода
        float exitSign = 0f;  // знак выхода

        // проверка по каждой оси (X, Y, Z)
        if (!UpdateSlab(localOrigin.x, localDir.x, -half.x, half.x, ref tMin, ref tMax, 0, ref exitAxis, ref exitSign)) return false;
        if (!UpdateSlab(localOrigin.y, localDir.y, -half.y, half.y, ref tMin, ref tMax, 1, ref exitAxis, ref exitSign)) return false;
        if (!UpdateSlab(localOrigin.z, localDir.z, -half.z, half.z, ref tMin, ref tMax, 2, ref exitAxis, ref exitSign)) return false;

        if (tMax <= rayOffset * 0.5f || tMax > maxDist)  // если выход слишком близко или далеко
        {
            return false;
        }

        Vector3 localPoint = localOrigin + localDir * tMax + box.center;  // точка выхода в локальных координатах
        Vector3 worldPoint = t.TransformPoint(localPoint);  // точка выхода в мировых координатах

        Vector3 localNormal = Vector3.zero;  // нормаль в локальных координатах
        if (exitAxis == 0) localNormal = new Vector3(exitSign, 0f, 0f);  // нормаль по X
        if (exitAxis == 1) localNormal = new Vector3(0f, exitSign, 0f);  // нормаль по Y
        if (exitAxis == 2) localNormal = new Vector3(0f, 0f, exitSign);  // нормаль по Z

        point = worldPoint;
        normal = t.TransformDirection(localNormal).normalized;  // нормаль в мировых координатах
        distance = Vector3.Distance(origin, worldPoint);  // расстояние до выхода
        hitCollider = collider;
        return true;
    }

    private static bool UpdateSlab(  // вспомогательный метод для расчёта пересечения с "плитой" (для BoxCollider)
        float ro,   // начало луча на оси
        float rd,   // направление луча на оси
        float min,  // минимальная граница
        float max,  // максимальная граница
        ref float tMin,  // минимальный параметр входа
        ref float tMax,  // максимальный параметр выхода
        int axis,  // текущая ось
        ref int exitAxis,  // ось выхода
        ref float exitSign)  // знак выхода
    {
        const float eps = 1e-6f;
        if (Mathf.Abs(rd) < eps)  // если луч параллелен оси
        {
            return ro >= min && ro <= max;  // проверяем, находится ли начало внутри
        }

        float t1 = (min - ro) / rd;  // параметр входа
        float t2 = (max - ro) / rd;  // параметр выхода
        float near = Mathf.Min(t1, t2);  // ближнее пересечение
        float far = Mathf.Max(t1, t2);   // дальнее пересечение

        if (near > tMin)  // обновляем ближний параметр
        {
            tMin = near;
        }

        if (far < tMax)  // обновляем дальний параметр и запоминаем ось выхода
        {
            tMax = far;
            exitAxis = axis;
            exitSign = t1 > t2 ? -1f : 1f;
        }

        return tMin <= tMax;  // пересечение существует, если tMin <= tMax
    }

    private static Vector3 Refract(Vector3 incident, Vector3 normal, float n1, float n2, out bool totalInternalReflection)  // расчёт преломлённого луча
    {
        incident.Normalize();  // нормализуем падающий луч
        normal.Normalize();    // нормализуем нормаль

        float eta = n1 / n2;  // отношение показателей преломления
        float cosI = -Vector3.Dot(normal, incident);  // косинус угла падения
        float sinT2 = eta * eta * (1f - cosI * cosI);  // квадрат синуса угла преломления

        if (sinT2 > 1f)  // если синус > 1 — полное внутреннее отражение
        {
            totalInternalReflection = true;
            return Vector3.zero;
        }

        float cosT = Mathf.Sqrt(1f - sinT2);  // косинус угла преломления
        Vector3 refracted = eta * incident + (eta * cosI - cosT) * normal;  // формула преломления

        totalInternalReflection = false;
        return refracted.normalized;  // возвращаем нормализованный преломлённый луч
    }
}