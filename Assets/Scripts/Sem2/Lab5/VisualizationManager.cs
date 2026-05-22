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
    public int photonCount = 15;
    
    [Header("Лазерный луч")]
    public LineRenderer laserBeam;
    public Transform beamStart;
    public Transform beamEnd;
    
    [Header("Ссылки")]
    public LaserSimulator simulator;
    
    private List<Image> atoms = new List<Image>();
    private List<RectTransform> photons = new List<RectTransform>();
    private float photonTimer = 0;
    
    void Start()
    {
        CreateAtoms();
        CreatePhotons();
        
        if (simulator != null)
            simulator.OnSimulationUpdate += OnUpdate;
        
        if (laserBeam != null)
        {
            laserBeam.enabled = false;
            laserBeam.startWidth = 0.1f;
            laserBeam.endWidth = 0.1f;
            laserBeam.startColor = Color.yellow;
            laserBeam.endColor = Color.red;
            
            if (laserBeam.material == null)
                laserBeam.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
    
    void CreateAtoms()
    {
        if (atomPrefab == null || atomContainer == null)
        {
            Debug.LogError("AtomPrefab или AtomContainer не назначен!");
            return;
        }
        
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
        
        Debug.Log($"Создано атомов: {atoms.Count}");
    }
    
    void CreatePhotons()
    {
        if (photonPrefab == null || photonContainer == null)
        {
            Debug.LogWarning("PhotonPrefab или PhotonContainer не назначен!");
            return;
        }
        
        for (int i = 0; i < photonCount; i++)
        {
            GameObject p = Instantiate(photonPrefab, photonContainer);
            RectTransform rect = p.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-500, 0); // Все на одной высоте
            photons.Add(rect);
            p.SetActive(false);
        }
    }
    
    void OnUpdate(float inversion, bool lasing)
    {
        // Атомы
        float t = Mathf.Clamp01(inversion / 2f);
        int excited = Mathf.FloorToInt(atoms.Count * t);
        for (int i = 0; i < atoms.Count; i++)
            if (atoms[i] != null)
                atoms[i].color = (i < excited) ? excitedColor : normalColor;
        
        // Фотоны
        if (lasing)
        {
            photonTimer += Time.deltaTime;
            if (photonTimer > 0.03f)
            {
                photonTimer = 0;
                foreach (var photon in photons)
                {
                    if (!photon.gameObject.activeSelf)
                    {
                        photon.gameObject.SetActive(true);
                        photon.anchoredPosition = new Vector2(-500, 0);
                        break;
                    }
                    else
                    {
                        Vector2 pos = photon.anchoredPosition;
                        pos.x += 80; // Быстрее
                        if (pos.x > 500)
                            photon.gameObject.SetActive(false);
                        else
                            photon.anchoredPosition = pos;
                    }
                }
            }
        }
        else
        {
            foreach (var p in photons)
                if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
        }
        
        // Луч
        if (laserBeam != null)
        {
            laserBeam.enabled = lasing;
            if (lasing && beamStart != null && beamEnd != null)
            {
                laserBeam.SetPosition(0, beamStart.position);
                laserBeam.SetPosition(1, beamEnd.position);
                Debug.Log($"Луч: {beamStart.position} → {beamEnd.position}");
            }
        }


        // ТЕСТ ЛУЧА (всегда включён)
        if (laserBeam != null)
        {
            laserBeam.enabled = true;  // принудительно
            
            if (beamStart != null && beamEnd != null)
            {
                laserBeam.SetPosition(0, beamStart.position);
                laserBeam.SetPosition(1, beamEnd.position);
                Debug.Log($"Луч включен! Start: {beamStart.position}, End: {beamEnd.position}");
            }
            else
            {
                Debug.LogError("beamStart или beamEnd не назначены!");
            }
        }
    }
    
    void OnDestroy()
    {
        if (simulator != null)
            simulator.OnSimulationUpdate -= OnUpdate;
    }
}