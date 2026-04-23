using UnityEditor;
using UnityEngine;

public static class MissingScriptsCleaner
{
    [MenuItem("Tools/Lab/Clean Missing Scripts In Open Scenes")]
    public static void CleanOpenScenes()
    {
        int removed = 0;
        int checkedObjects = 0;

        GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in roots)
        {
            if (EditorUtility.IsPersistent(go))
            {
                continue;
            }

            if (go.hideFlags != HideFlags.None)
            {
                continue;
            }

            checkedObjects++;
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        Debug.Log($"MissingScriptsCleaner: checked={checkedObjects}, removed={removed}");
    }
}
