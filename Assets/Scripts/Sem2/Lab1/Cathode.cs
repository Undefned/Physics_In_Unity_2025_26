using UnityEngine;
public class Cathode : MonoBehaviour
{
    [Header("Материалы")]
    public MaterialData[] availableMaterials;
    private MaterialData currentMaterial;
    
    [Header("Параметры")]
    public float workFunction; // в эВ
    public float electronEmissionRate; // количество электронов/сек
    
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        SetMaterial(0); // Цезий по умолчанию
    }
    
    public void SetMaterial(int index)
    {
        if (index >= 0 && index < availableMaterials.Length)
        {
            currentMaterial = availableMaterials[index];
            workFunction = currentMaterial.workFunction;
            var color = currentMaterial.cathodeColor;
            meshRenderer.material.color = new UnityEngine.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

            Debug.Log($"Материал: {currentMaterial.name}, Работа выхода: {workFunction} эВ");
        }
    }
    
    public bool CanEjectElectron(float photonEnergy)
    {
        return photonEnergy > workFunction;
    }
    
    public float GetKineticEnergy(float photonEnergy)
    {
        return Mathf.Max(0, photonEnergy - workFunction);
    }
}