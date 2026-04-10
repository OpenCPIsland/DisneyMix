using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class RebuildAssetBundles
{
	private static readonly string OutputRoot = Application.streamingAssetsPath + "\\AssetBundles";
	private const string RawBuildFolderName = "_raw";
	private const string DynamicAssetsRoot = "Assets\\DynamicAssets";
	private const string AssetBundlesJsonRelativePath = "Assets/Resources/json/AssetBundles.json";

	[MenuItem("Tools/AssetBundles/Rebuild All")]
	public static void RebuildAll()
	{
		string rawBuildPath = Path.Combine(OutputRoot, RawBuildFolderName);

		if (Directory.Exists(OutputRoot))
		{
			Directory.Delete(OutputRoot, true);
		}

		Directory.CreateDirectory(OutputRoot);
		Directory.CreateDirectory(rawBuildPath);

		BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle;
		BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

		AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(rawBuildPath, options, target);
		if (manifest == null)
		{
			Debug.LogError("AssetBundle rebuild failed.");
			return;
		}

		RestructureOutputFromDynamicAssets(rawBuildPath, OutputRoot, DynamicAssetsRoot, manifest);

		if (Directory.Exists(rawBuildPath))
		{
			Directory.Delete(rawBuildPath, true);
		}

		AssetDatabase.Refresh();
		Debug.Log("AssetBundles rebuilt and restructured at: " + Path.GetFullPath(OutputRoot));
	}

	private static void RestructureOutputFromDynamicAssets(string rawBuildPath, string outputRoot, string dynamicAssetsRoot, AssetBundleManifest manifest)
	{
		Dictionary<string, string> bundleToRelativeFolder = BuildBundleFolderMap(dynamicAssetsRoot, outputRoot);
		string miscPath = Path.Combine(outputRoot, "misc");
		Directory.CreateDirectory(miscPath);

		Debug.Log("[RebuildAssetBundles] Bundle map entries: " + bundleToRelativeFolder.Count);

		string[] builtBundles = manifest.GetAllAssetBundles();
		foreach (string bundleName in builtBundles)
		{
			string relativeFolder = ResolveRelativeFolder(bundleName, bundleToRelativeFolder);
			if (string.IsNullOrEmpty(relativeFolder))
			{
				relativeFolder = "misc";
				Debug.LogWarning("[RebuildAssetBundles] Unmapped bundle -> misc: " + bundleName);
			}

			string destinationFolder = Path.Combine(outputRoot, relativeFolder);
			Directory.CreateDirectory(destinationFolder);

			string sourceBundlePath = Path.Combine(rawBuildPath, bundleName.Replace('/', Path.DirectorySeparatorChar));
			string sourceManifestPath = sourceBundlePath + ".manifest";

			MoveFileIfExists(sourceBundlePath, Path.Combine(destinationFolder, Path.GetFileName(sourceBundlePath)));
			MoveFileIfExists(sourceManifestPath, Path.Combine(destinationFolder, Path.GetFileName(sourceManifestPath)));
		}

		// Move root-level leftovers (e.g., main manifest) into misc.
		foreach (string sourceFilePath in Directory.GetFiles(rawBuildPath))
		{
			MoveFileIfExists(sourceFilePath, Path.Combine(miscPath, Path.GetFileName(sourceFilePath)));
		}
	}

	private static Dictionary<string, string> BuildBundleFolderMap(string dynamicAssetsRoot, string outputRoot)
	{
		Dictionary<string, string> map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

		AddFileNameMappingsFromAssetBundlesJson(outputRoot, map);
		AddFileNameMappingsFromDynamicAssets(dynamicAssetsRoot, map);

		Debug.Log("[RebuildAssetBundles] BuildBundleFolderMap entries: " + map.Count);
		return map;
	}

	private static void AddFileNameMappingsFromAssetBundlesJson(string outputRoot, Dictionary<string, string> map)
	{
		string jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "StreamingAssets", Path.GetFileName(AssetBundlesJsonRelativePath)));
		if (!File.Exists(jsonPath))
		{
			Debug.LogWarning("assetbundles.json not found at: " + jsonPath + ". Falling back to DynamicAssets folder mapping.");
			return;
		}

		string json = File.ReadAllText(jsonPath);
		MatchCollection matches = Regex.Matches(json, "\"(?<key>[^\"]+)\"\\s*:\\s*\\{", RegexOptions.Compiled);
		foreach (Match match in matches)
		{
			string bundleKey = match.Groups["key"].Value;
			string relativeFolder = ResolveFolderFromAssetBundleKey(bundleKey, outputRoot);
			if (string.IsNullOrEmpty(relativeFolder))
			{
				continue;
			}

			string bundleName = Path.GetFileName(bundleKey);
			string bundleNameNoExt = Path.GetFileNameWithoutExtension(bundleKey);
			AddMapKey(map, bundleKey.ToLowerInvariant(), relativeFolder);
			AddMapVariants(map, bundleName, relativeFolder);
			AddMapVariants(map, bundleNameNoExt, relativeFolder);
		}
	}

	private static string ResolveFolderFromAssetBundleKey(string bundleKey, string outputRoot)
	{
		if (string.IsNullOrEmpty(bundleKey))
		{
			return string.Empty;
		}

		string normalizedKey = bundleKey.Replace("\\", "/").Trim();
		string normalizedOutputRoot = outputRoot.Replace("\\", "/").Trim('/');
		if (normalizedKey.StartsWith(normalizedOutputRoot + "/", System.StringComparison.OrdinalIgnoreCase))
		{
			normalizedKey = normalizedKey.Substring(normalizedOutputRoot.Length + 1);
		}

		if (normalizedKey.EndsWith(".unity3d", System.StringComparison.OrdinalIgnoreCase))
		{
			normalizedKey = normalizedKey.Substring(0, normalizedKey.Length - ".unity3d".Length);
		}

		string relativeFolder = Path.GetDirectoryName(normalizedKey) ?? string.Empty;
		relativeFolder = relativeFolder.Replace("\\", "/");
		if (string.IsNullOrEmpty(relativeFolder))
		{
			relativeFolder = "root";
		}

		return relativeFolder;
	}

	private static void AddFileNameMappingsFromDynamicAssets(string dynamicAssetsRoot, Dictionary<string, string> map)
	{
		string normalizedRoot = dynamicAssetsRoot.Replace("\\", "/").TrimEnd('/') + "/";

		foreach (string file in Directory.GetFiles(dynamicAssetsRoot, "*", SearchOption.AllDirectories))
		{
			if (file.EndsWith(".meta"))
			{
				continue;
			}

			string normalizedFile = file.Replace("\\", "/");
			string relativePath = normalizedFile.Substring(normalizedRoot.Length);
			string relativeFolder = Path.GetDirectoryName(relativePath) ?? string.Empty;
			relativeFolder = relativeFolder.Replace("\\", "/");
			if (string.IsNullOrEmpty(relativeFolder))
			{
				relativeFolder = "root";
			}

			string fileNameNoExt = Path.GetFileNameWithoutExtension(relativePath).ToLowerInvariant();

			// Custom: If file contains both "avtr" and "thumb", map to "avatar_thumbs"
			if (fileNameNoExt.Contains("avtr") && fileNameNoExt.Contains("thumb"))
			{
				AddMapKey(map, fileNameNoExt, "avatar_thumbs");
				AddMapKey(map, fileNameNoExt + ".unity3d", "avatar_thumbs");
				if (fileNameNoExt.EndsWith("_hd"))
				{
					AddMapKey(map, fileNameNoExt.Substring(0, fileNameNoExt.Length - 3), "avatar_thumbs");
				}
				else if (fileNameNoExt.EndsWith("_sd"))
				{
					AddMapKey(map, fileNameNoExt.Substring(0, fileNameNoExt.Length - 3), "avatar_thumbs");
				}
				continue; // Skip default mapping for these files
			}

			AddMapKey(map, fileNameNoExt, relativeFolder);

			// common bundle naming variants
			AddMapKey(map, fileNameNoExt + ".unity3d", relativeFolder);

			if (fileNameNoExt.EndsWith("_hd"))
			{
				AddMapKey(map, fileNameNoExt.Substring(0, fileNameNoExt.Length - 3), relativeFolder);
			}
			else if (fileNameNoExt.EndsWith("_sd"))
			{
				AddMapKey(map, fileNameNoExt.Substring(0, fileNameNoExt.Length - 3), relativeFolder);
			}
		}
	}

	private static void AddMapKey(Dictionary<string, string> map, string key, string relativeFolder)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}

		if (!map.ContainsKey(key))
		{
			map.Add(key, relativeFolder);
		}
	}

	private static void AddMapVariants(Dictionary<string, string> map, string bundleName, string relativeFolder)
	{
		string exact = bundleName.ToLowerInvariant();
		string file = Path.GetFileName(bundleName).ToLowerInvariant();
		string noExt = Path.GetFileNameWithoutExtension(bundleName).ToLowerInvariant();

		if (!map.ContainsKey(exact))
		{
			map.Add(exact, relativeFolder);
		}
		if (!map.ContainsKey(file))
		{
			map.Add(file, relativeFolder);
		}
		if (!map.ContainsKey(noExt))
		{
			map.Add(noExt, relativeFolder);
		}
	}

	private static string ResolveRelativeFolder(string bundleName, Dictionary<string, string> map)
	{
		string exact = bundleName.ToLowerInvariant();
		string file = Path.GetFileName(bundleName).ToLowerInvariant();
		string noExt = Path.GetFileNameWithoutExtension(bundleName).ToLowerInvariant();

		string value;
		if (map.TryGetValue(exact, out value))
		{
			return value;
		}
		if (map.TryGetValue(file, out value))
		{
			return value;
		}
		if (map.TryGetValue(noExt, out value))
		{
			return value;
		}

		// quality suffix fallback: *_hd / *_sd
		if (noExt.EndsWith("_hd"))
		{
			string baseName = noExt.Substring(0, noExt.Length - 3);
			if (map.TryGetValue(baseName, out value))
			{
				return value;
			}
		}
		else if (noExt.EndsWith("_sd"))
		{
			string baseName = noExt.Substring(0, noExt.Length - 3);
			if (map.TryGetValue(baseName, out value))
			{
				return value;
			}
		}

		// Similar-name fallback (mainly for thumbs like thumbsdownprefab_hd.unity3d)
		string normalizedBundle = NormalizeForSimilarity(noExt);
		int bestScore = -1;
		string bestFolder = null;

		foreach (KeyValuePair<string, string> kv in map)
		{
			string normalizedKey = NormalizeForSimilarity(kv.Key);
			if (string.IsNullOrEmpty(normalizedKey))
			{
				continue;
			}

			int score = 0;
			if (normalizedBundle == normalizedKey)
			{
				score = 100;
			}
			else if (normalizedBundle.StartsWith(normalizedKey) || normalizedKey.StartsWith(normalizedBundle))
			{
				score = 80;
			}
			else if (normalizedBundle.Contains(normalizedKey) || normalizedKey.Contains(normalizedBundle))
			{
				score = 60;
			}

			if (score > bestScore)
			{
				bestScore = score;
				bestFolder = kv.Value;
			}
		}

		if (bestScore >= 60)
		{
			Debug.Log("[RebuildAssetBundles] Similar match: " + bundleName + " -> " + bestFolder + " (score " + bestScore + ")");
			return bestFolder;
		}

		return null;
	}

	private static string NormalizeForSimilarity(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}

		string s = input.ToLowerInvariant();
		s = s.Replace(".unity3d", string.Empty);
		s = s.Replace("_hd", string.Empty);
		s = s.Replace("_sd", string.Empty);
		s = s.Replace("thumbs", string.Empty);
		s = s.Replace("thumb", string.Empty);
		s = s.Replace("prefab", string.Empty);
		s = s.Replace("_", string.Empty);
		s = s.Replace("-", string.Empty);

		return s.Trim();
	}

	private static void MoveFileIfExists(string sourcePath, string destinationPath)
	{
		if (!File.Exists(sourcePath))
		{
			return;
		}

		string parent = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
		{
			Directory.CreateDirectory(parent);
		}

		if (File.Exists(destinationPath))
		{
			File.Delete(destinationPath);
		}

		File.Move(sourcePath, destinationPath);
	}
}