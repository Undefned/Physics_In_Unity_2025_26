using UnityEngine;

[CreateAssetMenu(fileName = "New Pump", menuName = "Laser/Pump Type")]
public class PumpType : ScriptableObject
{
    public string pumpName;
    [Range(0, 2f)] public float powerCoefficient; // эффективность накачки
    public string workingPrinciple;
    [TextArea] public string description;
    
    // Совместимость хранится в отдельной матрице, но для удобства добавим поле
    // Оно будет заполняться через редактор
}