using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider wavelengthSlider;
    public Slider intensitySlider;
    public Slider voltageSlider;
    public TMP_Dropdown materialDropdown;
    public TMP_Text voltmeterText;
    public TMP_Text infoText;
    
    [Header("Объекты сцены")]
    public LightSource lightSource;
    public Cathode cathode;
    public Anode anode;
    
    void Start()
    {
        // Назначаем обработчики событий
        wavelengthSlider.onValueChanged.AddListener(OnWavelengthChanged);
        intensitySlider.onValueChanged.AddListener(OnIntensityChanged);
        voltageSlider.onValueChanged.AddListener(OnVoltageChanged);
        materialDropdown.onValueChanged.AddListener(OnMaterialChanged);
        
        // Инициализация
        OnWavelengthChanged(wavelengthSlider.value);
        OnIntensityChanged(intensitySlider.value);
        OnMaterialChanged(materialDropdown.value);
        
        // Заполняем выпадающий список
        UpdateMaterialDropdown();
    }
    
    void Update()
    {
        // Обновляем вольтметр
        if (voltmeterText != null)
        {
            // Преобразуем нормализованное значение (0-1) в реальное напряжение (-5V до +5V)
            float voltage = voltageSlider != null ? Mathf.Lerp(-5f, 5f, voltageSlider.value) : 0f;
            voltmeterText.text = $"Задерживающее напряжение: {voltage:F2} V";
        }
        
        // Обновляем информационный текст
        if (infoText != null && lightSource != null && cathode != null)
        {
            float photonEnergy = lightSource.GetPhotonEnergy();
            float kineticEnergy = cathode.GetKineticEnergy(photonEnergy);
            
            infoText.text = $"Фотон: {photonEnergy:F2} эВ\n" +
                           $"Работа выхода: {cathode.workFunction:F2} эВ\n" +
                           $"Кин. энергия: {kineticEnergy:F2} эВ\n" +
                           $"Ток: {anode.GetCurrent():F2} у.е.";
        }
    }
    
    void OnWavelengthChanged(float value)
    {
        // value - нормализованное значение 0-1, преобразуем в диапазон 200-800 нм
        float wavelength = Mathf.Lerp(200f, 800f, value);
        Debug.Log($"[UIManager] OnWavelengthChanged: slider={value}, wavelength={wavelength} нм");
        if (lightSource != null)
            lightSource.SetWavelength(wavelength);
        else
            Debug.LogError("[UIManager] LightSource не назначен!");
    }

    void OnIntensityChanged(float value)
    {
        // value - нормализованное значение 0-1, преобразуем в диапазон 0-100
        float intensity = Mathf.Lerp(0f, 100f, value);
        Debug.Log($"[UIManager] OnIntensityChanged: slider={value}, intensity={intensity}");
        if (lightSource != null)
            lightSource.SetIntensity(intensity);
        else
            Debug.LogError("[UIManager] LightSource не назначен!");
    }
    
    void OnVoltageChanged(float value)
    {
        float voltage = Mathf.Lerp(-5f, 5f, value);
        // Передаём напряжение на электроны (можно через статическую переменную)
        ElectronMovement.voltage = voltage;
    }
    
    void OnMaterialChanged(int index)
    {
        if (cathode != null)
            cathode.SetMaterial(index);
    }
    
    void UpdateMaterialDropdown()
    {
        if (materialDropdown != null && cathode != null && cathode.availableMaterials != null)
        {
            materialDropdown.ClearOptions();
            foreach (var mat in cathode.availableMaterials)
            {
                materialDropdown.options.Add(new TMP_Dropdown.OptionData(mat.name));
            }
            materialDropdown.RefreshShownValue();
        }
    }
}