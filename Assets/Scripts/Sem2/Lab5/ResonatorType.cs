using UnityEngine;

[CreateAssetMenu(fileName = "New Resonator", menuName = "Laser/Resonator Type")]
public class ResonatorType : ScriptableObject
{
    public string resonatorName;
    public float R1; // радиус кривизны зеркала 1 (мм) (∞ = 999999)
    public float R2; // радиус кривизны зеркала 2 (мм)
    public float length; // мм
    [Range(0.01f, 0.99f)] public float outputTransmission; // прозрачность выходного зеркала
    public float waistRadius; // мм (можно вычислить, но для простоты - задаём)
    [TextArea] public string description;
}