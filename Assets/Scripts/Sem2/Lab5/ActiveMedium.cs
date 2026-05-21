using UnityEngine;

[CreateAssetMenu(fileName = "New Medium", menuName = "Laser/Active Medium")]
public class ActiveMedium : ScriptableObject
{
    public string mediumName;
    public float wavelength; // nm
    public float lifetime; // ms, время жизни метастабильного уровня
    public float thresholdInversion; // порог инверсии (отн. ед.)
    public SchemeType schemeType;
    public Texture2D energyLevelDiagram; // картинка диаграммы уровней
    [TextArea] public string description;

    public enum SchemeType
    {
        ThreeLevel,
        FourLevel
    }
}