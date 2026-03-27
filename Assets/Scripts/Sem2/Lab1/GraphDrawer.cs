using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GraphDrawer : MonoBehaviour
{
    [Header("График")]
    public RectTransform graphArea;      // Область для точек
    public GameObject pointPrefab;       // Префаб точки
    public LineRenderer lineRenderer;    // Опционально
    
    [Header("Данные")]
    public Anode anode;
    public Slider voltageSlider;
    
    private List<Vector2> dataPoints = new List<Vector2>();
    private List<GameObject> points = new List<GameObject>();
    private float maxCurrentSeen = 1f; // для автоподбора масштаба
    
    [Header("Настройки осей")]
    public float minVoltage = -5f;
    public float maxVoltage = 5f;
    public float maxCurrent = 100f; // увеличено для корректного отображения
    public bool autoCapture = true;
    public bool autoScaleCurrent = true; // автоподбор масштаба по Y
    
    void Start()
    {
        if (graphArea == null)
            Debug.LogError("GraphArea не назначен!");
        if (pointPrefab == null)
            Debug.LogError("PointPrefab не назначен!");

        Debug.Log($"[GraphDrawer] GraphArea size: {graphArea.rect.width} x {graphArea.rect.height}");
    }
    
    /// <summary>
    /// Преобразует координаты (напряжение, ток) в позицию внутри graphArea
    /// </summary>
    private Vector2 DataToUIPosition(float voltage, float current)
    {
        if (graphArea == null) return Vector2.zero;

        // Нормализуем значения от 0 до 1
        float normalizedX = (voltage - minVoltage) / (maxVoltage - minVoltage);
        
        // Автоподбор масштаба по Y или фиксированный
        float currentMax = autoScaleCurrent ? Mathf.Max(maxCurrentSeen, 1f) : maxCurrent;
        float normalizedY = current / currentMax;

        // Ограничиваем, чтобы точки не выходили за границы
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // Преобразуем в координаты внутри graphArea
        // anchoredPosition считается от центра родителя, поэтому нужно учесть pivot
        float x = (normalizedX - 0.5f) * graphArea.rect.width;
        float y = (normalizedY - 0.5f) * graphArea.rect.height;

        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Добавление точки на график
    /// </summary>
    public void AddPoint(float voltage, float current)
    {
        if (graphArea == null || pointPrefab == null)
        {
            Debug.LogError("GraphArea или PointPrefab не назначены!");
            return;
        }

        // Обновляем максимальный ток для автоподбора масштаба
        if (autoScaleCurrent && current > maxCurrentSeen)
        {
            maxCurrentSeen = current;
            Debug.Log($"[GraphDrawer] maxCurrentSeen обновлён: {maxCurrentSeen:F2}");
        }

        dataPoints.Add(new Vector2(voltage, current));

        // Создаём видимую точку как дочерний объект graphArea
        GameObject newPoint = Instantiate(pointPrefab, graphArea);

        // Настраиваем RectTransform точки
        RectTransform rect = newPoint.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Устанавливаем якоря в центр
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            // Получаем позицию в координатах graphArea
            Vector2 position = DataToUIPosition(voltage, current);

            // Устанавливаем позицию (anchoredPosition отсчитывается от центра родителя)
            rect.anchoredPosition = position;

            // Pivot в центре
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Размер точки
            rect.sizeDelta = new Vector2(8, 8);

            Debug.Log($"[GraphDrawer] Точка: U={voltage:F2} V, I={current:F2} | UI: ({position.x:F1}, {position.y:F1})");
        }

        points.Add(newPoint);
    }
    
    /// <summary>
    /// Обновление линии графика
    /// </summary>
    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.positionCount = dataPoints.Count;
        
        for (int i = 0; i < dataPoints.Count; i++)
        {
            Vector2 uiPos = DataToUIPosition(dataPoints[i].x, dataPoints[i].y);
            
            // Для LineRenderer в UI нужно преобразовывать координаты особым образом
            // Упрощённо: пока оставляем, но для 3D LineRenderer это сложнее
            // Рекомендую пока не использовать LineRenderer, только точки
        }
    }
    
    /// <summary>
    /// Очистка графика
    /// </summary>
    public void ClearGraph()
    {
        dataPoints.Clear();

        foreach (GameObject point in points)
        {
            Destroy(point);
        }
        points.Clear();
        
        // Сбрасываем максимальный ток при очистке
        if (autoScaleCurrent)
            maxCurrentSeen = 1f;

        Debug.Log("График очищен");
    }
    
    /// <summary>
    /// Добавить текущую точку
    /// </summary>
    public void CapturePoint()
    {
        if (voltageSlider != null && anode != null)
        {
            // Преобразуем нормализованное значение слайдера (0-1) в реальное напряжение
            float voltage = Mathf.Lerp(minVoltage, maxVoltage, voltageSlider.value);
            float current = anode.GetCurrent();
            Debug.Log($"[GraphDrawer] CapturePoint: voltage={voltage:F2}, current={current:F2}");
            AddPoint(voltage, current);
        }
        else
        {
            Debug.LogError("VoltageSlider или Anode не назначены!");
        }
    }
    
    /// <summary>
    /// Автоматическое построение всей ВАХ
    /// </summary>
    public void BuildFullVAH()
    {
        ClearGraph();
        StartCoroutine(SweepVoltage());
    }
    
    private System.Collections.IEnumerator SweepVoltage()
    {
        if (voltageSlider == null)
        {
            Debug.LogError("VoltageSlider не назначен!");
            yield break;
        }

        float originalVoltage = voltageSlider.value;

        for (float v = minVoltage; v <= maxVoltage; v += 0.5f)
        {
            // Преобразуем реальное напряжение в нормализованное значение слайдера
            float normalizedValue = Mathf.InverseLerp(minVoltage, maxVoltage, v);
            voltageSlider.value = normalizedValue;
            
            // Ждём пока ток стабилизируется
            yield return new WaitForSeconds(0.3f);

            if (anode != null)
            {
                float current = anode.GetCurrent();
                Debug.Log($"[GraphDrawer] SweepVoltage: v={v:F2}, current={current:F2}");
                AddPoint(v, current);
            }
        }

        voltageSlider.value = originalVoltage;
        Debug.Log("Построение ВАХ завершено");
    }
    
    /// <summary>
    /// Добавить тестовые точки (для проверки)
    /// </summary>
    [ContextMenu("Add Test Points")]
    public void AddTestPoints()
    {
        ClearGraph();
        AddPoint(-5f, 0f);
        AddPoint(-3f, 0.5f);
        AddPoint(-2f, 1f);
        AddPoint(-1f, 2f);
        AddPoint(0f, 4f);
        AddPoint(1f, 6f);
        AddPoint(2f, 7f);
        AddPoint(3f, 8f);
        AddPoint(4f, 9f);
        AddPoint(5f, 10f);
    }
}