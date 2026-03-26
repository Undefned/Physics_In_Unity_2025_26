
using UnityEngine;

// Точечный электрический заряд - источник поля
public class Charge : MonoBehaviour
{
    // Величина заряда (положительная или отрицательная)
    public float chargeValue = 1f;

    // Вычисляет напряженность поля в заданной точке
    public Vector3 CalculateFieldAtPoint(Vector3 point)
    {
        // Вектор от заряда к точке
        Vector3 direction = point - transform.position;
        
        // Квадрат расстояния с защитой от деления на ноль
        float distanceSquared = direction.sqrMagnitude + 0.01f;
        
        // Напряженность поля точечного заряда
        // E = q * (r / |r|) / r²
        return chargeValue * direction.normalized / distanceSquared;
    }
}
