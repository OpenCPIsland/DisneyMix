using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds data/Writers_Block_Data/Writers_Block_Data.gz, the list of suggested
/// words the Secret Word (Writers Block) game offers when the player presses
/// the random/shake button (SecretWordCreateWordPanel: LoadData(
/// "data/Writers_Block_Data/Writers_Block_Data.gz", "Writers_Block_Data.txt",
/// ParseJsonData), then OnDataLoaded reads content.objects.wbd).
///
/// Unlike the Meme / Would You Rather builders there are no asset bundles:
/// each entry is just a word plus a hint. Entries come from
/// Assets/Editor/WritersBlockWords.txt, one per line:
///     WORD<TAB>hint text
/// Lines starting with # are ignored. If the file doesn't exist, a template
/// seeded with the four built-in debug entries is created and used.
///
/// Entries are skipped (and reported) when they would break the game:
///   - word isn't 3..MaxWordLength letters A-Z (the create panel needs >= 3
///     tiles, SecretWordGame.maxWordLength is 8, the tile grid is letters only)
///   - hint is empty (SecretWordCreateHintPanel rejects empty hints)
///   - hint contains the word (SecretWordCreateHintPanel.ValidateHint rejects it)
///   - duplicate word
///
/// File format (ContentObj, key "wbd", elements are Secret_Word_Data):
///   gzip( {"content":{"objects":{"wbd":[{"uid","word","hint","order",...}]}}} )
///
/// ASSUMPTION TO VERIFY: word/hint/uid are confirmed by the game code
/// (GetDebugWritersBlockData). order/hidden/preview/isNew/tags/pushNote are
/// copied from the shared MixGameContentData fields used by the other
/// catalogs. If suggestions fail to load, compare with Secret_Word_Data.
/// </summary>
public static class SecretWordDataBuilder
{
	// Keep in sync with SecretWordGame.maxWordLength.
	private const int MaxWordLength = 8;
	private const int MinWordLength = 3;

	private static string OutputPath { get { return Path.Combine(Application.streamingAssetsPath, "data/Writers_Block_Data/Writers_Block_Data.gz"); } }
	private static string WordsPath { get { return Path.Combine(Application.dataPath, "Editor/WritersBlockWords.txt"); } }

	private const string Template =
		"# Writers Block (Secret Word) suggestions. One per line: WORD<TAB>hint\n" +
		"# The hint must not contain the word. Words: 3-8 letters, A-Z only.\n" +
		"ALADDIN\tThree wishes and a monkey\n" +
		"ARIEL\tDreams about going up there\n" +
		"AURORA\tSleepy princess\n" +
		"BAMBI\tPrince of the forest\n";

	[MenuItem("Tools/Mix/Games/Build Writers Block Data")]
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
		EditorUtility.DisplayDialog("Writers Block Data", message, "OK");
	}

	/// <summary>For -batchmode: -executeMethod SecretWordDataBuilder.BuildBatch (exit 1 on failure).</summary>
	public static void BuildBatch()
	{
		string message;
		bool ok = TryBuild(out message);
		Console.WriteLine(message);
		EditorApplication.Exit(ok ? 0 : 1);
	}

	public static bool TryBuild(out string message)
	{
		bool createdTemplate = false;
		if (!File.Exists(WordsPath))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(WordsPath));
			File.WriteAllText(WordsPath, Template, new UTF8Encoding(false));
			createdTemplate = true;
		}

		List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
		HashSet<string> seen = new HashSet<string>();
		List<string> badWord = new List<string>();
		List<string> badHint = new List<string>();
		List<string> duplicates = new List<string>();

		foreach (string raw in File.ReadAllLines(WordsPath, Encoding.UTF8))
		{
			string line = raw.Trim('\r', '\n');
			if (line.Length == 0 || line.StartsWith("#"))
			{
				continue;
			}
			int tab = line.IndexOf('\t');
			if (tab <= 0)
			{
				badHint.Add(line);
				continue;
			}
			string word = line.Substring(0, tab).Trim().ToUpperInvariant();
			string hint = line.Substring(tab + 1).Trim();

			if (!IsValidWord(word))
			{
				badWord.Add(word);
				continue;
			}
			if (hint.Length == 0 || hint.ToUpperInvariant().Contains(word))
			{
				badHint.Add(word);
				continue;
			}
			if (!seen.Add(word))
			{
				duplicates.Add(word);
				continue;
			}
			entries.Add(new KeyValuePair<string, string>(word, hint));
		}

		if (entries.Count == 0)
		{
			message = "No valid WORD<TAB>hint lines in " + WordsPath;
			return false;
		}

		StringBuilder json = new StringBuilder();
		json.Append("{\"content\":{\"objects\":{\"wbd\":[");
		for (int i = 0; i < entries.Count; i++)
		{
			if (i > 0)
			{
				json.Append(',');
			}
			json.Append("{\"uid\":\"writers_block_").Append(i + 1)
				.Append("\",\"word\":\"").Append(Esc(entries[i].Key))
				.Append("\",\"hint\":\"").Append(Esc(entries[i].Value))
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

		message = "Wrote " + OutputPath + ": " + entries.Count + " words."
			+ (createdTemplate ? " Created template " + WordsPath + "; add your own lines and run again." : "")
			+ (badWord.Count > 0 ? " Skipped, bad word (need " + MinWordLength + "-" + MaxWordLength + " letters A-Z): " + string.Join(", ", badWord.ToArray()) + "." : "")
			+ (badHint.Count > 0 ? " Skipped, missing hint or hint contains the word: " + string.Join(", ", badHint.ToArray()) + "." : "")
			+ (duplicates.Count > 0 ? " Skipped duplicates: " + string.Join(", ", duplicates.ToArray()) + "." : "");
		return true;
	}

	private static bool IsValidWord(string word)
	{
		if (word.Length < MinWordLength || word.Length > MaxWordLength)
		{
			return false;
		}
		foreach (char c in word)
		{
			if (c < 'A' || c > 'Z')
			{
				return false;
			}
		}
		return true;
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