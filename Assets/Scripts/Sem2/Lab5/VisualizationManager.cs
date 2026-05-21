using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualizationManager : MonoBehaviour
{
    [Header("Атомы")]
    public GameObject atomPrefab;
    public int atomCount = 20;
    public RectTransform atomContainer;
    public Color normalAtomColor = Color.blue;
    public Color excitedAtomColor = Color.yellow;
    
    [Header("Фотоны (ParticleSystem)")]
    public ParticleSystem photonParticles;
    
    [Header("Лазерный луч")]
    public LineRenderer laserBeam;
    public Transform beamStart;
    public Transform beamEnd;
    
    [Header("Ссылки")]
    public LaserSimulator simulator;
    
    private List<Image> atomImages = new List<Image>();
    private float currentInversion = 0;
    private bool isLasing = false;
    private ParticleSystem.EmissionModule photonEmission;
    
    void Start()
    {
        CreateAtoms();
        
        if (simulator != null)
        {
            simulator.OnSimulationUpdate += OnSimulationUpdate;
        }
        
        // Настройка ParticleSystem
        if (photonParticles != null)
        {
            photonEmission = photonParticles.emission;
            photonEmission.rateOverTime = 0;
            photonParticles.Stop();
        }
        
        // Настройка луча
        if (laserBeam != null)
        {
            laserBeam.enabled = false;
            laserBeam.startWidth = 0.05f;
            laserBeam.endWidth = 0.05f;
            // Создаём простой материал для луча если его нет
            if (laserBeam.material == null)
            {
                laserBeam.material = new Material(Shader.Find("Sprites/Default"));
                laserBeam.startColor = Color.green;
                laserBeam.endColor = Color.red;
            }
        }
    }
    
    void CreateAtoms()
    {
        if (atomPrefab == null || atomContainer == null) return;
        
        for (int i = 0; i < atomCount; i++)
        {
            GameObject atom = Instantiate(atomPrefab, atomContainer);
            RectTransform rect = atom.GetComponent<RectTransform>();
            Image img = atom.GetComponent<Image>();
            
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(
                    Random.Range(50, atomContainer.rect.width - 50),
                    Random.Range(50, atomContainer.rect.height - 50)
                );
            }
            
            if (img != null)
            {
                atomImages.Add(img);
                img.color = normalAtomColor;
            }
        }
    }
    
    void OnSimulationUpdate(float inversion, bool lasing)
    {
        currentInversion = inversion;
        isLasing = lasing;
        
        // 1. Обновляем цвета атомов (чем выше инверсия, тем больше жёлтых)
        UpdateAtomColors();
        
        // 2. Обновляем фотоны (ParticleSystem)
        UpdatePhotons();
        
        // 3. Обновляем лазерный луч
        UpdateLaserBeam();
    }
    
    void UpdateAtomColors()
    {
        if (atomImages.Count == 0) return;
        
        // Нормализуем инверсию (0 - нет возбуждения, 1 - полностью возбуждены)
        float t = Mathf.Clamp01(currentInversion / 2f);
        int excitedCount = Mathf.FloorToInt(atomImages.Count * t);
        
        for (int i = 0; i < atomImages.Count; i++)
        {
            if (atomImages[i] != null)
            {
                atomImages[i].color = (i < excitedCount) ? excitedAtomColor : normalAtomColor;
            }
        }
    }
    
    void UpdatePhotons()
    {
        if (photonParticles == null) return;
        
        if (isLasing)
        {
            // Включаем частицы если они не играют
            if (!photonParticles.isPlaying)
            {
                photonParticles.Play();
            }
            
            // Чем выше инверсия, тем больше фотонов
            float rate = Mathf.Lerp(10f, 100f, Mathf.Clamp01(currentInversion));
            photonEmission.rateOverTime = rate;
        }
        else
        {
            // Останавливаем частицы
            if (photonParticles.isPlaying)
            {
                photonParticles.Stop();
            }
            photonEmission.rateOverTime = 0;
        }
    }
    
    void UpdateLaserBeam()
    {
        if (laserBeam == null) return;
        
        laserBeam.enabled = isLasing;
        
        if (isLasing && beamStart != null && beamEnd != null)
        {
            laserBeam.SetPosition(0, beamStart.position);
            laserBeam.SetPosition(1, beamEnd.position);
            
            // Меняем цвет в зависимости от мощности генерации
            float intensity = Mathf.Clamp01(currentInversion);
            laserBeam.startColor = new Color(1 - intensity, intensity, 0);
            laserBeam.endColor = new Color(1 - intensity * 0.5f, intensity * 0.8f, 0);
        }
    }
    
    void OnDestroy()
    {
        if (simulator != null)
        {
            simulator.OnSimulationUpdate -= OnSimulationUpdate;
        }
    }
}