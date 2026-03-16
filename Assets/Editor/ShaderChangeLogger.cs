using UnityEngine;
using UnityEditor;
using System.IO;

public class ShaderChangeLogger : EditorWindow
{
    [MenuItem("Tools/Inject Shader Logs")]
    public static void InjectLogs()
    {
        // The common "culprits" where shaders are swapped or materials assigned
        string[] searchPatterns = { ".shader =", "new Material(", "Shader.Find(" };
        string[] allFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        int count = 0;
        foreach (string file in allFiles)
        {
            // Skip the searcher scripts themselves to avoid infinite loops
            if (file.Contains("ScriptSearcher") || file.Contains("ShaderChangeLogger")) continue;

            string content = File.ReadAllText(file);
            bool modified = false;

            foreach (string pattern in searchPatterns)
            {
                if (content.Contains(pattern))
                {
                    // This adds a Debug.Log right before the line that changes the shader
                    // It includes the filename so you know exactly who is doing it
                    string fileName = Path.GetFileName(file);
                    string logLine = $"Debug.Log(\"<color=red>[SHADERTOPPER]</color> Shader change detected in: {fileName}\");\n";
                    
                    // Simple injection: puts the log before the pattern
                    content = content.Replace(pattern, logLine + pattern);
                    modified = true;
                    count++;
                }
            }

            if (modified)
            {
                File.WriteAllText(file, content);
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Successfully injected {count} logs into your scripts. Check the console when the shader swaps!");
    }
}