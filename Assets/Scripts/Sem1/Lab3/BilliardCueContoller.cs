using UnityEngine;
using UnityEngine.InputSystem;

public class BilliardCueController : MonoBehaviour
{
    [Header("References")]
    public GameObject cue; // Объект кия
    public GameObject cueBall; // Объект битка
    public Camera tableCamera; // Камера для расчета лучей
    
    [Header("Swing Settings")]
    public float rotationSpeed = 2f; // Скорость вращения кия вокруг битка
    public float maxPower = 20f; // Максимальная сила удара
    public float powerMultiplier = 1.5f; // Множитель нарастания силы
    private bool isAiming = false; // Флаг режима прицеливания
    private float currentPower = 0f; // Текущая сила удара
    private Vector3 cueOffset; // Смещение кия относительно битка
    private Mouse mouse; // Ссылка на устройство мыши

    void Start()
    {
        // Получаем ссылку на мышь из Input System
        mouse = Mouse.current;
        // Начальное смещение кия за битком
        cueOffset = new Vector3(0, 0, -0.3f);
        // Скрываем кий в начале
        DeactivateCue();
    }

    void Update()
    {
        // Проверяем доступность мыши
        if (mouse == null) return;

        // Обработка нажатия левой кнопки мыши
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartAiming();
        }
        
        // Если в режиме прицеливания - обрабатываем ввод
        if (isAiming)
        {
            HandleAiming();
            HandlePowerCharge();
        }
        
        // Обработка отпускания кнопки мыши для удара
        if (mouse.leftButton.wasReleasedThisFrame && isAiming)
        {
            ExecuteStrike();
        }
    }

    void StartAiming()
    {
        // Создаем луч от камеры к позиции мыши
        Ray ray = tableCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        
        // Проверяем попадание луча в биток
        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == cueBall)
        {
            // Активируем режим прицеливания
            isAiming = true;
            ActivateCue();
        }
    }

    void HandleAiming()
    {
        // Поворот кия с помощью мыши
        // Читаем изменение позиции мыши по X
        float mouseX = mouse.delta.x.ReadValue() * 0.1f;
        // Вращаем смещение кия вокруг битка
        cueOffset = Quaternion.Euler(0, mouseX * rotationSpeed, 0) * cueOffset;
        // Обновляем позицию кия
        UpdateCuePosition();
    }

    void HandlePowerCharge()
    {
        // Если кнопка мыши зажата - накапливаем силу удара
        if (mouse.leftButton.isPressed)
        {
            // Увеличиваем силу со временем
            currentPower += Time.deltaTime * powerMultiplier;
            // Ограничиваем максимальной силой
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);
            // Обновляем визуальную обратную связь
            UpdateCuePosition();
        }
    }

    void UpdateCuePosition()
    {
        if (cueBall == null) return;
        
        // Расчет позиции кия:
        // Базовая позиция + смещение + оттягивание для визуализации силы
        Vector3 cuePosition = cueBall.transform.position + cueOffset.normalized * (0.8f + currentPower * 0.03f);
        // Устанавливаем позицию кия
        cue.transform.position = cuePosition;
        // Поворачиваем кий чтобы он смотрел на биток
        cue.transform.LookAt(cueBall.transform.position);
    }

    void ExecuteStrike()
    {
        if (cueBall == null) return;
        
        // Получаем Rigidbody битка
        Rigidbody ballRb = cueBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // Направление удара - противоположно направлению кия
            Vector3 strikeDirection = -cueOffset.normalized;
            // Применяем силу к битку
            ballRb.AddForce(strikeDirection * currentPower, ForceMode.Impulse);
        }
        
        // Сбрасываем состояние после удара
        ResetCue();
    }

    void ActivateCue()
    {
        // Показываем кий
        cue.SetActive(true);
        // Сбрасываем силу удара
        currentPower = 0f;
        // Обновляем позицию
        UpdateCuePosition();
    }

    void DeactivateCue()
    {
        // Скрываем кий
        cue.SetActive(false);
    }

    void ResetCue()
    {
        // Сбрасываем все параметры прицеливания
        isAiming = false;
        currentPower = 0f;
        DeactivateCue();
    }
}