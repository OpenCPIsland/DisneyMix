using System.IO;
using Core.Joe;
using Core.MetaData;
using UnityEditor;
using UnityEngine;

/// <summary>
/// .joe <-> CSV tooling, split across three files:
///   JoeExtractor.cs         - shared constants and the decode -> encode recode check (this file)
///   JoeExtractor.Extract.cs - .joe -> CSV folder (typed headers + _sheets.txt manifest)
///   JoeExtractor.Build.cs   - CSV folder -> .joe (no template needed)
///
/// Extract writes one CSV per sheet plus a "_sheets.txt" manifest:
///   - Header cells are "ColumnName:ColumnType" (e.g. "Cost:NonNegativeInt"), so the CSVs carry their own schema.
///   - _sheets.txt lists "file.csv[TAB]Sheet Name" in original sheet order (GetSheet(i) is index based).
/// Build reads that folder back. Plain CSVs (no ":Type" suffix / no manifest) still work: types are inferred
/// and sheets are ordered by file name.
/// </summary>
public static partial class JoeExtractor
{
	private const string ManifestFileName = "_sheets.txt";

	[MenuItem("Tools/Mix/Joe/Recode .joe (decode -> encode)")]
	public static void RecodeJoe()
	{
		string inputPath = EditorUtility.OpenFilePanel("Select source .joe file", "", "joe");
		if (string.IsNullOrEmpty(inputPath))
		{
			return;
		}

		byte[] sourceBytes = File.ReadAllBytes(inputPath);
		JoeFile joeFile = new JoeFile(sourceBytes);
		Sheet[] sheets = joeFile.GetAllSheets();
		if (sheets == null || sheets.Length == 0)
		{
			Debug.LogError("Failed to parse source .joe file.");
			return;
		}

		byte[] recodedBytes = JoeFile.Encode(sheets);
		if (recodedBytes == null || recodedBytes.Length == 0)
		{
			Debug.LogError("Failed to re-encode .joe file.");
			return;
		}

		string defaultName = Path.GetFileNameWithoutExtension(inputPath) + ".recoded.joe";
		string savePath = EditorUtility.SaveFilePanel("Save recoded .joe", Path.GetDirectoryName(inputPath), defaultName, "joe");
		if (string.IsNullOrEmpty(savePath))
		{
			return;
		}

		File.WriteAllBytes(savePath, recodedBytes);
		AssetDatabase.Refresh();
		Debug.Log("JOE recode complete: " + savePath);
	}
}