using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LaserSimulator : MonoBehaviour
{
    [Header("Компоненты лазера")]
    public List<ActiveMedium> availableMediums;
    public List<PumpType> availablePumps;
    public List<ResonatorType> availableResonators;
    public CompatibilityMatrix compatibilityMatrix;
    
    [Header("UI элементы")]
    public TMP_Dropdown mediumDropdown;
    public TMP_Dropdown pumpDropdown;
    public TMP_Dropdown resonatorDropdown;
    public Slider pumpPowerSlider;
    public TextMeshProUGUI powerValueText;
    public Button startStopButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI inversionText;
    public TextMeshProUGUI divergenceText;
    public TextMeshProUGUI wavelengthText;
    public GameObject generationIndicator; // зеленый/красный индикатор
    
    [Header("Параметры симуляции")]
    [Range(0, 1)] public float pumpPower = 0; // 0-100%
    public bool isSimulating = false;
    public float updateInterval = 0.05f; // 20 раз в секунду
    
    // Текущие выбранные компоненты
    public ActiveMedium currentMedium;
    public PumpType currentPump;
    public ResonatorType currentResonator;
    public float currentInversion = 0;
    public bool isLasing = false;
    private float timer = 0;
    
    // События для визуализации
    public System.Action<float, bool> OnSimulationUpdate; // инверсия, лазер ли?
    
    void Start()
    {
        InitializeUI();
        UpdateComponentSelection();
        UpdateUI();
        
        startStopButton.onClick.AddListener(ToggleSimulation);
        pumpPowerSlider.onValueChanged.AddListener(OnPowerChanged);
        mediumDropdown.onValueChanged.AddListener((i) => UpdateComponentSelection());
        pumpDropdown.onValueChanged.AddListener((i) => UpdateComponentSelection());
        resonatorDropdown.onValueChanged.AddListener((i) => UpdateComponentSelection());
    }
    
    void Update()
    {
        if (!isSimulating) return;
        
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0;
            SimulateStep();
        }
    }
    
    void InitializeUI()
    {
        // Заполняем дропдауны
        mediumDropdown.ClearOptions();
        pumpDropdown.ClearOptions();
        resonatorDropdown.ClearOptions();
        
        mediumDropdown.AddOptions(availableMediums.ConvertAll(m => m.mediumName));
        pumpDropdown.AddOptions(availablePumps.ConvertAll(p => p.pumpName));
        resonatorDropdown.AddOptions(availableResonators.ConvertAll(r => r.resonatorName));
    }
    
    public void UpdateComponentSelection()
    {
        currentMedium = availableMediums[mediumDropdown.value];
        currentPump = availablePumps[pumpDropdown.value];
        currentResonator = availableResonators[resonatorDropdown.value];
        
        wavelengthText.text = $"Длина волны: {currentMedium.wavelength} нм";
    }
    
    void OnPowerChanged(float value)
    {
        pumpPower = value;
        powerValueText.text = $"{value:F0}%";
    }
    
    void ToggleSimulation()
    {
        isSimulating = !isSimulating;
        startStopButton.GetComponentInChildren<TextMeshProUGUI>().text = isSimulating ? "Остановить" : "Запустить";
        
        if (!isSimulating)
        {
            currentInversion = 0;
            isLasing = false;
            UpdateUI();
        }
    }
    
    void SimulateStep()
    {
        // 1. Проверка совместимости
        bool compatible = CheckCompatibility(currentMedium, currentPump, out string compatReason);
        if (!compatible)
        {
            statusText.text = $"НЕСОВМЕСТИМО: {compatReason}";
            isLasing = false;
            UpdateUI();
            return;
        }
        
        // 2. Расчет инверсии (простая линейная модель)
        currentInversion = CalculateInversion(currentMedium, currentPump, pumpPower);
        
        // 3. Проверка устойчивости резонатора
        bool stable = CheckResonatorStability(currentResonator);
        if (!stable)
        {
            statusText.text = "Резонатор НЕУСТОЙЧИВ - генерации нет";
            isLasing = false;
            UpdateUI();
            return;
        }
        
        // 4. Проверка порога генерации
        bool thresholdReached = currentInversion >= currentMedium.thresholdInversion;
        bool mediumActive = currentMedium.schemeType == ActiveMedium.SchemeType.FourLevel || 
                            (currentMedium.schemeType == ActiveMedium.SchemeType.ThreeLevel && currentInversion >= currentMedium.thresholdInversion * 1.5f);
        
        isLasing = compatible && stable && thresholdReached && mediumActive;
        
        // 5. Расчет расходимости пучка (если есть генерация)
        float divergence = 0;
        if (isLasing)
        {
            divergence = CalculateBeamDivergence(currentMedium.wavelength, currentResonator.waistRadius);
            statusText.text = $"ГЕНЕРАЦИЯ!";
        }
        else
        {
            if (!thresholdReached)
                statusText.text = $"Порог не достигнут: {currentInversion:F2} / {currentMedium.thresholdInversion}";
            else if (!mediumActive && currentMedium.schemeType == ActiveMedium.SchemeType.ThreeLevel)
                statusText.text = $"3-уровневая среда: нужна инверсия > {currentMedium.thresholdInversion * 1.5f:F2}";
        }
        
        UpdateUI();
        
        // Вызываем событие для визуализации
        OnSimulationUpdate?.Invoke(currentInversion, isLasing);
    }
    
    public bool CheckCompatibility(ActiveMedium medium, PumpType pump, out string reason)
    {
        return compatibilityMatrix.IsCompatible(medium, pump, out reason);
    }
    
    public float CalculateInversion(ActiveMedium medium, PumpType pump, float powerPercent)
    {
        float normalizedPower = Mathf.Clamp01(powerPercent);
        
        // Базовая инверсия: мощность * КПД накачки * базовый множитель
        float inversion = normalizedPower * pump.powerCoefficient * 1.2f;  // +20% для запаса
        
        // Для 3-уровневой схемы (рубин) снижаем КПД
        if (medium.schemeType == ActiveMedium.SchemeType.ThreeLevel)
            inversion *= 0.5f;
        
        // Ограничиваем разумным максимумом
        inversion = Mathf.Min(inversion, 2.0f);
        
        return inversion;
    }
        
    public bool CheckResonatorStability(ResonatorType resonator)
    {
        // Для бесконечности (плоское зеркало) используем g = 1
        float g1 = (resonator.R1 >= 999998) ? 1f : (1f - resonator.length / resonator.R1);
        float g2 = (resonator.R2 >= 999998) ? 1f : (1f - resonator.length / resonator.R2);
        float product = g1 * g2;
        
        // Диапазон устойчивости: 0 < g1*g2 < 1
        // НЕ включаем границы (0 и 1) - они неустойчивы
        bool stable = (product > 0.001f && product < 0.999f);
        
        // Для отладки - выводим в консоль
        Debug.Log($"Resonator: {resonator.resonatorName}, g1={g1:F3}, g2={g2:F3}, product={product:F3}, stable={stable}");
        
        return stable;
    }
    
    public float CalculateBeamDivergence(float wavelengthNm, float waistRadiusMm)
    {
        // θ = λ / (π * w0)
        float wavelengthMm = wavelengthNm / 1_000_000f; // nm -> mm
        float divergence = wavelengthMm / (Mathf.PI * waistRadiusMm);
        return divergence;
    }
    
    void UpdateUI()
    {
        inversionText.text = $"Инверсия: {currentInversion:F3} / {currentMedium.thresholdInversion}";
        
        if (isLasing)
        {
            float divergence = CalculateBeamDivergence(currentMedium.wavelength, currentResonator.waistRadius);
            divergenceText.text = $"Расходимость: {divergence:F4} рад";
        }
        else
        {
            divergenceText.text = $"Расходимость: ---";
        }
        
        if (generationIndicator != null)
            generationIndicator.GetComponent<Image>().color = isLasing ? Color.green : Color.red;
    }
}