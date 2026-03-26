using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CarouselController : MonoBehaviour
{
    [Header("Physics References")]
    public Rigidbody carouselRigidbody;                    // Физическое тело карусели
    public List<Transform> massPoints;                     // Точки крепления грузов (привязаны к кубам)
    public List<GameObject> massPrefabs;                   // Префабы визуальных моделей грузов

    [Header("Level Settings")]
    public int currentLevel = 1;                           // Текущий уровень (1, 2, 3)
    private List<GameObject> currentMasses = new List<GameObject>(); // Активные грузы на сцене
    private static List<float> currentMassesDistance = new List<float>(); // Расстояния активных грузов на сцене


    [Header("Control Settings")]
    public float moveSpeed = 2.0f;                         // Скорость движения грузов
    private int selectedMassIndex = 0;                     // Индекс выбранного груза (0, 1, 2)

    [Header("Initial Spin")]
    public float initialSpinForce = 5f;                    // Начальный импульс вращения

    [Header("UI References")]
    public TMP_Text angularVelocityText;                   // Текст угловой скорости
    public TMP_Text momentOfInertiaText;                   // Текст момента инерции
    public TMP_Text levelText;                             // Текст текущего уровня
    public Vector3 centerOfCarousel;                       // Центр вращения карусели

    [Header("Mass Objects")]
    public bool rotateMassesAroundCenter = true;           // Включить вращение грузов

    public Material originalMaterial;                      // Стандартный материал груза
    public Material selectedMaterial;                      // Материал выделенного груза
    private List<Vector3> initialMassPointPositions = new List<Vector3>(); // Начальные позиции точек крепления(не изменяется)

    void Start()
    {
        initialMassPointPositions.Clear();
        for (int i = 0; i < massPoints.Count; i++)
        {
            initialMassPointPositions.Add(massPoints[i].localPosition); // Сохраняем локальные позиции
        }
        
        // Получаем Rigidbody если не установлен в инспекторе
        if (carouselRigidbody == null)
            carouselRigidbody = GetComponent<Rigidbody>();
            
        // Загружаем начальный уровень
        LoadLevel(currentLevel);
    }

    void Update()
    {
        HandleObjectSelection();
        HandleObjectMovement();
        ChangeLevelByPressKeyboard();

        // Вращаем точки крепления грузов вокруг центра
        // if (rotateMassesAroundCenter && carouselRigidbody != null)
        // {
            // RotateMassVisuals();
            // ChangePositionForMassVisual();
            // ChangePositionForMassVisualRelativeToTheCenter();
        // }

        UpdateUI();
    }

    private float FindRealPlatformCenter()
    {
        return 0.0f; // fuck, just kill me
    }

    private void HandleObjectMovement()
    {
        if (currentMasses.Count == 0 || selectedMassIndex >= currentMasses.Count) return;

        bool moved = false;
        GameObject selectedMass = currentMasses[selectedMassIndex];
        
        if (selectedMass == null) return;
        
        Transform massPoint = selectedMass.transform.parent;

        // Обработка движения
        if (Keyboard.current.aKey.isPressed)
        {
            massPoint.Translate(-moveSpeed * Time.deltaTime, 0, 0);
            moved = true;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            massPoint.Translate(moveSpeed * Time.deltaTime, 0, 0);
            moved = true;
        }

        // Ограничение радиуса и пересчет физики
        if (moved)
        {
            Vector3 localPos = massPoint.localPosition;
            localPos.x = Mathf.Clamp(localPos.x, -0.6f, 0.6f);
            massPoint.localPosition = localPos;
            
            // обновляем расстояние для физики
            currentMassesDistance[selectedMassIndex] = Mathf.Abs(massPoint.localPosition.x);
            
            UpdateInertiaTensor();
        }
    }

    private void PrevLevelLoad()
    {
        if (currentLevel > 1) 
        {
            currentLevel--;
            LoadLevel(currentLevel);
        }
    }

    private void NextLevelLoad()
    {
        if (currentLevel < 3) 
        {
            currentLevel++;
            LoadLevel(currentLevel);
            Debug.Log(currentLevel);
        }
    }

    private void LoadLevel(int level)
    {
        Debug.Log($"Loading level {level}");
        ResetMassPointsPositions();

        // Останавливаем текущее вращение
        if (carouselRigidbody != null)
        {
            carouselRigidbody.angularVelocity = Vector3.zero;
            carouselRigidbody.linearVelocity = Vector3.zero;
        }

        // Уничтожаем старые грузы
        foreach (GameObject mass in currentMasses)
        {
            if (mass != null) Destroy(mass);
        }
        currentMasses.Clear();
        currentMassesDistance.Clear();

        // Создаем грузы в зависимости от уровня
        switch (level)
        {
            case 1:
                // Создаем 2 груза симметрично
                SpawnMassAtPoint(0); // левый центр
                SpawnMassAtPoint(1); // правый центр
                break;
            case 2:
                SpawnMassAtPoint(2); // правый край
                SpawnMassAtPoint(3); // левый край
                break;
            case 3:
                // Создаем асимметрично: 2 слева, 1 справа
                SpawnMassAtPoint(0); // левый центр
                SpawnMassAtPoint(1); // правый центр
                SpawnMassAtPoint(2); // правый край
                break;
        }

        // Сбрасываем выбор объекта
        selectedMassIndex = 0;

        
        // Пересчитываем физику и раскручиваем
        UpdateInertiaTensor();
        // SaveInitialOffsets();
        StartSpinning();
    }

    private void SpawnMassAtPoint(int pointIndex)
    {
        if (pointIndex < massPoints.Count && massPrefabs.Count > 0 && massPoints[pointIndex] != null)
        {
            GameObject newMass = Instantiate(massPrefabs[0], massPoints[pointIndex].position, 
                                        massPoints[pointIndex].rotation, massPoints[pointIndex]);
            currentMasses.Add(newMass);

            // РАСЧЕТ РАССТОЯНИЯ ОТ ЦЕНТРА (по X)
            float distance = Mathf.Abs(massPoints[pointIndex].localPosition.x); // Берем абсолютное значение

            for (int i = 0; i < massPoints.Count; i++)
            {
                Debug.Log($"massPoints[pointIndex]------------------------------------------- {massPoints[pointIndex]}");
            }

            currentMassesDistance.Add(distance);
            
            Debug.Log($"Spawned mass at point {pointIndex}, distance from center: {distance}");

            MassObject massComp = newMass.GetComponent<MassObject>();
            if (massComp != null)
            {
                Debug.Log($"Mass value: {massComp.massValue}");
            }
        }
        else
        {
            Debug.LogWarning($"Cannot spawn mass at point {pointIndex}");
        }
    }

    private void UpdateInertiaTensor()
    {
        if (carouselRigidbody == null) return;

        carouselRigidbody.automaticCenterOfMass = false;
        carouselRigidbody.automaticInertiaTensor = false;

        // Упрощенный расчет центра масс (раз центр известен = 0)
        carouselRigidbody.centerOfMass = new Vector3(0, 0, 0);

        // Расчет тензора инерции
        float I_platform = carouselRigidbody.mass * 1f; 
        
        float I_masses = 0f;
        for (int i = 0; i < currentMasses.Count; i++)
        {
            if (currentMasses[i] == null) continue;
            
            MassObject massComp = currentMasses[i].GetComponent<MassObject>();
            
            // текущие расстояния из списка!
            float distance = currentMassesDistance[i];
            float m = massComp.massValue;
            
            I_masses += m * distance * distance;
            
            // ДЛЯ ОТЛАДКИ
            Debug.Log($"Mass {i}: distance={distance:F2}, mass={m}, I_add={m * distance * distance:F2}");
        }

        float I_total = I_platform + I_masses;
        
        // ОТЛАДКА
        Debug.Log($"Total I: {I_total:F2} (Platform: {I_platform:F2} + Masses: {I_masses:F2})");
        
        carouselRigidbody.inertiaTensor = new Vector3(I_total, I_total, I_total);
        carouselRigidbody.inertiaTensorRotation = Quaternion.identity;
    }

    private void OnDrawGizmos()
    {
        if (carouselRigidbody == null) return;

        // Рисуем центр масс
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(carouselRigidbody.worldCenterOfMass, 0.2f);

        // Рисуем вектор угловой скорости
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(carouselRigidbody.worldCenterOfMass, carouselRigidbody.angularVelocity * 2f);
    }

    private void UpdateUI()
    {
        if (angularVelocityText != null && carouselRigidbody != null)
            angularVelocityText.text = $"Angular Velocity: {carouselRigidbody.angularVelocity.ToString("F2")}";

        if (momentOfInertiaText != null && carouselRigidbody != null)
            momentOfInertiaText.text = $"Inertia Tensor: {carouselRigidbody.inertiaTensor.x.ToString("F2")}";

        if (levelText != null)
            levelText.text = $"Level: {currentLevel}";
    }
    
    private void StartSpinning()
    {
        if (carouselRigidbody != null)
        {
            carouselRigidbody.AddTorque(0, initialSpinForce, 0, ForceMode.Impulse);
            Debug.Log("Started spinning");
        }
    }

    private void ResetMassPointsPositions()
    {
        // Сбрасываем на сохраненные ЛОКАЛЬНЫЕ позиции
        for (int i = 0; i < massPoints.Count && i < initialMassPointPositions.Count; i++)
        {
            massPoints[i].localPosition = initialMassPointPositions[i];
        }
    }

    private void ChangeLevelByPressKeyboard()
    {
        // управление уровнями с клавиатуры
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            PrevLevelLoad();
        }
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            NextLevelLoad();
        }
    }

    private void HandleObjectSelection()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            selectedMassIndex = 0;
            Debug.Log($"Selected mass: {selectedMassIndex}");
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            selectedMassIndex = 1;
            Debug.Log($"Selected mass: {selectedMassIndex}");
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            selectedMassIndex = 2;
            Debug.Log($"Selected mass: {selectedMassIndex}");
        }

        if (selectedMassIndex < currentMasses.Count)
        {
            currentMasses[selectedMassIndex].GetComponent<Renderer>().material = selectedMaterial;
            for (int i = 0; i < 2; i++)
            {
                if (i == selectedMassIndex) continue;
                currentMasses[i].GetComponent<Renderer>().material = originalMaterial;
            }
        }
    }

    // public void ChangePositionForMassVisualRelativeToTheCenter()
    // {
    //     if (carouselRigidbody == null) return;
        
    //     // Вращаем каждый груз вокруг указанного центра
    //     foreach (GameObject mass in currentMasses)
    //     {
    //         if (mass != null && mass.transform.parent != null)
    //         {
    //             Transform massPoint = mass.transform.parent;
                
    //             // Получаем вектор от центра к точке
    //             Vector3 directionToCenter = massPoint.position - centerOfCarousel;
                
    //             // Вычисляем новую позицию точки
    //             Vector3 newPosition = centerOfCarousel + directionToCenter;
                
    //             // Применяем новую позицию
    //             massPoint.position = newPosition;
    //         }
    //     }
    // }

    // void RotateMassVisuals()
    // {
    //     float rotationSpeed = carouselRigidbody.angularVelocity.y * Mathf.Rad2Deg;
        
    //     foreach (GameObject mass in currentMasses)
    //     {
    //         if (mass != null)
    //         {
    //             // Вращаем только визуальную часть вокруг ее собственной оси
    //             mass.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    //         }
    //     }
    // }

    // public void ChangePositionForMassVisual()
    // {
    //     if (carouselRigidbody == null) return;
        
    //     // Угол поворота за этот кадр
    //     float rotationAngle = carouselRigidbody.angularVelocity.y * Time.deltaTime * Mathf.Rad2Deg;
        
    //     // Создаем поворот вокруг оси Y
    //     Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
        
    //     // Вращаем каждый груз вокруг указанного центра
    //     foreach (GameObject mass in currentMasses)
    //     {
    //         if (mass != null && mass.transform.parent != null)
    //         {
    //             Transform massPoint = mass.transform.parent;
                
    //             // Получаем вектор от центра к точке
    //             Vector3 directionToCenter = massPoint.position - centerOfCarousel;
                
    //             // Вращаем этот вектор
    //             Vector3 rotatedDirection = rotation * directionToCenter;
                
    //             // Вычисляем новую позицию точки
    //             Vector3 newPosition = centerOfCarousel + rotatedDirection;
                
    //             // Применяем новую позицию
    //             massPoint.position = newPosition;
    //         }
    //     }
    // }
}