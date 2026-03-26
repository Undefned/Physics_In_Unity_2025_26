using UnityEngine;

public class FieldVisualizer : MonoBehaviour
{
    [Header("Настройки поля")]
    public Vector3 fieldDirection = Vector3.forward;
    public float fieldStrength = 1f;
    public int lineCount = 10;
    public float lineLength = 10f;
    public float lineSpacing = 1f;
    
    [Header("Визуализация")]
    public Color fieldColor = Color.cyan;

    void Update()
    {
        DrawFieldLines();
    }
    
    void DrawFieldLines()
    {
        Vector3 startPos = transform.position - new Vector3(lineSpacing * lineCount / 2f, 0f, 0f);
        
        for (int i = 0; i < lineCount; i++)
        {
            Vector3 lineStart = startPos + new Vector3(i * lineSpacing, 0f, 0f);
            Vector3 lineEnd = lineStart + fieldDirection.normalized * lineLength * fieldStrength;
            
            Debug.DrawLine(lineStart, lineEnd, fieldColor);
            
            // Стрелочки направления (опционально)
            DrawArrow(lineEnd, fieldDirection.normalized, 0.5f, fieldColor);
        }
    }
    
    void DrawArrow(Vector3 position, Vector3 direction, float size, Color color)
    {
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 30, 0) * Vector3.back;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -30, 0) * Vector3.back;
        
        Debug.DrawRay(position, right * size, color);
        Debug.DrawRay(position, left * size, color);
    }
}