using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class ScriptSearcher : EditorWindow
{
    private string searchString = "";
    private List<string> results = new List<string>();
    private Vector2 scrollPos;
    private bool caseSensitive = false;

    [MenuItem("Tools/Modular Script Searcher")]
    public static void ShowWindow()
    {
        GetWindow<ScriptSearcher>("Script Searcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Search Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        searchString = EditorGUILayout.TextField("Find String:", searchString);
        caseSensitive = EditorGUILayout.Toggle("Case Sensitive", caseSensitive);

        if (GUILayout.Button("Search Whole Project", GUILayout.Height(30)))
        {
            PerformSearch();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Quick shortcuts for your specific ghost shader problem
        GUILayout.Label("Quick Ghost-Hunt Presets:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Spawners")) { searchString = "Instantiate"; PerformSearch(); }
        if (GUILayout.Button("Find Material Logic")) { searchString = ".material"; PerformSearch(); }
        if (GUILayout.Button("Find Shader Fixes")) { searchString = "Shader.Find"; PerformSearch(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (results.Count > 0)
        {
            GUILayout.Label($"Results Found: {results.Count}", EditorStyles.helpBox);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (string filePath in results)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(Path.GetFileName(filePath), EditorStyles.linkLabel))
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(filePath);
                    EditorGUIUtility.PingObject(obj);
                    AssetDatabase.OpenAsset(obj);
                }
                GUILayout.Label(filePath, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        else if (!string.IsNullOrEmpty(searchString))
        {
            GUILayout.Label("No results found.");
        }
    }

    private void PerformSearch()
    {
        results.Clear();
        if (string.IsNullOrEmpty(searchString)) return;

        // Using AssetDatabase to find all scripts is faster than Directory.GetFiles
        string[] guids = AssetDatabase.FindAssets("t:Script");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;

            string content = File.ReadAllText(path);

            bool found = false;
            if (caseSensitive)
            {
                found = content.Contains(searchString);
            }
            else
            {
                found = content.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (found)
            {
                results.Add(path);
            }
        }
    }
}