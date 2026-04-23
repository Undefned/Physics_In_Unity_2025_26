using System;
using System.Drawing;
using UnityEngine;

[System.Serializable]
public class MaterialData
{
    public string name;
    public float workFunction; // в эВ
    public System.Drawing.Color cathodeColor;
}

[System.Serializable]
public class MaterialsDatabase : ScriptableObject
{
    public MaterialData[] materials;
}