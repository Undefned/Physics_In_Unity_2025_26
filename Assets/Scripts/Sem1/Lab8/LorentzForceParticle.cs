using UnityEngine;
using TMPro;

public class LorentzForceParticle : MonoBehaviour
{
    [Header("Параметры")]
    public float charge = 1f;
    public float mass = 1f;
    public float initialSpeed = 5f;
    public float B_strength = 1f;
    public float period;

    
    [Header("Визуализация")]
    private TrailRenderer trail;
    public Color positiveColor = Color.blue;
    public Color negativeColor = Color.red;
    
    [Header("UI")]
    public TMP_Text speedText;
    public TMP_Text energyText;
    public TMP_Text fieldText;
    public TMP_Text radiusText;
    public TMP_Text periodText;
    // public TMP_Text chargeText;

    [Header("Электрическое поле (для продвинутого уровня)")]
    public Vector3 electricField = Vector3.zero;
    public bool useElectricField = false;
    
    // Приватные
    private Vector3 velocity;
    private Vector3 position;
    private float kineticEnergy;
    private float radius;
    private float initialEnergy;
    
    void Start()
    {
        // НАЧАЛЬНЫЕ УСЛОВИЯ (строго по условиям задачи)
        position = Vector3.zero;
        
        // Скорость ТОЛЬКО в плоскости XY, перпендикулярно B
        velocity = new Vector3(initialSpeed, 0f, 0f); // (vx, 0, 0)
        
        // Запоминаем начальную энергию
        initialEnergy = 0.5f * mass * initialSpeed * initialSpeed;
        kineticEnergy = initialEnergy;

        // Проверяем и настраиваем TrailRenderer
        if (trail == null)
        {
            // Пытаемся найти на этом же объекте
            trail = GetComponent<TrailRenderer>();
            
            // Если нет - создаем
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }
        }
        
        // Настройки TrailRenderer
        if (trail != null)
        {
            trail.time = 3f; // Длина следа в секундах
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            // Цвет установится в UpdateTrailColor()
            UpdateTrailColor();
        }
        
        // Цвет следа
        // UpdateTrailColor();
        
        Debug.Log("Начальная скорость: " + velocity + " (строго в плоскости XY)");
        Debug.Log("Начальная энергия: " + initialEnergy + " Дж");
    }
    
    void FixedUpdate()
    {
        Vector3 B = new Vector3(0, 0, B_strength);
    
        // 2. СИЛА ЛОРЕНЦА: F = q(v × B) или F = q(E + v × B)
        Vector3 force;
        if (useElectricField)
        {
            // Полная сила Лоренца с электрическим полем
            force = charge * (electricField + Vector3.Cross(velocity, B));
        }
        else
        {
            // Только магнитное поле
            force = charge * Vector3.Cross(velocity, B);
        }
            
        // 3. УСКОРЕНИЕ
        Vector3 acceleration = force / mass;
        
        // 4. ТОЧНОЕ ИНТЕГРИРОВАНИЕ (аналитическое решение для однородного поля)
        // Для однородного B и v ⊥ B: движение по окружности
        // v(t+dt) = v(t) + a*dt (но тут надо аккуратно)
        
        // Простой Эйлер, но с маленьким шагом и проверкой
        float dt = Time.fixedDeltaTime;
        
        // Сохраняем старую скорость для проверки
        // Vector3 oldVelocity = velocity;
        
        // Интегрируем
        velocity += acceleration * dt;
        position += velocity * dt;
        
        // ПРОВЕРКА: скорость должна сохранять модуль!
        // Если модуль изменился — нормализуем
        float speedShouldBe = initialSpeed; // Должно быть постоянно!
        float currentSpeed = velocity.magnitude;
        
        if (Mathf.Abs(currentSpeed - speedShouldBe) > 0.001f)
        {
            // Принудительно сохраняем модуль скорости
            velocity = velocity.normalized * speedShouldBe;
        }
        
        // 5. Обновляем позицию
        transform.position = position;
        
        // 6. Расчет физики
        currentSpeed = velocity.magnitude;
        kineticEnergy = 0.5f * mass * currentSpeed * currentSpeed;
        
        // Радиус: R = mv/(|q|B)
        radius = mass * currentSpeed / (Mathf.Abs(charge) * B_strength);

        if (Mathf.Abs(charge) > 0.0001f && B_strength > 0.0001f)
        {
            period = (2f * Mathf.PI * mass) / (Mathf.Abs(charge) * B_strength);
        }
        else
        {
            period = 0f;
        }
                
        // 7. Обновляем UI
        UpdateUI();
        
        // 8. Визуализация поля
        DrawFieldLines();
    }
    
    void UpdateUI()
    {
        float currentSpeed = velocity.magnitude;
        
        if (speedText) speedText.text = $"Скорость: {currentSpeed:F4}";
        if (energyText) energyText.text = $"Энергия: {kineticEnergy:F4}";
        if (fieldText) fieldText.text = $"Поле B: {B_strength:F2}";
        
        
        // Радиос вычисляем правильно
        if (Mathf.Abs(charge) > 0.0001f && B_strength > 0.0001f)
        {
            radius = mass * currentSpeed / (Mathf.Abs(charge) * B_strength);
            if (radiusText) radiusText.text = $"Радиус: {radius:F2}";
        }
        else
        {
            if (radiusText) radiusText.text = $"Радиус: ∞";
        }

        if (periodText) periodText.text = $"Период: {period:F2}";
        
        // if (chargeText) chargeText.text = $"Заряд: {charge:F2}";
    }
    
    void UpdateTrailColor()
    {
        Color c = (charge > 0) ? positiveColor : negativeColor;
        trail.startColor = c;
        trail.endColor = c;
    }
    
    void DrawFieldLines()
    {
        // Простые линии вдоль Z
        for (int i = -5; i <= 5; i++)
        {
            for (int j = -5; j <= 5; j++)
            {
                Vector3 start = new Vector3(i, j, -5);
                Vector3 end = new Vector3(i, j, 5);
                Debug.DrawLine(start, end, Color.cyan);
            }
        }
    }
    
    public void SetCharge(float q)
    {
        charge = q;
        UpdateTrailColor();
    }
    
    public void SetField(float b)
    {
        B_strength = b;
    }
    
    public void SetMass(float newMass)
    {
        // Сохраняем старую энергию
        float oldEnergy = 0.5f * mass * velocity.sqrMagnitude;
        
        // Меняем массу
        mass = newMass;
        
        // ПЕРЕСЧИТЫВАЕМ скорость так, чтобы энергия сохранилась!
        // K = ½mv² → v = √(2K/m)
        float newSpeed = Mathf.Sqrt(2f * oldEnergy / mass);
        
        // Сохраняем направление, меняем величину
        velocity = velocity.normalized * newSpeed;
        
        // Обновляем начальную энергию
        initialEnergy = oldEnergy;
        kineticEnergy = oldEnergy;
        
        // Обновляем initialSpeed для сброса
        initialSpeed = newSpeed;
    }
}