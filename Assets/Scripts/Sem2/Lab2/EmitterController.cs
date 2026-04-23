using UnityEngine;

// EmitterController управляет направлением источника луча.
// В лабораторной угол падения задается через incidentAngle в Inspector.
// Смена угла в реальном времени позволяет проверять отражение/преломление
// и находить критический угол для полного внутреннего отражения.
public class EmitterController : MonoBehaviour
{
    [Range(0f, 89f)]
    public float incidentAngle = 30f;

    [Header("Базовое направление")]
    public Vector3 baseDirection = Vector3.forward;

    [Header("Ось поворота")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Обновлять в реальном времени")]
    public bool updateEveryFrame = true;

    private void Start()
    {
        ApplyCurrentAngle();
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            // При включенном updateEveryFrame поворот пересчитывается каждый кадр,
            // чтобы луч сразу реагировал на изменения в Inspector/UI.
            ApplyCurrentAngle();
        }
    }

    public void ApplyCurrentAngle()
    {
        // Защита от нулевых векторов (чтобы не получать NaN/некорректный forward).
        Vector3 axis = rotationAxis.sqrMagnitude > 0f ? rotationAxis.normalized : Vector3.up;
        Vector3 dir = baseDirection.sqrMagnitude > 0f ? baseDirection.normalized : Vector3.forward;

        // Итоговое направление источника:
        // поворачиваем базовое направление на incidentAngle вокруг rotationAxis.
        transform.forward = Quaternion.AngleAxis(incidentAngle, axis) * dir;
    }
}
