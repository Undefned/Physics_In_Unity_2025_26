using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider wavelengthSlider;
    public Slider intensitySlider;
    public Slider voltageSlider;
    public Dropdown materialDropdown;
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
            float voltage = voltageSlider != null ? voltageSlider.value : 0f;
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
        if (lightSource != null)
            lightSource.SetWavelength(value);
    }
    
    void OnIntensityChanged(float value)
    {
        if (lightSource != null)
            lightSource.SetIntensity(value);
    }
    
    void OnVoltageChanged(float value)
    {
        // Передаём напряжение на электроны (можно через статическую переменную)
        ElectronMovement.voltage = value;
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
                materialDropdown.options.Add(new Dropdown.OptionData(mat.name));
            }
            materialDropdown.RefreshShownValue();
        }
    }
}

public class TMPro_Text
{
}