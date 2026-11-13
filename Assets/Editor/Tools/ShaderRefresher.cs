using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ShaderRefresher {
    [MenuItem("Tools/Project/Refresh UI Materials")]
    static void Refresh() {
        // This forces Unity to update every material's internal property block
        Shader.WarmupAllShaders();
        AssetDatabase.Refresh();
        Debug.Log("UI Materials Refreshed!");
    }
}