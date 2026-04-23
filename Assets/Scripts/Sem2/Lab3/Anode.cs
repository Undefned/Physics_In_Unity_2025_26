using UnityEngine;
using System.Collections.Generic;

public class Anode : MonoBehaviour
{
    [Header("Измерения")]
    public float current = 0f; // ток в условных единицах
    public List<float> currentHistory = new List<float>();
    
    private float electronCount = 0f;
    private float timer = 0f;
    private const float MEASURE_INTERVAL = 0.1f; // измеряем ток каждые 0.1 сек
    
    void Start()
    {
        gameObject.tag = "Anode";
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= MEASURE_INTERVAL)
        {
            // Ток пропорционален количеству электронов в секунду
            current = electronCount / MEASURE_INTERVAL;
            currentHistory.Add(current);
            
            // Ограничиваем историю для графика
            while (currentHistory.Count > 100)
                currentHistory.RemoveAt(0);
            
            electronCount = 0f;
            timer = 0f;
        }
    }
    
    public void CollectElectron()
    {
        electronCount++;
        Debug.Log($"[Anode] Electron collected! Count={electronCount}, current={current}");
    }
    
    public float GetCurrent() => current;
    
    public List<float> GetCurrentHistory() => currentHistory;
    
    public void ResetCurrent()
    {
        electronCount = 0f;
        current = 0f;
        currentHistory.Clear();
    }
}