using UnityEngine;
using System.Collections;

public class LightSource : MonoBehaviour
{
    [Header("Настройки")]
    public float wavelength = 550f; // нм
    public float intensity = 50f; // относительная интенсивность
    
    [Header("Компоненты")]
    public Light pointLight;
    public Cathode cathode;
    public GameObject electronPrefab;
    public Transform anodeTransform;
    
    [Header("Константы")]
    private const float h = 6.626e-34f; // постоянная Планка
    private const float c = 3e8f; // скорость света
    private const float e = 1.6e-19f; // заряд электрона
    private const float eVToJoule = 1.6e-19f;
    
    private float photonEnergy; // в эВ
    private float timer = 0f;
    
    void Start()
    {
        if (pointLight == null)
            pointLight = GetComponent<Light>();
        
        UpdateWavelength();
    }
    
    void Update()
    {
        // Генерация фотонов с частотой, зависящей от интенсивности
        float photonsPerSecond = intensity * 10f;
        float timeBetweenPhotons = 1f / Mathf.Max(1, photonsPerSecond);
        
        timer += Time.deltaTime;
        while (timer >= timeBetweenPhotons)
        {
            timer -= timeBetweenPhotons;
            EmitPhoton();
        }
    }
    
    void EmitPhoton()
    {
        if (cathode == null) return;
        
        // Проверяем, может ли фотон выбить электрон
        if (cathode.CanEjectElectron(photonEnergy))
        {
            // Вычисляем кинетическую энергию электрона
            float kineticEnergyEV = cathode.GetKineticEnergy(photonEnergy);
            float kineticEnergyJ = kineticEnergyEV * eVToJoule;
            
            // Вычисляем скорость электрона
            float electronMass = 9.11e-31f;
            float velocity = Mathf.Sqrt(2 * kineticEnergyJ / electronMass);
            
            // Создаём визуализацию электрона
            SpawnElectron(velocity);
        }
        else
        {
            // Фотон не выбил электрон — можно добавить визуальный эффект (вспышка)
            // Debug.Log($"Фотон с энергией {photonEnergy:F2} эВ не выбил электрон (работа выхода {cathode.workFunction:F2} эВ)");
        }
    }
    
    void SpawnElectron(float velocity)
    {
        if (electronPrefab == null) return;
        
        GameObject electron = Instantiate(electronPrefab, cathode.transform.position + Vector3.right * 0.5f, Quaternion.identity);
        ElectronMovement movement = electron.GetComponent<ElectronMovement>();
        if (movement != null)
        {
            movement.velocity = velocity;
            movement.anode = anodeTransform;
        }
        
        Destroy(electron, 2f); // самоуничтожение через 2 секунды
    }
    
    public void UpdateWavelength()
    {
        wavelength = Mathf.Clamp(wavelength, 200f, 800f);
        
        // Вычисляем энергию фотона в эВ
        // E = h*c/λ, результат в джоулях, переводим в эВ
        float wavelengthM = wavelength * 1e-9f;
        float energyJ = (h * c) / wavelengthM;
        photonEnergy = energyJ / eVToJoule;
        
        // Меняем цвет источника света
        Color lightColor = WavelengthToColor(wavelength);
        pointLight.color = lightColor;
        
        // Меняем цвет сферы (источника)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = lightColor;
        
        Debug.Log($"Длина волны: {wavelength} нм, Энергия фотона: {photonEnergy:F2} эВ");
    }
    
    Color WavelengthToColor(float wavelength)
    {
        // Приближённое преобразование длины волны в цвет
        if (wavelength < 380) return Color.white;
        if (wavelength < 440) return new Color(0.5f, 0f, 1f);
        if (wavelength < 490) return new Color(0f, 0.5f, 1f);
        if (wavelength < 510) return new Color(0f, 1f, 0.5f);
        if (wavelength < 580) return new Color(0.5f, 1f, 0f);
        if (wavelength < 645) return new Color(1f, 0.5f, 0f);
        return new Color(1f, 0f, 0f);
    }
    
    public float GetPhotonEnergy() => photonEnergy;
    
    public void SetWavelength(float value)
    {
        wavelength = value;
        UpdateWavelength();
    }
    
    public void SetIntensity(float value)
    {
        intensity = value;
        if (pointLight != null)
            pointLight.intensity = value / 20f;
    }
}