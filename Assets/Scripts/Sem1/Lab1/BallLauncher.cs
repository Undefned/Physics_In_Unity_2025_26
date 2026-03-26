using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;
    public Transform spawnPoint;
    public LineRenderer trajectoryLine;
    
    [Header("Launch Settings")]
    public float currentSpeed = 5f;
    public float currentAngle = 45f;

    [Header("Ball Physics Settings")]
    public float ballMass = 1f;           // Масса шара (кг)
    public float ballDrag = 0f;           // Сопротивление воздуха
    public float ballAngularDrag = 0.05f; // Сопротивление вращению
    public PhysicsMaterial ballPhysicsMaterial; // Физический материал для упругости
    
    [Header("UI References")]
    public TMP_InputField speedInputField;
    public TMP_InputField angleInputField;
    public Button startButton;
    public Button restartButton;

    private GameObject currentBall;
    private Rigidbody currentBallRigidbody;
    private bool isBallLaunched = false;

    void Start()
    {
        // Проверяем и настраиваем Line Renderer
        SetupTrajectoryLine();
        
        // Находим UI элементы
        FindUIElements();
        
        // Устанавливаем начальные значения в UI
        speedInputField.text = currentSpeed.ToString();
        angleInputField.text = currentAngle.ToString();
        
        // Подписываемся на события
        startButton.onClick.AddListener(OnStartButtonClick);
        restartButton.onClick.AddListener(OnRestartButtonClick);
        speedInputField.onValueChanged.AddListener(OnSpeedInputChanged);
        angleInputField.onValueChanged.AddListener(OnAngleInputChanged);
        
        // Создаем начальный шар
        SpawnNewBall();
        
        // Принудительно обновляем траекторию
        ForceUpdateTrajectory();
    }

    void SetupTrajectoryLine()
    {
        if (trajectoryLine == null)
        {
            return;
        }
        
        // Принудительная настройка Line Renderer
        trajectoryLine.positionCount = 50;
        trajectoryLine.startWidth = 0.05f;
        trajectoryLine.endWidth = 0.05f;
        trajectoryLine.useWorldSpace = true;
        
        trajectoryLine.startColor = Color.red;
        trajectoryLine.endColor = Color.red;
        
        trajectoryLine.enabled = true;
    }

    void FindUIElements()
    {
        if (speedInputField == null)
        {
            GameObject speedInput = GameObject.Find("Speed_Input");
            if (speedInput != null) speedInputField = speedInput.GetComponent<TMP_InputField>();
        }
        
        if (angleInputField == null)
        {
            GameObject angleInput = GameObject.Find("Angle_Input");
            if (angleInput != null) angleInputField = angleInput.GetComponent<TMP_InputField>();
        }
        
    }

    void Update()
    {
        // Постоянно обновляем траекторию, пока шар не запущен
        if (!isBallLaunched)
        {
            UpdateTrajectoryPreview();
        }
    }

    void SpawnNewBall()
    {
        // Удаляем старый шар
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // Создаем новый шар
        currentBall = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        currentBallRigidbody = currentBall.GetComponent<Rigidbody>();
        
        // ИСПРАВЛЕНИЕ: Сначала проверяем наличие Rigidbody, потом настраиваем
        if (currentBallRigidbody == null)
        {
            currentBallRigidbody = currentBall.AddComponent<Rigidbody>();
        }
        
        // НАСТРОЙКА ФИЗИЧЕСКИХ СВОЙСТВ ШАРА:
        // Масса влияет на инерцию - чем больше масса, тем сложнее изменить движение
        currentBallRigidbody.mass = ballMass;
        
        // Drag - сопротивление движению (0 = нет сопротивления воздуха)
        currentBallRigidbody.linearDamping = ballDrag;
        
        // Angular Drag - сопротивление вращению
        currentBallRigidbody.angularDamping = ballAngularDrag;
        
        // Включаем гравитацию для шара
        currentBallRigidbody.useGravity = true;
        
        // Настраиваем коллайдер для упругости
        SetupBallCollider();
        
        // Делаем шар кинематическим до запуска
        currentBallRigidbody.isKinematic = true;
        isBallLaunched = false;
        
        // Включаем и обновляем траекторию
        trajectoryLine.enabled = true;
    }

    void SetupBallCollider()
    {
        // Получаем или добавляем коллайдер шару
        SphereCollider collider = currentBall.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = currentBall.AddComponent<SphereCollider>();
        }

        // НАСТРОЙКА ФИЗИЧЕСКОГО МАТЕРИАЛА ДЛЯ УПРУГОСТИ:
        // Создаем физический материал если он не назначен
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = new PhysicsMaterial();
            ballPhysicsMaterial.bounciness = 0.8f;    // Коэффициент упругости (0-1)
            ballPhysicsMaterial.dynamicFriction = 0.1f; // Трение при движении
            ballPhysicsMaterial.staticFriction = 0.1f;  // Трение покоя
        }

        collider.material = ballPhysicsMaterial;
    }
    
    void ForceUpdateTrajectory()
    {
        // Принудительное обновление траектории
        trajectoryLine.enabled = true;
        UpdateTrajectoryPreview();
    }

    void UpdateTrajectoryPreview()
    {
        // Проверяем, доступен ли LineRenderer для отрисовки траектории
        if (trajectoryLine == null || !trajectoryLine.enabled) return;
        
        // ПРЕОБРАЗОВАНИЕ УГЛА: Переводим угол из градусов в радианы
        // Формула: угол_в_радианах = угол_в_градусах × (π / 180)
        // Mathf.Deg2Rad - константа, равная π/180 ≈ 0.0174533
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        
        // РАЗЛОЖЕНИЕ ВЕКТОРА СКОРОСТИ НА КОМПОНЕНТЫ:
        // Вектор начальной скорости раскладывается на горизонтальную (X) и вертикальную (Y) составляющие
        // Формулы:
        // Vx = V₀ × cos(α) - горизонтальная компонента скорости (постоянная)
        // Vy = V₀ × sin(α) - вертикальная компонента скорости (изменяется под действием гравитации)
        Vector3 startVelocity = new Vector3(
            Mathf.Cos(angleInRadians) * currentSpeed,  // Vx = V₀·cos(α)
            Mathf.Sin(angleInRadians) * currentSpeed,  // Vy = V₀·sin(α)
            0  // Z-компонента равна 0 (движение в 2D плоскости)
        );

        // НАСТРОЙКА ЛИНИИ ТРАЕКТОРИИ:
        // Устанавливаем количество точек для построения плавной кривой
        int pointsCount = 50;
        trajectoryLine.positionCount = pointsCount;

        // НАЧАЛЬНАЯ ПОЗИЦИЯ: Точка, откуда начинается движение (позиция спавна шара)
        Vector3 startPosition = spawnPoint.position;

        // РАСЧЕТ ТОЧЕК ТРАЕКТОРИИ:
        // Для каждой точки временной шкалы рассчитываем позицию шара
        for (int i = 0; i < pointsCount; i++)
        {
            // ВРЕМЯ СИМУЛЯЦИИ: 
            // Каждая точка соответствует определенному моменту времени полета
            // simulationTime = номер_точки × временной_шаг
            float simulationTime = i * 0.1f;  // шаг времени = 0.1 секунды
                
            // ФОРМУЛА ДВИЖЕНИЯ ТЕЛА ПОД УГЛОМ К ГОРИЗОНТУ:
            // Вектор перемещения S рассчитывается по формуле:
            // S = V₀·t + (a·t²)/2
            // где:
            // V₀·t - перемещение за счет начальной скорости (равномерное движение)
            // (a·t²)/2 - перемещение за счет ускорения (равноускоренное движение)
            Vector3 displacement = startVelocity * simulationTime + 
                                Physics.gravity * simulationTime * simulationTime * 0.5f;
            
            // ПОЗИЦИЯ ТОЧКИ ТРАЕКТОРИИ:
            // Рассчитываем мировые координаты точки на траектории
            // pointPosition = начальная_позиция + перемещение
            Vector3 pointPosition = startPosition + displacement;
            
            // УСТАНОВКА ПОЗИЦИИ В LineRenderer:
            // Записываем рассчитанную позицию в соответствующую точку линии
            trajectoryLine.SetPosition(i, pointPosition);
        }
    }

    public void OnSpeedInputChanged(string newValue)
    {
        // ОБРАБОТКА ИЗМЕНЕНИЯ СКОРОСТИ:
        // Преобразуем текстовое значение в число и проверяем корректность
        if (float.TryParse(newValue, out float result) && result > 0)
        {
            // Устанавливаем новое значение скорости (в условных единицах)
            currentSpeed = result;
        }
        else
        {
            // ВОССТАНОВЛЕНИЕ ПРИ НЕКОРРЕКТНОМ ВВОДЕ:
            // Если введено некорректное значение, восстанавливаем предыдущее
            speedInputField.text = currentSpeed.ToString();
        }
    }

    public void OnAngleInputChanged(string newValue)
    {
        // ОБРАБОТКА ИЗМЕНЕНИЯ УГЛА:
        // Преобразуем текстовое значение в число
        if (float.TryParse(newValue, out float result))
        {
            // ОГРАНИЧЕНИЕ ДИАПАЗОНА УГЛА:
            // Угол броска ограничиваем диапазоном от 0° до 90°
            // 0° - горизонтальный бросок, 90° - вертикальный вверх
            currentAngle = Mathf.Clamp(result, 0, 90);
            
            // ОБНОВЛЕНИЕ ОТОБРАЖАЕМОГО ЗНАЧЕНИЯ:
            // Показываем фактическое значение угла (после применения ограничений)
            angleInputField.text = currentAngle.ToString();
        }
        else
        {
            // ВОССТАНОВЛЕНИЕ ПРИ НЕКОРРЕКТНОМ ВВОДЕ
            angleInputField.text = currentAngle.ToString();
        }
    }

    void OnStartButtonClick()
    {
        // ПРОВЕРКА ВОЗМОЖНОСТИ ЗАПУСКА:
        // Шар можно запустить только если он еще не запущен и Rigidbody доступен
        if (isBallLaunched || currentBallRigidbody == null) return;

        // ОБНОВЛЕНИЕ ПАРАМЕТРОВ ПЕРЕД ЗАПУСКОМ:
        // Считываем актуальные значения из полей ввода
        OnSpeedInputChanged(speedInputField.text);
        OnAngleInputChanged(angleInputField.text);

        // ЗАПУСК ШАРА
        LaunchCurrentBall();
    }

    void LaunchCurrentBall()
    {
        // АКТИВАЦИЯ ФИЗИКИ:
        // Переключаем Rigidbody из кинематического режима в динамический
        // Кинематический режим - объект не подвержен физике, но может двигаться
        // Динамический режим - объект полностью управляется физическим движком
        currentBallRigidbody.isKinematic = false;
        
        // РАСЧЕТ НАПРАВЛЕНИЯ СИЛЫ:
        // Повторно рассчитываем вектор направления (аналогично траектории)
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        
        // Вектор направления силы:
        // direction = (cos(α), sin(α), 0) - единичный вектор направления
        Vector3 launchDirection = new Vector3(
            Mathf.Cos(angleInRadians), 
            Mathf.Sin(angleInRadians), 
            0
        );
        
        // ПРИМЕНЕНИЕ СИЛЫ К ШАРУ:
        // AddForce - добавляет силу к Rigidbody
        // ForceMode.VelocityChange - мгновенно изменяет скорость, игнорируя массу
        // Формула: сила = направление × скорость
        // В VelocityChange скорость изменяется непосредственно, без учета массы
        currentBallRigidbody.AddForce(launchDirection * currentSpeed, ForceMode.VelocityChange);

        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ПОСЛЕ ЗАПУСКА:
        // Отключаем отображение траектории, так как шар уже запущен
        trajectoryLine.enabled = false;
        
        // УСТАНОВКА ФЛАГА СОСТОЯНИЯ:
        // Помечаем, что шар находится в полете
        isBallLaunched = true;
    }

    void OnRestartButtonClick()
    {
        SpawnNewBall();
        ForceUpdateTrajectory();
    }
}
