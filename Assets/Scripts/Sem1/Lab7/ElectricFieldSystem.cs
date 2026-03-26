
using UnityEngine;

// Управляет всеми зарядами и рассчитывает общее поле
public class ElectricFieldSystem : MonoBehaviour
{
    // Все заряды на сцене
    public Charge[] allCharges;

    // Получить общую напряженность поля в точке
    public Vector3 GetTotalFieldAt(Vector3 point)
    {
        Vector3 totalField = Vector3.zero;

        // Суммируем поля от всех зарядов
        foreach (Charge charge in allCharges)
        {
            totalField += charge.CalculateFieldAtPoint(point);
        }

        return totalField;
    }
}
