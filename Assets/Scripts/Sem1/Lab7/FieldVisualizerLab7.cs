using UnityEngine;

// Рисует стрелки электрического поля в редакторе Unity
public class FieldVisualizerLab7 : MonoBehaviour
{
    // Система для расчета поля
    public ElectricFieldSystem fieldSystem;

    // Настройки сетки для визуализации
    public float areaSize = 20f;     // Размер области от центра
    public float gridStep = 1f;     // Шаг между точками
    public float arrowSize = 1f;  // Длина стрелок

    void Update()
    {
        if (fieldSystem == null) return;

        // Проходим по всем точкам сетки
        for (float x = -areaSize; x <= areaSize; x += gridStep)
        {
            for (float y = -areaSize; y <= areaSize; y += gridStep)
            {
                Vector3 gridPoint = new Vector3(x, y, 0);
                Vector3 field = fieldSystem.GetTotalFieldAt(gridPoint);

                // Рисуем стрелку только если поле достаточно сильное
                if (field.magnitude > 0.001f)
                {
                    Debug.DrawRay(
                        gridPoint,
                        field.normalized * arrowSize,
                        Color.aquamarine
                    );
                }
            }
        }
    }
}