using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GraphDrawer : MonoBehaviour
{
    [Header("График")]
    public RectTransform graphArea; // область графика
    public GameObject pointPrefab; // префаб точки
    public LineRenderer lineRenderer; // для линии графика
    
    [Header("Данные")]
    public Anode anode;
    public Slider voltageSlider;
    
    private List<Vector2> dataPoints = new List<Vector2>();
    private List<Image> points = new List<Image>();
    
    [Header("Настройки")]
    public float maxVoltage = 5f;
    public float maxCurrent = 10f;
    public bool autoCapture = true;
    
    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }
    
    public void AddPoint(float voltage, float current)
    {
        dataPoints.Add(new Vector2(voltage, current));
        
        if (autoCapture)
            UpdateGraph();
    }
    
    public void UpdateGraph()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = dataPoints.Count;
            for (int i = 0; i < dataPoints.Count; i++)
            {
                // Преобразуем координаты в локальные координаты графика
                Vector2 normalizedPos = new Vector2(
                    (dataPoints[i].x + maxVoltage) / (2 * maxVoltage),
                    dataPoints[i].y / maxCurrent
                );
                
                Vector3 worldPos = graphArea.TransformPoint(
                    new Vector3(normalizedPos.x * graphArea.rect.width,
                                normalizedPos.y * graphArea.rect.height,
                                0)
                );
                
                lineRenderer.SetPosition(i, worldPos);
            }
        }
    }
    
    public void ClearGraph()
    {
        dataPoints.Clear();
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
        
        // Удаляем точки-префабы
        foreach (var point in points)
            Destroy(point.gameObject);
        points.Clear();
    }
    
    public void CapturePoint()
    {
        if (voltageSlider != null && anode != null)
        {
            float voltage = voltageSlider.value;
            float current = anode.GetCurrent();
            AddPoint(voltage, current);
            
            Debug.Log($"Точка ВАХ: U={voltage:F2} V, I={current:F2}");
        }
    }
    
    public void BuildFullVAH()
    {
        ClearGraph();
        StartCoroutine(SweepVoltage());
    }
    
    System.Collections.IEnumerator SweepVoltage()
    {
        if (voltageSlider == null) yield break;
        
        for (float v = -maxVoltage; v <= maxVoltage; v += 0.5f)
        {
            voltageSlider.value = v;
            yield return new WaitForSeconds(0.2f); // ждём установления тока
            
            if (anode != null)
                AddPoint(v, anode.GetCurrent());
        }
    }
}