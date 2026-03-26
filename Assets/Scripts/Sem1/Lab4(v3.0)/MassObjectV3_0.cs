using UnityEngine;

public class MassObjectV3_0 : MonoBehaviour
{
    [Header("Параметры объекта")]
    public float mass = 40f;              // Масса объекта в кг
    public Material normalMat;           // Материал в обычном состоянии
    public Material selectedMat;         // Материал при выделении

    private Renderer objectRenderer;     // Компонент для рендеринга

    void Start()
    {
        // Инициализация компонентов
        objectRenderer = GetComponent<Renderer>();
        var rb = GetComponent<Rigidbody>();
        
        rb.mass = mass;                  // Установка массы
        rb.isKinematic = true;           // Ручное управление движением
        rb.useGravity = false;           // Гравитация не нужна
    }

    /// <summary>
    /// Перемещает объект по радиусу платформы
    /// </summary>
    /// <param name="distance">Изменение радиуса (положительное - наружу, отрицательное - внутрь)</param>
    public void MoveRadial(float distance)
    {
        Vector3 pos = transform.localPosition;
        
        // Текущий угол относительно центра
        float angle = Mathf.Atan2(pos.z, pos.x);
        
        // Текущий радиус
        float currentRadius = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
        
        // Новый радиус с ограничениями
        float newRadius = Mathf.Clamp(currentRadius + distance, 1.5f, 2.2f);
        
        // Пересчёт координат
        pos.x = Mathf.Cos(angle) * newRadius;
        pos.z = Mathf.Sin(angle) * newRadius;
        
        transform.localPosition = pos;
        
        // Логирование для отладки
        Debug.Log($"Объект: радиус {currentRadius:0.00} → {newRadius:0.00}");
    }

    /// <summary>
    /// Устанавливает состояние выделения объекта
    /// </summary>
    /// <param name="selected">true - объект выделен, false - обычное состояние</param>
    public void SetSelected(bool selected)
    {
        objectRenderer.material = selected ? selectedMat : normalMat;
    }
}