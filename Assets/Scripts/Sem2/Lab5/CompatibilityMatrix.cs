using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CompatibilityMatrix", menuName = "Laser/Compatibility Matrix")]
public class CompatibilityMatrix : ScriptableObject
{
    [Serializable]
    public class CompatibilityRule
    {
        public string mediumName;
        public string pumpName;
        public bool isCompatible;
        [TextArea] public string reasonIfNotCompatible;
    }

    public List<CompatibilityRule> rules;

    public bool IsCompatible(ActiveMedium medium, PumpType pump, out string reason)
    {
        reason = "";
        
        foreach (var rule in rules)
        {
            if (rule.mediumName == medium.mediumName && rule.pumpName == pump.pumpName)
            {
                if (!rule.isCompatible)
                    reason = rule.reasonIfNotCompatible;
                return rule.isCompatible;
            }
        }
        
        // По умолчанию совместимы, но с предупреждением
        reason = "Совместимость явно не определена в матрице";
        return true;
    }
}