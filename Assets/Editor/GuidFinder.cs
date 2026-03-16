using UnityEditor;
using UnityEngine;

public class GuidFinder : Editor {
    [MenuItem("Tools/Find Asset by GUID")]
    public static void FindAsset() {
        string guid = "4e97a996c92aac2419ba1259a06cfb38";
        string path = AssetDatabase.GUIDToAssetPath(guid);
        
        if (!string.IsNullOrEmpty(path)) {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"Asset found at: {path}");
        } else {
            Debug.LogError("No asset found with that GUID.");
        }
    }
}
