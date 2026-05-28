using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualizationManager : MonoBehaviour
{
    [Header("Атомы")]
    public GameObject atomPrefab;
    public int atomCount = 20;
    public RectTransform atomContainer;
    public Color normalColor = Color.blue;
    public Color excitedColor = Color.yellow;
    
    [Header("Фотоны")]
    public GameObject photonPrefab;
    public RectTransform photonContainer;
    public int photonCount = 50;
    
    [Header("Лазерный луч")]
    public LineRenderer laserBeam;
    public Transform beamStart;
    public Transform beamEnd;
    
    [Header("Ссылки")]
    public LaserSimulator simulator;

    
    private List<Image> atoms = new List<Image>();
    private List<RectTransform> photons = new List<RectTransform>();
    private float photonTimer = 0;
    private int currentPhotonIndex = 0;
    
    void Start()
    {
        CreateAtoms();
        CreatePhotons();
        
        if (simulator != null)
            simulator.OnSimulationUpdate += OnUpdate;
        
        if (laserBeam != null)
        {
            laserBeam.enabled = false;
            laserBeam.startWidth = 0.05f;
            laserBeam.endWidth = 0.05f;
            laserBeam.startColor = Color.red;
            laserBeam.endColor = Color.yellow;
        }
    }
    
    void CreateAtoms()
    {
        if (atomPrefab == null || atomContainer == null) return;
        
        float w = atomContainer.rect.width;
        float h = atomContainer.rect.height;
        
        for (int i = 0; i < atomCount; i++)
        {
            GameObject atom = Instantiate(atomPrefab, atomContainer);
            RectTransform rect = atom.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                Random.Range(-w/2 + 30, w/2 - 30),
                Random.Range(-h/2 + 30, h/2 - 30)
            );
            atoms.Add(atom.GetComponent<Image>());
        }
    }
    
    void CreatePhotons()
    {
        if (photonPrefab == null || photonContainer == null) return;
        
        for (int i = 0; i < photonCount; i++)
        {
            GameObject p = Instantiate(photonPrefab, photonContainer);
            RectTransform rect = p.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-1000, Random.Range(-100, 100));
            photons.Add(rect);
            p.SetActive(false);
        }
    }
    
    void OnUpdate(float inversion, bool lasing)
    {
        // Обновление атомов (цвета)
        float t = Mathf.Clamp01(inversion / 2f);
        int excited = Mathf.FloorToInt(atoms.Count * t);
        for (int i = 0; i < atoms.Count; i++)
            if (atoms[i] != null)
                atoms[i].color = (i < excited) ? excitedColor : normalColor;
        
        // ========== ФОТОНЫ С ЗАВИСИМОСТЬЮ ОТ ИНВЕРСИИ ==========
        if (lasing)
        {
            // Чем выше инверсия, тем чаще рождаются фотоны
            float intensity = Mathf.Clamp01(inversion);
            float creationInterval = Mathf.Lerp(0.3f, 0.03f, intensity);
            
            photonTimer += Time.deltaTime;
            if (photonTimer > creationInterval)
            {
                photonTimer = 0;
                
                // Запускаем новый фотон
                for (int i = 0; i < photons.Count; i++)
                {
                    if (!photons[i].gameObject.activeSelf)
                    {
                        photons[i].gameObject.SetActive(true);
                        photons[i].anchoredPosition = new Vector2(-500, Random.Range(-20, 20));
                        break;
                    }
                }
            }
            
            // Двигаем все активные фотоны (скорость тоже зависит от инверсии)
            float speed = Mathf.Lerp(500f, 2000f, intensity);  // 500 → 2000
            
            foreach (var photon in photons)
            {
                if (photon.gameObject.activeSelf)
                {
                    Vector2 pos = photon.anchoredPosition;
                    pos.x += speed * Time.deltaTime;
                    if (pos.x > 600)
                        photon.gameObject.SetActive(false);
                    else
                        photon.anchoredPosition = pos;
                }
            }
        }
        else
        {
            // Скрываем все фотоны при отсутствии генерации
            foreach (var p in photons)
                if (p.gameObject.activeSelf) 
                    p.gameObject.SetActive(false);
        }
        
        // Обновление лазерного луча
        if (laserBeam != null)
        {
            laserBeam.enabled = lasing;
            if (lasing && beamStart != null && beamEnd != null)
            {
                laserBeam.SetPosition(0, beamStart.position);
                laserBeam.SetPosition(1, beamEnd.position);
            }
        }
    }
    
    void OnDestroy()
    {
        if (simulator != null)
            simulator.OnSimulationUpdate -= OnUpdate;
    }
}