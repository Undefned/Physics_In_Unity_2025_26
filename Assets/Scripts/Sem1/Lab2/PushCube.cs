using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PushCube : MonoBehaviour
{
    [System.Serializable]
    public class CubePreset
    {
        public string name;
        public float mass;
        public float friction;
        // public Material material; // опционально - разный цвет
    }

    public CubePreset[] cubePresets = new CubePreset[]
    {
        new CubePreset { name = "Легкий", mass = 1f, friction = 0.2f },
        new CubePreset { name = "Средний", mass = 3f, friction = 0.4f },
        new CubePreset { name = "Тяжелый", mass = 5f, friction = 0.6f }
    };

    private int currentCubeIndex = 0;
    private float playerSpeed = 5f; // Скорость игрока по умолчанию

    [Header("References")]
    public Rigidbody objectRb;
    public TMP_Text statusText; // для скорости

    [Header("References")]
    public Transform startPoint;
    public Transform finishPoint;
    // public TMP_Text forceText;
    public TMP_Text distanceText;

    [Header("UI Inputs")]
    public TMP_InputField playerForceInput;
    public TMP_InputField forceToPushInput;
    public Button pushButton;
    public Button previousButton;
    public Button nextButton;

    [Header("Finish UI")]
    public GameObject finishPanel; // Панель победы
    public TMP_Text resultsText;   // Текст результатов

    // Для отслеживания достижения финиша
    private bool reachedFinish = false;
    private float finishDistance = 4f;

    void Start()
    {
        // Назначаем обработчики кнопок
        pushButton.onClick.AddListener(PushCubeWithForce);
        previousButton.onClick.AddListener(PreviousCube);
        nextButton.onClick.AddListener(NextCube);

        // Устанавливаем начальные значения
        playerForceInput.text = playerSpeed.ToString("F1");
        forceToPushInput.text = "1"; // Сила толчка по умолчанию

        // Загружаем первый куб
        LoadCubePreset(currentCubeIndex);

        UpdateUI();
    }

    void Update()
    {
        PushCubeViaPressKey();
    }
    void FixedUpdate()
    {
        // Проверяем достижение финиша
        CheckFinish();
        UpdateUI();
    }

    // Толкание куба с заданной силой
    public void PushCubeWithForce()
    {
        if (float.TryParse(forceToPushInput.text, out float force))
        {
            objectRb.AddForce(Vector3.forward * force, ForceMode.Impulse);
            Debug.Log($"Куб толкнут с силой: {force}N");
        }
    }

    // Смена куба на предыдущий
    public void PreviousCube()
    {
        currentCubeIndex--;
        if (currentCubeIndex < 0) currentCubeIndex = cubePresets.Length - 1;
        LoadCubePreset(currentCubeIndex);
    }

    // Смена куба на следующий
    public void NextCube()
    {
        currentCubeIndex++;
        if (currentCubeIndex >= cubePresets.Length) currentCubeIndex = 0;
        LoadCubePreset(currentCubeIndex);
    }

    // Загрузка пресета куба
    private void LoadCubePreset(int index)
    {
        var preset = cubePresets[index];

        // Применяем параметры
        objectRb.mass = preset.mass;
        ApplyFriction(preset.friction);

        Debug.Log($"Загружен куб: {preset.name} (масса: {preset.mass}, трение: {preset.friction})");
    }

    // Применение трения через Physics Material
private void ApplyFriction(float friction)
{
    // Получаем компонент Collider с объекта, который имеет Rigidbody
    Collider collider = objectRb.GetComponent<Collider>();
    
    // Проверяем, что коллайдер существует (на всякий случай)
    if (collider != null)
    {
        // Получаем текущий Physic Material из коллайдера
        // Physic Material определяет физические свойства поверхности
        PhysicsMaterial physicMat = collider.material;
        
        // Если Physic Material еще не назначен, создаем новый
        if (physicMat == null)
        {
            physicMat = new PhysicsMaterial();
            collider.material = physicMat;
        }

        // Устанавливаем динамическое трение - сопротивление при движении
        // Это сила, которая замедляет объект, когда он уже движется
        physicMat.dynamicFriction = friction;
        
        // Устанавливаем статическое трение - сопротивление началу движения
        // Обычно статическое трение больше динамического (примерно в 1.2 раза)
        // Это сила, которую нужно преодолеть, чтобы сдвинуть объект с места
        physicMat.staticFriction = friction * 1.2f;
    }
}

    // Обновление скорости игрока из UI
    public void UpdatePlayerSpeed()
    {
        if (float.TryParse(playerForceInput.text, out float speed))
        {
            playerSpeed = speed;
        }
    }

    private void CheckFinish()
    {
        if (!reachedFinish)
        {
            float currentDistance = Vector3.Distance(objectRb.position, startPoint.position);
            if (currentDistance >= finishDistance)
            {
                reachedFinish = true;
                OnFinishReached();
            }
        }
    }

    public void OnFinishReached()
    {
        Debug.Log($"🎉 ФИНИШ! Объект прошел {finishDistance} метров!");

        // Показываем панель победы
        if (finishPanel != null)
        {
            finishPanel.SetActive(true);

            // Заполняем результаты
            float actualDistance = Vector3.Distance(startPoint.position, objectRb.position);
            float time = Time.timeSinceLevelLoad;

            resultsText.text =
                $"ФИНИШ!\n\n" +
                $"Пройдено: {actualDistance:F2} м\n" +
                $"Время: {time:F1} сек";
        }

        // Меняем статус
        if (statusText != null)
        {
            statusText.color = Color.green;
            statusText.text = "ФИНИШ ДОСТИГНУТ!";
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"Скорость: {objectRb.linearVelocity.magnitude:F2} m/s\n" +
                             $"Куб: {cubePresets[currentCubeIndex].name}";
        }

        if (distanceText != null)
        {
            float distanceFromStart = Vector3.Distance(objectRb.position, startPoint.position);
            float distanceToFinish = Mathf.Max(0, finishDistance - distanceFromStart);

            distanceText.text = $"От старта: {distanceFromStart:F2} m\n" +
                                $"До финиша: {distanceToFinish:F2} m\n" +
                                $"Цель: {finishDistance} m";
        }
        UpdatePlayerSpeed();
    }

    public void PushCubeViaPressKey()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PushCubeWithForce();
        }
    }
}