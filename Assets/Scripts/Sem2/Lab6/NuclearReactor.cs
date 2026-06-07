using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class NuclearReactor : MonoBehaviour
{
    [Header("Префабы")]
    public GameObject neutronPrefab;
    public GameObject uraniumPrefab;
    public Transform absorberZone;
    
    [Header("Параметры")]
    [Range(0, 1)] public float fissionProbability = 1.0f;
    public float neutronLifetime = 2f;
    public float neutronSpeed = 3f;
    
    [Header("UI Text (TMP)")]
    public TMP_Text activeNucleiText;
    public TMP_Text neutronCountText;
    public TMP_Text totalEnergyText;
    public TMP_Text statusText;
    public TMP_Text theoryText;
    
    private List<GameObject> activeNuclei = new List<GameObject>();
    private List<GameObject> neutrons = new List<GameObject>();
    private float totalEnergy = 0f;
    private float energyPerFission = 200f;
    
    void Start()
    {
        CreateUraniumGrid3D();
        SpawnInitialNeutron();
        UpdateUI();
        ShowTheory();
    }
    
    void CreateUraniumGrid3D()
    {
        // 3D сетка: X и Z вместо X и Y
        for (int x = -6; x <= 6; x++)
        {
            for (int z = -4; z <= 4; z++)
            {
                Vector3 pos = new Vector3(x * 1.2f, 0f, z * 1.2f);
                GameObject nucleus = Instantiate(uraniumPrefab, pos, Quaternion.identity);
                nucleus.tag = "Uranium";
                
                var nukeScript = nucleus.AddComponent<NucleusTag>();
                nukeScript.isActive = true;
                
                activeNuclei.Add(nucleus);
            }
        }
    }
    
    void SpawnInitialNeutron()
    {
        SpawnNeutron(Vector3.zero);
    }
    
    public void SpawnNeutron(Vector3 position)
    {
        GameObject neutron = Instantiate(neutronPrefab, position, Quaternion.identity);
        neutron.tag = "Neutron";
        
        var neutronScript = neutron.AddComponent<Neutron>();
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        neutronScript.Init(this, direction * neutronSpeed, neutronLifetime);
        
        neutrons.Add(neutron);
        UpdateUI();
    }
    
    public void OnNeutronHitNucleus(GameObject neutronObj, GameObject nucleusObj)
    {
        NucleusTag nucleus = nucleusObj.GetComponent<NucleusTag>();
        if (!nucleus.isActive) return;
        
        if (Random.value > fissionProbability) return;
        
        nucleus.isActive = false;
        activeNuclei.Remove(nucleusObj);
        
        // Меняем цвет через Renderer (3D)
        Renderer rend = nucleusObj.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(0.5f, 0.5f, 0.5f);
        
        totalEnergy += energyPerFission;
        
        int newNeutrons = Random.value < 0.4f ? 3 : 2;
        for (int i = 0; i < newNeutrons; i++)
        {
            SpawnNeutron(nucleusObj.transform.position);
        }
        
        neutrons.Remove(neutronObj);
        Destroy(neutronObj);
        
        UpdateUI();
    }
    
    public void OnNeutronHitAbsorber(GameObject neutronObj)
    {
        neutrons.Remove(neutronObj);
        Destroy(neutronObj);
        UpdateUI();
    }
    
    public void RemoveNeutron(GameObject neutronObj)
    {
        if (neutrons.Contains(neutronObj))
            neutrons.Remove(neutronObj);
        Destroy(neutronObj);
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (activeNucleiText != null)
            activeNucleiText.text = "Активных ядер: " + activeNuclei.Count;
        
        if (neutronCountText != null)
            neutronCountText.text = "Нейтронов: " + neutrons.Count;
        
        if (totalEnergyText != null)
        {
            double energyJoules = totalEnergy * 1.602e-13;
            if (energyJoules < 1e-6)
                totalEnergyText.text = $"Энергия: {totalEnergy:F1} МэВ";
        }
        
        // ФИЗИЧЕСКИ ПРАВИЛЬНЫЙ k (коэффициент размножения)
        float k;
        if (activeNuclei.Count == 0)
            k = 0f;
        else if (neutrons.Count == 0)
            k = 0f;
        else
        {
            // Реалистичный k: зависит от нейтронов и вероятности
            k = fissionProbability * 2.4f * (neutrons.Count / (float)(activeNuclei.Count + neutrons.Count + 1));
            k = Mathf.Clamp(k, 0.5f, 1.5f);  // ограничиваем реалистичным диапазоном
        }
        
        string state;
        if (k < 0.99f) state = "ПОДКРИТИЧЕСКАЯ (k<1)";
        else if (k > 1.01f) state = "СВЕРХКРИТИЧЕСКАЯ (k>1)";
        else state = "КРИТИЧЕСКАЯ (k=1)";
        
        if (statusText != null)
            statusText.text = $"Состояние: {state}\nk ≈ {k:F3}\nВероятность: {fissionProbability:F2}";
        
        if (activeNuclei.Count == 0 && statusText != null)
            statusText.text = "РЕАКЦИЯ ОСТАНОВЛЕНА\nТопливо выгорело\nk = 0";
    }
    
    void ShowTheory()
    {
        if (theoryText != null)
        {
            theoryText.text = "=== ТЕОРИЯ ===\n" +
                "1. Дефект массы → E = Δm·c²\n" +
                "2. Нестабильность: n/p дисбаланс\n" +
                "3. Критическая масса: k=1\n" +
                "4. График: рост→плато(стержни)→спад";
        }
    }
}