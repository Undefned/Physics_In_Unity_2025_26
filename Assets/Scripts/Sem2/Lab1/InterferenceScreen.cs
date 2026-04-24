using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InterferenceScreen : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ВОЛН (общие для двух источников) ===")]
    [Range(0.2f, 1.2f)] public float wavelength = 0.5f;
    
    [Header("=== ГЕОМЕТРИЯ ===")]
    [Range(0.3f, 4.0f)] public float distanceBetweenSources = 1.0f;
    [Range(2f, 12f)]    public float distanceToScreen = 5f;
    
    [Header("=== СИЛА ИСТОЧНИКОВ (амплитуды) ===")]
    [Range(0f, 2f)]     public float amplitude1 = 1f;
    [Range(0f, 2f)]     public float amplitude2 = 1f;

    [Header("=== НАСТРОЙКИ ОТОБРАЖЕНИЯ ===")]
    public int resolution = 512;
    public float screenLeft = -6f;
    public float screenRight = 6f;
    
    [Header("=== ВИЗУАЛЬНЫЕ НАСТРОЙКИ ===")]
    [Tooltip("Если включено — амплитуды влияют на общую яркость экрана")]
    public bool amplitudeAffectsBrightness = true;  // НОВЫЙ ПАРАМЕТР!

    private Texture2D texture;
    private Renderer rend;
    public TMP_Text infoText;

    void Start()
    {
        rend = GetComponent<Renderer>();
        texture = new Texture2D(resolution, 1);
        texture.filterMode = FilterMode.Bilinear;
        rend.material.mainTexture = texture;
        
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = Color.black;
        }
    }

    void Update()
    {
        UpdatePattern();
    }

    void UpdatePattern()
    {
        float source1X = -distanceBetweenSources / 2f;
        float source2X = distanceBetweenSources / 2f;
        float fringeWidth = (wavelength * distanceToScreen) / distanceBetweenSources;

        // Максимально возможная интенсивность (если бы оба источника были в фазе и с max амплитудой)
        float globalMaxPossible = 4f; // когда A1=2, A2=2, cos=1 → (2+2)²=16, но мы нормируем хитро
        
        // ДЛЯ ОТОБРАЖЕНИЯ В ТЕКСТЕ
        float contrastValue = (amplitude1 == 0 || amplitude2 == 0) ? 0 : 
                               (amplitude1 + amplitude2) * (amplitude1 + amplitude2) - 
                               (amplitude1 - amplitude2) * (amplitude1 - amplitude2);
        contrastValue = Mathf.Clamp01(contrastValue / 4f);
        
        if (infoText != null)
        {
            infoText.text = $"ИНТЕРФЕРЕНЦИЯ ДВУХ ИСТОЧНИКОВ\n" +
                           $"λ (длина волны) ........ {wavelength:F2}\n" +
                           $"d (между источниками) ... {distanceBetweenSources:F2}\n" +
                           $"L (до экрана) .......... {distanceToScreen:F2}\n" +
                           $"A1 (сила 1-го) .......... {amplitude1:F2}\n" +
                           $"A2 (сила 2-го) .......... {amplitude2:F2}\n" +
                           $"Ширина полосы = λ·L / d = {fringeWidth:F3}\n" +
                           $"Желтый = максимум | Черный = минимум";
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float x = Mathf.Lerp(screenLeft, screenRight, t);

            float r1 = Mathf.Sqrt((x - source1X) * (x - source1X) + distanceToScreen * distanceToScreen);
            float r2 = Mathf.Sqrt((x - source2X) * (x - source2X) + distanceToScreen * distanceToScreen);

            float pathDiff = r2 - r1;
            float phaseDiff = 2f * Mathf.PI * pathDiff / wavelength;

            // === ФИЗИЧЕСКАЯ ИНТЕНСИВНОСТЬ (без нормировки) ===
            float rawIntensity = amplitude1 * amplitude1 + amplitude2 * amplitude2 +
                              2f * amplitude1 * amplitude2 * Mathf.Cos(phaseDiff);
            
            float intensity;

            if (amplitudeAffectsBrightness)
            {
                // РЕЖИМ 1: Амплитуды влияют на общую яркость
                // Максимальная возможная интенсивность при данных амплитудах
                float maxPossible = (amplitude1 + amplitude2) * (amplitude1 + amplitude2);
                if (maxPossible <= 0.001f)
                    intensity = 0f;
                else
                    intensity = rawIntensity / maxPossible; // Нормируем ОТНОСИТЕЛЬНО текущих амплитуд
            }
            else
            {
                // РЕЖИМ 2: Только контраст (как было раньше)
                float maxI = (amplitude1 + amplitude2) * (amplitude1 + amplitude2);
                float minI = (amplitude1 - amplitude2) * (amplitude1 - amplitude2);
                
                if (Mathf.Approximately(maxI, minI) || maxI == 0f)
                {
                    if (amplitude1 == 0 && amplitude2 == 0)
                        intensity = 0f;
                    else if (amplitude1 == 0 || amplitude2 == 0)
                        intensity = 0.5f;
                    else
                        intensity = 0.5f;
                }
                else
                {
                    intensity = (rawIntensity - minI) / (maxI - minI);
                }
            }
            
            intensity = Mathf.Clamp01(intensity);
            
            // НЕБОЛЬШОЕ УСИЛЕНИЕ КОНТРАСТА ДЛЯ НАГЛЯДНОСТИ
            // Делаем темные участки чуть темнее, светлые — чуть светлее
            intensity = Mathf.Pow(intensity, 1.2f);
            
            // Цвет: чёрный (минимум) → ярко-жёлтый (максимум)
            Color color = Color.Lerp(Color.black, new Color(1f, 0.85f, 0.2f), intensity);
            texture.SetPixel(i, 0, color);
        }
        texture.Apply();
    }

    // ===== МЕТОДЫ ДЛЯ ПОЛЗУНКОВ =====
    public void SetWavelength(float val)                    { wavelength = val; }
    public void SetDistanceBetweenSources(float val)        { distanceBetweenSources = val; }
    public void SetDistanceToScreen(float val)              { distanceToScreen = val; }
    public void SetAmplitude1(float val)                    { amplitude1 = val; }
    public void SetAmplitude2(float val)                    { amplitude2 = val; }
    
    // НОВЫЙ МЕТОД для переключения режима (можно привязать к Toggle)
    public void SetAmplitudeAffectsBrightness(bool value)   { amplitudeAffectsBrightness = value; }
}