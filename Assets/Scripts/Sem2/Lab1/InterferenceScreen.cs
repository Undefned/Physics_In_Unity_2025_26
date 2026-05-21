using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InterferenceScreen : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ВОЛН (общие для двух источников) ===")]
    [Range(0.2f, 1.2f)] public float wavelength = 0.5f;   // длина волны (относительная, 0.2-1.2)
    
    [Header("=== ГЕОМЕТРИЯ ===")]
    [Range(0.3f, 4.0f)] public float distanceBetweenSources = 1.0f;  // d - расстояние между источниками (усл. ед.)
    [Range(2f, 12f)]    public float distanceToScreen = 5f;           // L - расстояние до экрана (усл. ед.)
    
    [Header("=== СИЛА ИСТОЧНИКОВ (амплитуды) ===")]
    [Range(0f, 2f)]     public float amplitude1 = 1f;   // A₁ - амплитуда первого источника
    [Range(0f, 2f)]     public float amplitude2 = 1f;   // A₂ - амплитуда второго источника

    [Header("=== НАСТРОЙКИ ОТОБРАЖЕНИЯ ===")]
    public int resolution = 512;           // разрешение текстуры экрана (пикселей по ширине)
    public float screenLeft = -6f;         // левая граница экрана в мировых координатах
    public float screenRight = 6f;         // правая граница экрана в мировых координатах
    
    [Header("=== ВИЗУАЛЬНЫЕ НАСТРОЙКИ ===")]
    [Tooltip("Если включено — амплитуды влияют на общую яркость экрана")]
    public bool amplitudeAffectsBrightness = true;   // режим: яркость от амплитуд (true) или только контраст (false)

    private Texture2D texture;             // текстура 1xN для отображения интерференционной картины
    private Renderer rend;                 // рендерер объекта-экрана
    public TMP_Text infoText;              // UI текст для вывода параметров

    void Start()
    {
        rend = GetComponent<Renderer>();                                   // получаем компонент рендерера
        texture = new Texture2D(resolution, 1);                            // создаём текстуру шириной resolution, высотой 1 пиксель
        texture.filterMode = FilterMode.Bilinear;                          // билинейная фильтрация для плавности
        rend.material.mainTexture = texture;                               // назначаем текстуру на материал экрана
        
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = Color.black;                     // фон камеры - чёрный
        }
    }

    void Update()
    {
        UpdatePattern();    // каждый кадр обновляем интерференционную картину
    }

    void UpdatePattern()
    {
        // позиции источников на оси X (симметрично относительно центра)
        float source1X = -distanceBetweenSources / 2f;   // левый источник
        float source2X = distanceBetweenSources / 2f;    // правый источник

        // конвертация в реальные единицы ТОЛЬКО ДЛЯ ВЫВОДА В UI
        float wavelengthNM = 400f + (wavelength - 0.2f) * (300f / 1.0f);   // 0.2→400нм, 1.2→640нм
        float distanceMM = distanceBetweenSources;                         // мм (0.3-4.0)
        float fringeWidthMM = (wavelengthNM * 1e-6f * distanceToScreen) / (distanceMM * 0.001f) * 1000f;  // ширина полосы в мм

        // вывод информации на UI
        if (infoText != null)
        {
            infoText.text = $"ИНТЕРФЕРЕНЦИЯ ДВУХ ИСТОЧНИКОВ\n" +
                        $"λ (длина волны) ........ {wavelengthNM:F0} нм\n" +
                        $"d (между источниками) ... {distanceMM:F1} мм\n" +
                        $"L (до экрана) .......... {distanceToScreen:F2} м\n" +
                        $"A1 (амплитуда 1-го) ..... {amplitude1:F2}\n" +
                        $"A2 (амплитуда 2-го) ..... {amplitude2:F2}\n" +
                        $"Ширина полосы = λ·L / d = {fringeWidthMM:F2} мм\n";
        }

        // цикл по всем пикселям экрана (от i=0 до resolution-1)
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);                         // нормализованная координата (0..1)
            float x = Mathf.Lerp(screenLeft, screenRight, t);              // мировые координаты X точки на экране

            // расстояние от точки экрана до первого источника
            float r1 = Mathf.Sqrt((x - source1X) * (x - source1X) + distanceToScreen * distanceToScreen);
            // расстояние от точки экрана до второго источника
            float r2 = Mathf.Sqrt((x - source2X) * (x - source2X) + distanceToScreen * distanceToScreen);

            float pathDiff = r2 - r1;                                      // разность хода волн
            float phaseDiff = 2f * Mathf.PI * pathDiff / wavelength;       // разность фаз

            // === ФИЗИЧЕСКАЯ ИНТЕНСИВНОСТЬ (формула интерференции) ===
            float rawIntensity = amplitude1 * amplitude1 + amplitude2 * amplitude2 +
                              2f * amplitude1 * amplitude2 * Mathf.Cos(phaseDiff);  // I = A1² + A2² + 2·A1·A2·cos(Δφ)
            
            float intensity;    // нормированная интенсивность (0..1)

            if (amplitudeAffectsBrightness)
            {
                // РЕЖИМ 1: амплитуды влияют на общую яркость
                float maxPossible = (amplitude1 + amplitude2) * (amplitude1 + amplitude2);  // (A1+A2)² - максимум при cos=+1
                if (maxPossible <= 0.001f)
                    intensity = 0f;                                    // оба источника выключены -> темно
                else
                    intensity = rawIntensity / maxPossible;            // нормировка относительно текущих амплитуд
            }
            else
            {
                // РЕЖИМ 2: только контраст (макс/мин масштабируются в 0..1)
                float maxI = (amplitude1 + amplitude2) * (amplitude1 + amplitude2);  // максимум
                float minI = (amplitude1 - amplitude2) * (amplitude1 - amplitude2);  // минимум
                
                if (Mathf.Approximately(maxI, minI) || maxI == 0f)      // защита от деления на ноль (нет интерференции)
                {
                    if (amplitude1 == 0 && amplitude2 == 0)
                        intensity = 0f;                                 // оба выключены
                    else if (amplitude1 == 0 || amplitude2 == 0)
                        intensity = 0.5f;                               // один источник -> равномерный серый
                    else
                        intensity = 0.5f;                               // A1=A2 -> средний серый
                }
                else
                {
                    intensity = (rawIntensity - minI) / (maxI - minI);   // нормировка в диапазон [0..1]
                }
            }
            
            intensity = Mathf.Clamp01(intensity);                       // обрезаем до [0,1] на всякий случай
            
            // гамма-коррекция для усиления контраста (темные темнее, светлые светлее)
            intensity = Mathf.Pow(intensity, 1.2f);
            
            // линейная интерполяция: чёрный (минимум) → жёлтый (максимум)
            Color color = Color.Lerp(Color.black, new Color(1f, 0.85f, 0.2f), intensity);
            texture.SetPixel(i, 0, color);                               // устанавливаем цвет пикселя в текстуре
        }
        texture.Apply();    // применяем изменения текстуры
    }

    // ===== МЕТОДЫ ДЛЯ ПОЛЗУНКОВ =====
    public void SetWavelength(float val)                    { wavelength = val; }          // ползунок 1: длина волны
    public void SetDistanceBetweenSources(float val)        { distanceBetweenSources = val; } // ползунок 2: расстояние между источниками
    public void SetDistanceToScreen(float val)              { distanceToScreen = val; }    // ползунок 3: расстояние до экрана
    public void SetAmplitude1(float val)                    { amplitude1 = val; }          // ползунок 4: амплитуда источника 1
    public void SetAmplitude2(float val)                    { amplitude2 = val; }          // ползунок 5: амплитуда источника 2
    
    public void SetAmplitudeAffectsBrightness(bool value)   { amplitudeAffectsBrightness = value; } // переключение режима (Toggle)
}