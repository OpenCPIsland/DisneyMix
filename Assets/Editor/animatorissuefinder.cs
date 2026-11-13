using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class AnimatorStaticScanner : EditorWindow
{
    [MenuItem("Tools/Project/Deep Scan All Scripts")]
    public static void ScanAllScripts()
    {
        // 1. Get all C# scripts in the entire project
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
        int totalFound = 0;

        Debug.Log("<color=cyan><b>[Static Scan]</b> Analyzing all project scripts for Animator bugs...</color>");

        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;

            string content = File.ReadAllText(path);

            // 2. Look for .Play or .CrossFade calls
            // This regex finds the method call and captures the arguments inside
            MatchCollection matches = Regex.Matches(content, @"\.(Play|CrossFade)\(([^)]+)\)");

            foreach (Match match in matches)
            {
                string methodName = match.Groups[1].Value;
                string arguments = match.Groups[2].Value;
                string[] argArray = arguments.Split(',');

                // 3. Check for the "-1" Layer Bug or Hardcoded Strings
                if (arguments.Contains("-1"))
                {
                    Debug.LogError($"<b>[BUG FOUND]</b> In script: <color=yellow>{path}</color>\n" +
                                   $"Method <color=white>{methodName}</color> is passing <b>-1</b> as a layer index. This is an invalid index.", 
                                   AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                    totalFound++;
                }

                // 4. Highlight suspicious hardcoded strings
                if (argArray.Length > 0 && argArray[0].Contains("\""))
                {
                    string stateName = argArray[0].Trim().Replace("\"", "");
                    // Just a log to help you verify names
                    Debug.Log($"<color=white>[Logic Trace]</color> Script <b>{Path.GetFileName(path)}</b> calls {methodName} for state: \"{stateName}\"");
                }
            }
        }

        Debug.Log($"<color=green><b>[Scan Complete]</b> Processed {scriptGuids.Length} scripts. Found {totalFound} critical errors.</color>");
    }
}