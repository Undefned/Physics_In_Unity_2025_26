using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PresetExperiments : MonoBehaviour
{
    public LaserSimulator simulator;
    public VisualizationManager visualizer;
    
    [Header("UI для вывода результатов")]
    public TextMeshProUGUI experimentResultText;
    
    // Ссылки на дропдауны (чтобы программно менять)
    public TMP_Dropdown mediumDropdown;
    public TMP_Dropdown pumpDropdown;
    public TMP_Dropdown resonatorDropdown;
    
    public void Experiment1_Threshold()
    {
        // He-Ne + газовый разряд + полусферический резонатор
        SetPreset("He-Ne", "Газовый разряд", "Полусферический");
        simulator.pumpPowerSlider.value = 0;
        simulator.isSimulating = true;
        
        experimentResultText.text = "ЭКСПЕРИМЕНТ 1: Порог генерации\n" +
                                    "Конфигурация: He-Ne + газовый разряд + полусферический\n" +
                                    "Медленно увеличивайте мощность.\n" +
                                    "Зафиксируйте мощность, при которой загорится зеленый индикатор.";
    }
    
    public void Experiment2_ThreeVsFourLevel()
    {
        // Сравнить рубин (3-уровневая) и Nd:YAG (4-уровневая)
        experimentResultText.text = "ЭКСПЕРИМЕНТ 2: Трёх- vs четырёхуровневая схема\n" +
                                    "1. Выберите Рубин, ламповую накачку, резонатор любой\n" +
                                    "2. Зафиксируйте пороговую мощность\n" +
                                    "3. Выберите Nd:YAG, ту же накачку, тот же резонатор\n" +
                                    "4. Сравните пороги\n\n" +
                                    "Ожидание: У рубина порог ВЫШЕ из-за 3-уровневой схемы";
    }
    
    public void Experiment3_ResonatorGeometry()
    {
        // Nd:YAG + диодная накачка, мощность 80%
        SetPreset("Nd:YAG", "Диодная", "");
        simulator.pumpPowerSlider.value = 80;
        
        experimentResultText.text = "ЭКСПЕРИМЕНТ 3: Влияние геометрии резонатора\n" +
                                    "Nd:YAG + диодная накачка, мощность 80%\n\n" +
                                    "Тестируйте по очереди 5 типов резонаторов:\n" +
                                    "1. Плоско-параллельный | 2. Концентрический | 3. Конфокальный\n" +
                                    "4. Полусферический | 5. Кольцевой\n\n" +
                                    "Результаты запишите в таблицу: устойчивость | расходимость | генерация";
    }
    
    void SetPreset(string mediumName, string pumpName, string resonatorName)
    {
        // Находим индексы в дропдаунах
        int mediumIndex = mediumDropdown.options.FindIndex(opt => opt.text == mediumName);
        int pumpIndex = pumpDropdown.options.FindIndex(opt => opt.text == pumpName);
        
        if (mediumIndex >= 0) mediumDropdown.value = mediumIndex;
        if (pumpIndex >= 0) pumpDropdown.value = pumpIndex;
        
        if (!string.IsNullOrEmpty(resonatorName))
        {
            int resIndex = resonatorDropdown.options.FindIndex(opt => opt.text == resonatorName);
            if (resIndex >= 0) resonatorDropdown.value = resIndex;
        }
        
        mediumDropdown.RefreshShownValue();
        pumpDropdown.RefreshShownValue();
        resonatorDropdown.RefreshShownValue();
        
        simulator.UpdateComponentSelection();
    }
    
    // Кнопка для экспорта результатов эксперимента
    public void ExportResults()
    {
        string results = $"Результаты эксперимента на {System.DateTime.Now}:\n" +
                         $"Активная среда: {simulator.currentMedium.mediumName}\n" +
                         $"Накачка: {simulator.currentPump.pumpName}\n" +
                         $"Резонатор: {simulator.currentResonator.resonatorName}\n" +
                         $"Мощность: {simulator.pumpPower}%\n" +
                         $"Инверсия: {simulator.currentInversion:F3}\n" +
                         $"Генерация: {(simulator.isLasing ? "ДА" : "НЕТ")}\n";
        
        Debug.Log(results);
        experimentResultText.text = results + "\nРезультат скопирован в консоль (Ctrl+C)";
        
        // Копируем в буфер обмена (работает в сборке)
        GUIUtility.systemCopyBuffer = results;
    }
}