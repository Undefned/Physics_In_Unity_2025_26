using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class CarouselControllerV3_0 : MonoBehaviour
{
    [Header("Физические параметры")]
    public float basePlatformInertia = 15f;  // Базовый момент инерции платформы (без грузов)
    public float radialAdjustSpeed = 3f;     // Скорость изменения радиуса грузов
    public Transform[] objectSpawnSlots = new Transform[4]; // Позиции для размещения грузов
    public int activeScenario = 1;          // Текущий уровень/сценарий

    [Header("Элементы интерфейса")]
    public TMP_Text statusDisplay;          // Текстовый элемент для вывода информации
    private List<MassObjectV3_0> placedObjects = new List<MassObjectV3_0>(); // Список активных грузов
    private int currentlySelected = 0;      // Индекс выбранного груза
    private bool isSystemRotating = false;  // Флаг состояния вращения системы

    [Header("Параметры затухания")]
    public float angularDrag = 0.1f;  // Коэффициент сопротивления вращению

    private Vector3 preservedAngularMomentum = Vector3.zero; // Сохранённый угловой момент
    private Vector3 currentAngularVelocity = Vector3.zero;   // Текущая угловая скорость
    private float momentOfInertiaX = 0f;    // Момент инерции относительно оси X
    private float momentOfInertiaY = 0f;    // Момент инерции относительно оси Y
    private float momentOfInertiaZ = 0f;    // Момент инерции относительно оси Z

    void Awake()
    {
        // Инициализация физического компонента платформы
        var platformRigidbody = GetComponent<Rigidbody>();
        platformRigidbody.isKinematic = true;   // Ручное управление вращением
        platformRigidbody.useGravity = false;    // Отключение гравитационных эффектов
    }

    void Start()
    {
        // Загрузка начального сценария при старте
        InitializeScenario(activeScenario);
    }

    void Update()
    {
        // Основной цикл обработки
        ProcessUserCommands();          // Обработка ввода пользователя
        ExecuteRotationPhysics();       // Применение физики вращения
        RefreshObjectVisuals();         // Обновление визуального состояния
        RefreshInterfaceDisplay();      // Обновление информации на экране
        HandleLevelNavigation();        // Обработка переключения уровней
    }

    /// <summary>
    /// Обработка пользовательского ввода с клавиатуры
    /// </summary>
    void ProcessUserCommands()
    {
        if (placedObjects.Count == 0) return; // Нет объектов для управления

        Keyboard inputDevice = Keyboard.current;
        if (inputDevice == null) return;

        // Переключение выбранного объекта
        if (inputDevice.qKey.wasPressedThisFrame)
            currentlySelected = (currentlySelected + placedObjects.Count - 1) % placedObjects.Count;

        if (inputDevice.eKey.wasPressedThisFrame)
            currentlySelected = (currentlySelected + 1) % placedObjects.Count;

        // Изменение радиуса выбранного объекта
        float radialChange = 0;
        if (inputDevice.aKey.isPressed)
            radialChange -= radialAdjustSpeed * Time.deltaTime;
        if (inputDevice.dKey.isPressed)
            radialChange += radialAdjustSpeed * Time.deltaTime;

        if (radialChange != 0)
        {
            placedObjects[currentlySelected].MoveRadial(radialChange);
            RecalculateSystemProperties();
        }

        // Запуск/остановка вращения
        if (inputDevice.spaceKey.wasPressedThisFrame)
        {
            if (!isSystemRotating)
            {
                // Инициализация вращения с начальной угловой скоростью по оси Y
                Vector3 initialOmega = Vector3.up * 5f;
                RecalculateSystemProperties();
                Vector3 omegaLocal = transform.InverseTransformDirection(initialOmega);
                Vector3 angularMomentumLocal = new Vector3(momentOfInertiaX * omegaLocal.x,
                                                          momentOfInertiaY * omegaLocal.y,
                                                          momentOfInertiaZ * omegaLocal.z);
                preservedAngularMomentum = transform.TransformDirection(angularMomentumLocal);
                currentAngularVelocity = initialOmega;
                isSystemRotating = true;
            }
            else
            {
                // Остановка вращения
                isSystemRotating = false;
                currentAngularVelocity = Vector3.zero;
                preservedAngularMomentum = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Пересчёт моментов инерции и центра масс системы
    /// </summary>
    void RecalculateSystemProperties()
    {
        float Ixx = basePlatformInertia;
        float Iyy = basePlatformInertia;
        float Izz = basePlatformInertia;

        Vector3 centerOfMass = Vector3.zero;
        float combinedMass = 0f;

        foreach (var obj in placedObjects)
        {
            Vector3 position = obj.transform.localPosition;
            float mass = obj.mass;

            // Расчёт моментов инерции по формулам для точечных масс
            Ixx += mass * (position.z * position.z);
            Iyy += mass * (position.x * position.x + position.z * position.z);
            Izz += mass * (position.x * position.x);

            // Суммирование для вычисления центра масс
            centerOfMass += position * mass;
            combinedMass += mass;
        }

        momentOfInertiaX = Ixx;
        momentOfInertiaY = Iyy;
        momentOfInertiaZ = Izz;

        var platformRigidbody = GetComponent<Rigidbody>();
        platformRigidbody.centerOfMass = combinedMass > 0 ? centerOfMass / combinedMass : Vector3.zero;
    }

    /// <summary>
    /// Расчёт угловой скорости из сохранённого углового момента
    /// </summary>
    void CalculateAngularVelocityFromConservation()
    {
        if (!isSystemRotating || preservedAngularMomentum == Vector3.zero) return;

        Vector3 momentumLocal = transform.InverseTransformDirection(preservedAngularMomentum);
        Vector3 omegaLocal = new Vector3(
            momentOfInertiaX > 0f ? momentumLocal.x / momentOfInertiaX : 0f,
            momentOfInertiaY > 0f ? momentumLocal.y / momentOfInertiaY : 0f,
            momentOfInertiaZ > 0f ? momentumLocal.z / momentOfInertiaZ : 0f
        );

        currentAngularVelocity = transform.TransformDirection(omegaLocal);
    }

    /// <summary>
    /// Применение физики вращения и прецессии
    /// </summary>
    void ExecuteRotationPhysics()
    {
        if (!isSystemRotating) return;
        
        // Применяем естественное затухание
        if (currentAngularVelocity.magnitude > 0)
        {
            // Уменьшаем угловую скорость
            currentAngularVelocity -= currentAngularVelocity * angularDrag * Time.deltaTime;
            
            // Обновляем сохранённый момент импульса
            Vector3 omegaLocal = transform.InverseTransformDirection(currentAngularVelocity);
            Vector3 momentumLocal = new Vector3(
                momentOfInertiaX * omegaLocal.x,
                momentOfInertiaY * omegaLocal.y,
                momentOfInertiaZ * omegaLocal.z
            );
            preservedAngularMomentum = transform.TransformDirection(momentumLocal);
            
            // Если скорость почти нулевая - останавливаем
            if (currentAngularVelocity.magnitude < 0.05f)
            {
                currentAngularVelocity = Vector3.zero;
                preservedAngularMomentum = Vector3.zero;
                isSystemRotating = false;
            }
        }

        RecalculateSystemProperties();
        CalculateAngularVelocityFromConservation();

        // Специальная обработка для сценария с 3 объектами (дисбаланс)
        if (placedObjects.Count == 3)
        {
            Vector3 centerOfMass = GetComponent<Rigidbody>().centerOfMass;
            Vector3 horizontalOffset = new Vector3(centerOfMass.x, 0f, centerOfMass.z);

            // Если центр масс смещён, возникает прецессия
            if (horizontalOffset.sqrMagnitude > 0.0005f)
            {
                Vector3 torque = Vector3.Cross(horizontalOffset, preservedAngularMomentum);
                float epsilon = 1e-3f;

                // Эффективные моменты инерции (избегаем деления на ноль)
                float IxxEffective = Mathf.Max(momentOfInertiaX, epsilon);
                float IyyEffective = Mathf.Max(momentOfInertiaY, epsilon);
                float IzzEffective = Mathf.Max(momentOfInertiaZ, epsilon);

                Vector3 deltaOmegaLocal = new Vector3(
                    torque.x / IxxEffective,
                    torque.y / IyyEffective,
                    torque.z / IzzEffective
                ) * Time.deltaTime;

                float precessionIntensity = 0.2f;
                Vector3 deltaOmegaWorld = transform.TransformDirection(deltaOmegaLocal) * precessionIntensity;

                // Демпфирование для устойчивости
                float dampingFactor = 0.95f;
                currentAngularVelocity.x = (currentAngularVelocity.x + deltaOmegaWorld.x) * dampingFactor;
                currentAngularVelocity.z = (currentAngularVelocity.z + deltaOmegaWorld.z) * dampingFactor;
            }
        }

        // Ограничение углов наклона для стабильности
        float maximumTilt = 20f * Mathf.Deg2Rad;
        currentAngularVelocity.x = Mathf.Clamp(currentAngularVelocity.x, -maximumTilt, maximumTilt);
        currentAngularVelocity.z = Mathf.Clamp(currentAngularVelocity.z, -maximumTilt, maximumTilt);

        // Применение вращения к платформе
        Vector3 rotationDelta = currentAngularVelocity * Mathf.Rad2Deg * Time.deltaTime;
        transform.Rotate(rotationDelta, Space.World);
    }

    /// <summary>
    /// Обновление визуального выделения объектов
    /// </summary>
    void RefreshObjectVisuals()
    {
        for (int index = 0; index < placedObjects.Count; index++)
        {
            placedObjects[index].SetSelected(index == currentlySelected);
        }
    }

    /// <summary>
    /// Обновление текстового интерфейса с информацией о системе
    /// </summary>
    void RefreshInterfaceDisplay()
    {
        if (!statusDisplay) return;

        var platformRigidbody = GetComponent<Rigidbody>();
        Vector3 centerOfMass = platformRigidbody.centerOfMass;

        // Расчёт момента инерции относительно оси Y для отображения
        float IyyDisplay = basePlatformInertia;
        foreach (var obj in placedObjects)
        {
            Vector3 position = obj.transform.localPosition;
            IyyDisplay += obj.mass * (position.x * position.x + position.z * position.z);
        }

        // Определение задачи для текущего сценария
        string scenarioObjective = placedObjects.Count switch
        {
            2 => "Балансировка симметричных масс",
            4 => "Создание максимальной инерции",
            3 => "Компенсация дисбаланса массы",
            _ => "Настройте распределение масс"
        };

        // Форматированный вывод информации
        statusDisplay.text = 
        $@"ЭКСПЕРИМЕНТ: ВРАЩЕНИЕ ТВЁРДОГО ТЕЛА
        ==============================
        Уровень: {activeScenario} | Объектов: {placedObjects.Count}
        Задача: {scenarioObjective}
        ------------------------------
        Угловые скорости (рад/с):
        ω_x = {currentAngularVelocity.x,8:F2}
        ω_y = {currentAngularVelocity.y,8:F2} 
        ω_z = {currentAngularVelocity.z,8:F2}
        ------------------------------
        Параметры системы:
        Момент инерции Iyy = {IyyDisplay:F1} кг·м²
        Угловой момент L = {preservedAngularMomentum.magnitude:F1} кг·м²/с
        Центр масс: ({centerOfMass.x:F2}, {centerOfMass.y:F2}, {centerOfMass.z:F2})
        ------------------------------
        Управление:
        Q/E - выбор объекта
        A/D - изменение радиуса  
        ПРОБЕЛ - запуск/остановка
        ←/→ - смена уровня";
    }

    /// <summary>
    /// Загрузка предыдущего уровня
    /// </summary>
    private void LoadPreviousScenario()
    {
        if (activeScenario > 1) 
        {
            activeScenario--;
            InitializeScenario(activeScenario);
        }
    }

    /// <summary>
    /// Загрузка следующего уровня
    /// </summary>
    private void LoadNextScenario()
    {
        if (activeScenario < 3) 
        {
            activeScenario++;
            InitializeScenario(activeScenario);
        }
    }

    /// <summary>
    /// Инициализация указанного сценария (уровня)
    /// </summary>
    /// <param name="scenarioNumber">Номер сценария для загрузки</param>
    private void InitializeScenario(int scenarioNumber)
    {
        // Сброс состояния вращения
        isSystemRotating = false;
        currentAngularVelocity = Vector3.zero;
        preservedAngularMomentum = Vector3.zero;

        // Сброс ориентации платформы
        transform.rotation = Quaternion.identity;

        // Сброс центра масс
        var platformRigidbody = GetComponent<Rigidbody>();
        if (platformRigidbody != null)
            platformRigidbody.centerOfMass = Vector3.zero;

        // Деактивация предыдущих объектов
        foreach (var obj in placedObjects)
            if (obj)
                obj.gameObject.SetActive(false);

        placedObjects.Clear();

        // Определение конфигурации объектов для каждого сценария
        int[] objectConfiguration = scenarioNumber switch
        {
            1 => new[] { 0, 2 },        // Сценарий 1: 2 груза в противоположных точках
            2 => new[] { 0, 1, 2, 3 },  // Сценарий 2: все 4 точки заняты
            3 => new[] { 0, 1, 2 },     // Сценарий 3: 3 груза в асимметричном расположении
            _ => new int[0]
        };

        // Создание объектов согласно конфигурации
        foreach (int slotIndex in objectConfiguration)
        {
            if (slotIndex < objectSpawnSlots.Length && objectSpawnSlots[slotIndex]?.childCount > 0)
            {
                var massObject = objectSpawnSlots[slotIndex].GetChild(0).GetComponent<MassObjectV3_0>();
                if (massObject)
                {
                    massObject.gameObject.SetActive(true);
                    placedObjects.Add(massObject);
                }
            }
        }

        // Первоначальный расчёт параметров системы
        RecalculateSystemProperties();
    }

    /// <summary>
    /// Обработка навигации по уровням с клавиатуры
    /// </summary>
    private void HandleLevelNavigation()
    {
        Keyboard inputDevice = Keyboard.current;
        if (inputDevice == null) return;

        if (inputDevice.leftArrowKey.wasPressedThisFrame)
        {
            LoadPreviousScenario();
        }
        if (inputDevice.rightArrowKey.wasPressedThisFrame)
        {
            LoadNextScenario();
        }
    }

    private void OnDrawGizmos()
    {
        var carouselRigidbody = GetComponent<Rigidbody>();
        if (carouselRigidbody == null) return;

        // Рисуем центр масс
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(carouselRigidbody.worldCenterOfMass, 2f);

        // Рисуем вектор угловой скорости
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(carouselRigidbody.worldCenterOfMass, carouselRigidbody.angularVelocity * 2f);
    }
}