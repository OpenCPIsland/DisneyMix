using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds data/CardsOfDoom/CardsOfDoom.gz, the list of cards the Would You
/// Rather game deals (WouldYouRatherGame: LoadData("data/CardsOfDoom/CardsOfDoom.gz",
/// "CardsOfDoom.txt", ParseJsonData), then CreateListOfChoices picks 10 at random).
///
/// Each card is a caption plus an image. The image is a bundle named
/// "<imageUrl>_hd.unity3d" in
/// StreamingAssets/AssetBundles/<platform>/hd/gogs/would_you_rather/game_assets/en_US/
/// (WouldYouRatherChoice appends "_hd.unity3d" to imageUrl and prefixes that folder).
///
/// Captions come from Assets/Editor/WouldYouRatherCards.txt, one card per line:
///     imageStem<TAB>caption text
/// Lines starting with # are ignored. A bundle with no line gets its file name
/// as a caption ("fly_like_a_bird" -> "Fly like a bird"). A line whose bundle
/// doesn't exist is skipped and reported.
/// To add a card: add the image to the game_assets bundle source, run
/// Tools > Mix > Rebuild Asset Bundles, add its caption line, then run this.
///
/// File format (ContentObj, same as the meme catalog, key "cod"):
///   gzip( {"content":{"objects":{"cod":[{"uid","text","imageUrl","order",...}]}}} )
///
/// ASSUMPTION TO VERIFY: the per-card fields uid/order/hidden/preview/isNew/tags/pushNote
/// are copied from the meme catalog's shared MixGameContentData fields. text and
/// imageUrl are confirmed by the game code. If cards fail to load, compare with
/// the Would_You_Rather_Data and MixGameContentData classes.
/// </summary>
public static class WouldYouRatherDataBuilder
{
	private const string AssetsRelative = "hd/gogs/would_you_rather/game_assets/en_US";
	private static readonly string[] Platforms = { "windows64", "standalonelinux64", "android", "iphone", "standalonemacos" };

	private static string BundlesRoot { get { return Path.Combine(Application.streamingAssetsPath, "AssetBundles"); } }
	private static string OutputPath { get { return Path.Combine(Application.streamingAssetsPath, "data/CardsOfDoom/CardsOfDoom.gz"); } }
	private static string CaptionsPath { get { return Path.Combine(Application.dataPath, "Editor/WouldYouRatherCards.txt"); } }

	[MenuItem("Tools/Mix/Build Would You Rather Data")]
	public static void Build()
	{
		string message;
		bool ok = TryBuild(out message);
		if (ok)
		{
			Debug.Log(message);
		}
		else
		{
			Debug.LogError(message);
		}
		EditorUtility.DisplayDialog("Would You Rather Data", message, "OK");
	}

	/// <summary>For -batchmode: -executeMethod WouldYouRatherDataBuilder.BuildBatch (exit 1 on failure).</summary>
	public static void BuildBatch()
	{
		string message;
		bool ok = TryBuild(out message);
		Console.WriteLine(message);
		EditorApplication.Exit(ok ? 0 : 1);
	}

	public static bool TryBuild(out string message)
	{
		string dir = null;
		string platform = null;
		foreach (string p in Platforms)
		{
			string candidate = Path.Combine(BundlesRoot, p + "/" + AssetsRelative);
			if (Directory.Exists(candidate))
			{
				dir = candidate;
				platform = p;
				break;
			}
		}
		if (dir == null)
		{
			message = "No Would You Rather bundles found under " + BundlesRoot + "/<platform>/" + AssetsRelative + ". Build the asset bundles first.";
			return false;
		}

		// Bundles that exist: stem -> true.
		HashSet<string> bundles = new HashSet<string>();
		foreach (string file in Directory.GetFiles(dir, "*_hd.unity3d"))
		{
			string name = Path.GetFileName(file);
			bundles.Add(name.Substring(0, name.Length - "_hd.unity3d".Length));
		}

		// Captions: stem -> text, in file order.
		List<KeyValuePair<string, string>> cards = new List<KeyValuePair<string, string>>();
		HashSet<string> captioned = new HashSet<string>();
		List<string> missingBundle = new List<string>();
		if (File.Exists(CaptionsPath))
		{
			foreach (string raw in File.ReadAllLines(CaptionsPath, Encoding.UTF8))
			{
				string line = raw.Trim('\r', '\n');
				if (line.Length == 0 || line.StartsWith("#"))
				{
					continue;
				}
				int tab = line.IndexOf('\t');
				if (tab <= 0 || tab == line.Length - 1)
				{
					continue;
				}
				string stem = line.Substring(0, tab).Trim();
				string text = line.Substring(tab + 1).Trim();
				if (!bundles.Contains(stem))
				{
					missingBundle.Add(stem);
					continue;
				}
				if (captioned.Add(stem))
				{
					cards.Add(new KeyValuePair<string, string>(stem, text));
				}
			}
		}

		// Bundles with no caption line: use the file name.
		List<string> autoNamed = new List<string>();
		foreach (string stem in bundles.OrderBy(s => s, StringComparer.Ordinal))
		{
			if (!captioned.Contains(stem))
			{
				cards.Add(new KeyValuePair<string, string>(stem, Prettify(stem)));
				autoNamed.Add(stem);
			}
		}

		if (cards.Count == 0)
		{
			message = "No <name>_hd.unity3d bundles in " + dir;
			return false;
		}

		StringBuilder json = new StringBuilder();
		json.Append("{\"content\":{\"objects\":{\"cod\":[");
		for (int i = 0; i < cards.Count; i++)
		{
			if (i > 0)
			{
				json.Append(',');
			}
			json.Append("{\"uid\":\"").Append(Esc(cards[i].Key))
				.Append("\",\"text\":\"").Append(Esc(cards[i].Value))
				.Append("\",\"imageUrl\":\"").Append(Esc(cards[i].Key))
				.Append("\",\"order\":").Append(i + 1)
				.Append(",\"hidden\":false,\"preview\":false,\"isNew\":false,\"tags\":\"\",\"pushNote\":\"\"}");
		}
		json.Append("]}}}");

		Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
		using (FileStream fs = File.Create(OutputPath))
		using (GZipStream gz = new GZipStream(fs, CompressionMode.Compress))
		{
			byte[] bytes = new UTF8Encoding(false).GetBytes(json.ToString());
			gz.Write(bytes, 0, bytes.Length);
		}
		AssetDatabase.Refresh();

		message = "Wrote " + OutputPath + ": " + cards.Count + " cards (from " + platform + ")."
			+ (autoNamed.Count > 0 ? " Captioned from file name (no line in WouldYouRatherCards.txt): " + string.Join(", ", autoNamed.ToArray()) + "." : "")
			+ (missingBundle.Count > 0 ? " Skipped, no bundle: " + string.Join(", ", missingBundle.ToArray()) + "." : "");
		return true;
	}

	private static string Prettify(string stem)
	{
		string s = stem.Replace('_', ' ').Trim();
		return s.Length == 0 ? stem : char.ToUpperInvariant(s[0]) + s.Substring(1);
	}

	private static string Esc(string s)
	{
		StringBuilder sb = new StringBuilder();
		foreach (char c in s)
		{
			switch (c)
			{
				case '\\': sb.Append("\\\\"); break;
				case '"': sb.Append("\\\""); break;
				case '\n': sb.Append("\\n"); break;
				case '\r': sb.Append("\\r"); break;
				case '\t': sb.Append("\\t"); break;
				default: sb.Append(c); break;
			}
		}
		return sb.ToString();
	}
}