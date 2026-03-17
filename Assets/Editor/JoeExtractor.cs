using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Joe;
using Core.MetaData;
using UnityEditor;
using UnityEngine;

public static class JoeExtractor
{
	[MenuItem("Tools/Joe/Extract contentfile.joe to CSV")]
	public static void ExtractJoeToCsv()
	{
		string joePath = EditorUtility.OpenFilePanel("Select .joe file", "", "joe");
		if (string.IsNullOrEmpty(joePath))
		{
			return;
		}

		string outputDir = EditorUtility.OpenFolderPanel("Select output folder", "", "");
		if (string.IsNullOrEmpty(outputDir))
		{
			return;
		}

		byte[] bytes = File.ReadAllBytes(joePath);
		JoeFile joeFile = new JoeFile(bytes);
		Sheet[] sheets = joeFile.GetAllSheets();

		if (sheets == null || sheets.Length == 0)
		{
			Debug.LogError("Failed to parse .joe file (invalid or unsupported format).");
			return;
		}

		for (int i = 0; i < sheets.Length; i++)
		{
			Sheet sheet = sheets[i];
			Column[] columns = sheet.InternalGetAllColumns();
			var rows = sheet.GetAllRows();

			if (columns == null || rows == null)
			{
				continue;
			}

			StringBuilder sb = new StringBuilder();

			// Header
			for (int c = 0; c < columns.Length; c++)
			{
				if (c > 0)
				{
					sb.Append(",");
				}
				sb.Append(EscapeCsv(columns[c].ColName));
			}
			sb.AppendLine();

			// Data
			foreach (var kv in rows)
			{
				Row row = kv.Value;

				for (int c = 0; c < columns.Length; c++)
				{
					if (c > 0)
					{
						sb.Append(",");
					}

					string value = GetCellValue(row, columns[c], c);
					sb.Append(EscapeCsv(value));
				}

				sb.AppendLine();
			}

			string safeSheetName = MakeSafeFileName(sheet.SheetName);
			string csvPath = Path.Combine(outputDir, safeSheetName + ".csv");
			File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
		}

		AssetDatabase.Refresh();
		Debug.Log("JOE extraction complete.");
	}

	[MenuItem("Tools/Joe/Recode .joe (decode -> encode)")]
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

	[MenuItem("Tools/Joe/Recode CSV Folder -> .joe (uses template types)")]
	public static void RecodeCsvFolderToJoe()
	{
		string templateJoePath = EditorUtility.OpenFilePanel("Select template .joe file", "", "joe");
		if (string.IsNullOrEmpty(templateJoePath))
		{
			return;
		}

		string csvFolder = EditorUtility.OpenFolderPanel("Select CSV folder", "", "");
		if (string.IsNullOrEmpty(csvFolder))
		{
			return;
		}

		string outputJoePath = EditorUtility.SaveFilePanel("Save output .joe", Path.GetDirectoryName(templateJoePath), "contentdata.recoded", "joe");
		if (string.IsNullOrEmpty(outputJoePath))
		{
			return;
		}

		JoeFile templateJoe = new JoeFile(File.ReadAllBytes(templateJoePath));
		Sheet[] templateSheets = templateJoe.GetAllSheets();
		if (templateSheets == null || templateSheets.Length == 0)
		{
			Debug.LogError("Template .joe is invalid.");
			return;
		}

		List<CsvSheetData> sheetData = new List<CsvSheetData>();
		for (int i = 0; i < templateSheets.Length; i++)
		{
			Sheet templateSheet = templateSheets[i];
			Column[] columns = templateSheet.InternalGetAllColumns();
			if (columns == null)
			{
				continue;
			}

			string csvPath = Path.Combine(csvFolder, MakeSafeFileName(templateSheet.SheetName) + ".csv");
			if (!File.Exists(csvPath))
			{
				Debug.LogWarning("Missing CSV for sheet: " + templateSheet.SheetName + " (" + csvPath + ")");
				continue;
			}

			List<string[]> records = ParseCsv(File.ReadAllText(csvPath));
			if (records.Count == 0)
			{
				continue;
			}

			Dictionary<string, int> headerMap = new Dictionary<string, int>();
			string[] header = records[0];
			for (int h = 0; h < header.Length; h++)
			{
				if (!headerMap.ContainsKey(header[h]))
				{
					headerMap.Add(header[h], h);
				}
			}

			CsvSheetData data = new CsvSheetData();
			data.SheetName = templateSheet.SheetName;
			data.Columns = columns;
			data.Rows = new List<string[]>();

			for (int r = 1; r < records.Count; r++)
			{
				string[] inputRow = records[r];
				string[] ordered = new string[columns.Length];
				for (int c = 0; c < columns.Length; c++)
				{
					int idx;
					if (headerMap.TryGetValue(columns[c].ColName, out idx) && idx >= 0 && idx < inputRow.Length)
					{
						ordered[c] = inputRow[idx];
					}
					else
					{
						ordered[c] = string.Empty;
					}
				}
				data.Rows.Add(ordered);
			}

			sheetData.Add(data);
		}

		byte[] joeBytes = EncodeCsvSheets(sheetData);
		File.WriteAllBytes(outputJoePath, joeBytes);
		AssetDatabase.Refresh();
		Debug.Log("JOE recode complete: " + outputJoePath);
	}

	private sealed class CsvSheetData
	{
		public string SheetName;
		public Column[] Columns;
		public List<string[]> Rows;
	}

	private static byte[] EncodeCsvSheets(List<CsvSheetData> sheets)
	{
		List<string> strings = new List<string>();
		Dictionary<string, int> stringIndex = new Dictionary<string, int>();

		List<int> ints = new List<int>();
		Dictionary<int, int> intIndex = new Dictionary<int, int>();

		List<float> floats = new List<float>();
		Dictionary<int, int> floatBitsIndex = new Dictionary<int, int>();

		List<string[]> stringArrays = new List<string[]>();
		Dictionary<string, int> stringArrayIndex = new Dictionary<string, int>();

		List<int[]> nnIntArrays = new List<int[]>();
		Dictionary<string, int> nnIntArrayIndex = new Dictionary<string, int>();

		List<int[]> rawIntArrays = new List<int[]>();
		Dictionary<string, int> rawIntArrayIndex = new Dictionary<string, int>();

		List<float[]> floatArrays = new List<float[]>();
		Dictionary<string, int> floatArrayIndex = new Dictionary<string, int>();

		for (int s = 0; s < sheets.Count; s++)
		{
			CsvSheetData sheet = sheets[s];
			AddString(sheet.SheetName, strings, stringIndex);
			for (int c = 0; c < sheet.Columns.Length; c++)
			{
				AddString(sheet.Columns[c].ColName, strings, stringIndex);
			}

			for (int r = 0; r < sheet.Rows.Count; r++)
			{
				string[] row = sheet.Rows[r];
				for (int c = 0; c < sheet.Columns.Length; c++)
				{
					string v = row[c];
					if (string.IsNullOrEmpty(v))
					{
						continue;
					}

					switch (sheet.Columns[c].ColType)
					{
						case ColumnType.String:
							AddString(v, strings, stringIndex);
							break;
						case ColumnType.RawInt:
							AddInt(ParseInt(v), ints, intIndex);
							break;
						case ColumnType.Float:
							AddFloat(ParseFloat(v), floats, floatBitsIndex);
							break;
						case ColumnType.StringArray:
						{
							string[] arr = SplitArray(v);
							for (int i = 0; i < arr.Length; i++)
							{
								AddString(arr[i], strings, stringIndex);
							}
							AddStringArray(arr, stringArrays, stringArrayIndex);
							break;
						}
						case ColumnType.NonNegativeIntArray:
							AddNNIntArray(ParseIntArray(v), nnIntArrays, nnIntArrayIndex);
							break;
						case ColumnType.RawIntArray:
						{
							int[] arr = ParseIntArray(v);
							for (int i = 0; i < arr.Length; i++)
							{
								AddInt(arr[i], ints, intIndex);
							}
							AddRawIntArray(arr, rawIntArrays, rawIntArrayIndex);
							break;
						}
						case ColumnType.FloatArray:
						{
							float[] arr = ParseFloatArray(v);
							for (int i = 0; i < arr.Length; i++)
							{
								AddFloat(arr[i], floats, floatBitsIndex);
							}
							AddFloatArray(arr, floatArrays, floatArrayIndex);
							break;
						}
					}
				}
			}
		}

		using (MemoryStream ms = new MemoryStream())
		using (BinaryWriter w = new BinaryWriter(ms))
		{
			w.Write((byte)74); w.Write((byte)79); w.Write((byte)69); w.Write((byte)1);

			w.Write(strings.Count);
			for (int i = 0; i < strings.Count; i++)
			{
				byte[] utf8 = Encoding.UTF8.GetBytes(strings[i] ?? string.Empty);
				WriteVarLen(w, (uint)utf8.Length);
				w.Write(utf8);
			}

			w.Write(ints.Count);
			for (int i = 0; i < ints.Count; i++) w.Write(ints[i]);

			w.Write(floats.Count);
			for (int i = 0; i < floats.Count; i++) w.Write(floats[i]);

			w.Write(stringArrays.Count);
			for (int i = 0; i < stringArrays.Count; i++)
			{
				string[] arr = stringArrays[i];
				WriteVarLen(w, (uint)arr.Length);
				for (int j = 0; j < arr.Length; j++) WriteVarLen(w, (uint)stringIndex[arr[j]]);
			}

			w.Write(nnIntArrays.Count);
			for (int i = 0; i < nnIntArrays.Count; i++)
			{
				int[] arr = nnIntArrays[i];
				WriteVarLen(w, (uint)arr.Length);
				for (int j = 0; j < arr.Length; j++) WriteVarLen(w, (uint)(arr[j] < 0 ? 0 : arr[j]));
			}

			w.Write(rawIntArrays.Count);
			for (int i = 0; i < rawIntArrays.Count; i++)
			{
				int[] arr = rawIntArrays[i];
				WriteVarLen(w, (uint)arr.Length);
				for (int j = 0; j < arr.Length; j++) WriteVarLen(w, (uint)intIndex[arr[j]]);
			}

			w.Write(floatArrays.Count);
			for (int i = 0; i < floatArrays.Count; i++)
			{
				float[] arr = floatArrays[i];
				WriteVarLen(w, (uint)arr.Length);
				for (int j = 0; j < arr.Length; j++) WriteVarLen(w, (uint)floatBitsIndex[FloatBits(arr[j])]);
			}

			WriteVarLen(w, (uint)sheets.Count);
			for (int s = 0; s < sheets.Count; s++) WriteVarLen(w, (uint)stringIndex[sheets[s].SheetName]);

			for (int s = 0; s < sheets.Count; s++)
			{
				CsvSheetData sheet = sheets[s];
				WriteVarLen(w, (uint)sheet.Columns.Length);

				for (int c = 0; c < sheet.Columns.Length; c++)
				{
					w.Write((byte)sheet.Columns[c].ColType);
					WriteVarLen(w, (uint)stringIndex[sheet.Columns[c].ColName]);
				}

				WriteVarLen(w, (uint)(sheet.Rows.Count * sheet.Columns.Length));
				for (int r = 0; r < sheet.Rows.Count; r++)
				{
					for (int c = 0; c < sheet.Columns.Length; c++)
					{
						WriteCell(w, sheet.Columns[c].ColType, sheet.Rows[r][c], stringIndex, intIndex, floatBitsIndex, stringArrayIndex, nnIntArrayIndex, rawIntArrayIndex, floatArrayIndex);
					}
				}
			}

			w.Write((byte)0);
			return ms.ToArray();
		}
	}

	private static string GetCellValue(Row row, Column column, int colIndex)
	{
		switch (column.ColType)
		{
			case ColumnType.StringArray:
			{
				string[] arr = row.TryGetStringArray(colIndex);
				return arr == null ? string.Empty : string.Join("|", arr);
			}
			case ColumnType.NonNegativeIntArray:
			case ColumnType.RawIntArray:
			{
				int[] arr = row.TryGetIntArray(colIndex);
				if (arr == null)
				{
					return string.Empty;
				}

				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < arr.Length; i++)
				{
					if (i > 0)
					{
						sb.Append("|");
					}
					sb.Append(arr[i]);
				}
				return sb.ToString();
			}
			case ColumnType.FloatArray:
			{
				float[] arr = row.TryGetFloatArray(colIndex);
				if (arr == null)
				{
					return string.Empty;
				}

				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < arr.Length; i++)
				{
					if (i > 0)
					{
						sb.Append("|");
					}
					sb.Append(arr[i]);
				}
				return sb.ToString();
			}
			default:
				return row.TryGetString(colIndex, string.Empty);
		}
	}

	private static string EscapeCsv(string value)
	{
		if (value == null)
		{
			return string.Empty;
		}

		bool needsQuotes = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
		if (!needsQuotes)
		{
			return value;
		}

		return "\"" + value.Replace("\"", "\"\"") + "\"";
	}

	private static string MakeSafeFileName(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return "Sheet";
		}

		char[] invalid = Path.GetInvalidFileNameChars();
		StringBuilder sb = new StringBuilder(fileName.Length);

		for (int i = 0; i < fileName.Length; i++)
		{
			char ch = fileName[i];
			bool isInvalid = false;

			for (int j = 0; j < invalid.Length; j++)
			{
				if (ch == invalid[j])
				{
					isInvalid = true;
					break;
				}
			}

			sb.Append(isInvalid ? '_' : ch);
		}

		return sb.ToString();
	}

	private static void WriteCell(BinaryWriter w, ColumnType type, string value, Dictionary<string, int> strIdx, Dictionary<int, int> intIdx, Dictionary<int, int> floatIdx, Dictionary<string, int> strArrIdx, Dictionary<string, int> nnArrIdx, Dictionary<string, int> rawArrIdx, Dictionary<string, int> floatArrIdx)
	{
		if (string.IsNullOrEmpty(value))
		{
			w.Write((byte)0);
			return;
		}

		switch (type)
		{
			case ColumnType.String:
				WriteVarLen(w, (uint)(strIdx[value] + 1));
				return;
			case ColumnType.Boolean:
			{
				bool b = value == "1" || value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
				w.Write((byte)(b ? 2 : 1));
				return;
			}
			case ColumnType.NonNegativeInt:
				WriteVarLen(w, (uint)(System.Math.Max(0, ParseInt(value)) + 1));
				return;
			case ColumnType.RawInt:
				WriteVarLen(w, (uint)(intIdx[ParseInt(value)] + 1));
				return;
			case ColumnType.Float:
				WriteVarLen(w, (uint)(floatIdx[FloatBits(ParseFloat(value))] + 1));
				return;
			case ColumnType.StringArray:
				WriteVarLen(w, (uint)(strArrIdx[JoinKey(SplitArray(value))] + 1));
				return;
			case ColumnType.NonNegativeIntArray:
				WriteVarLen(w, (uint)(nnArrIdx[JoinKey(ParseIntArray(value))] + 1));
				return;
			case ColumnType.RawIntArray:
				WriteVarLen(w, (uint)(rawArrIdx[JoinKey(ParseIntArray(value))] + 1));
				return;
			case ColumnType.FloatArray:
				WriteVarLen(w, (uint)(floatArrIdx[JoinKey(ParseFloatArray(value))] + 1));
				return;
			default:
				w.Write((byte)0);
				return;
		}
	}

	private static List<string[]> ParseCsv(string text)
	{
		List<string[]> rows = new List<string[]>();
		List<string> row = new List<string>();
		StringBuilder cell = new StringBuilder();
		bool inQuotes = false;

		for (int i = 0; i < text.Length; i++)
		{
			char ch = text[i];
			if (inQuotes)
			{
				if (ch == '"')
				{
					if (i + 1 < text.Length && text[i + 1] == '"')
					{
						cell.Append('"');
						i++;
					}
					else
					{
						inQuotes = false;
					}
				}
				else
				{
					cell.Append(ch);
				}
			}
			else
			{
				if (ch == '"') inQuotes = true;
				else if (ch == ',') { row.Add(cell.ToString()); cell.Length = 0; }
				else if (ch == '\n')
				{
					row.Add(cell.ToString()); cell.Length = 0;
					if (row.Count > 0) rows.Add(row.ToArray());
					row.Clear();
				}
				else if (ch != '\r') cell.Append(ch);
			}
		}

		if (cell.Length > 0 || row.Count > 0)
		{
			row.Add(cell.ToString());
			rows.Add(row.ToArray());
		}
		return rows;
	}

	private static void AddString(string v, List<string> list, Dictionary<string, int> map) { if (!map.ContainsKey(v)) { map[v] = list.Count; list.Add(v); } }
	private static void AddInt(int v, List<int> list, Dictionary<int, int> map) { if (!map.ContainsKey(v)) { map[v] = list.Count; list.Add(v); } }
	private static void AddFloat(float v, List<float> list, Dictionary<int, int> map) { int k = FloatBits(v); if (!map.ContainsKey(k)) { map[k] = list.Count; list.Add(v); } }
	private static void AddStringArray(string[] arr, List<string[]> list, Dictionary<string, int> map) { string k = JoinKey(arr); if (!map.ContainsKey(k)) { map[k] = list.Count; list.Add(arr); } }
	private static void AddNNIntArray(int[] arr, List<int[]> list, Dictionary<string, int> map) { for (int i = 0; i < arr.Length; i++) if (arr[i] < 0) arr[i] = 0; string k = JoinKey(arr); if (!map.ContainsKey(k)) { map[k] = list.Count; list.Add(arr); } }
	private static void AddRawIntArray(int[] arr, List<int[]> list, Dictionary<string, int> map) { string k = JoinKey(arr); if (!map.ContainsKey(k)) { map[k] = list.Count; list.Add(arr); } }
	private static void AddFloatArray(float[] arr, List<float[]> list, Dictionary<string, int> map) { string k = JoinKey(arr); if (!map.ContainsKey(k)) { map[k] = list.Count; list.Add(arr); } }

	private static string[] SplitArray(string s) { return string.IsNullOrEmpty(s) ? new string[0] : s.Split('|'); }
	private static int ParseInt(string s) { int v; return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0; }
	private static float ParseFloat(string s) { float v; return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f; }
	private static int[] ParseIntArray(string s) { string[] p = SplitArray(s); int[] a = new int[p.Length]; for (int i = 0; i < p.Length; i++) a[i] = ParseInt(p[i]); return a; }
	private static float[] ParseFloatArray(string s) { string[] p = SplitArray(s); float[] a = new float[p.Length]; for (int i = 0; i < p.Length; i++) a[i] = ParseFloat(p[i]); return a; }

	private static string JoinKey(string[] arr) { return string.Join("|", arr); }
	private static string JoinKey(int[] arr) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < arr.Length; i++) { if (i > 0) sb.Append("|"); sb.Append(arr[i]); } return sb.ToString(); }
	private static string JoinKey(float[] arr) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < arr.Length; i++) { if (i > 0) sb.Append("|"); sb.Append(FloatBits(arr[i])); } return sb.ToString(); }

	private static int FloatBits(float v) { return System.BitConverter.ToInt32(System.BitConverter.GetBytes(v), 0); }

	private static void WriteVarLen(BinaryWriter w, uint val)
	{
		if (val == 0) { w.Write((byte)0); return; }
		byte[] tmp = new byte[5];
		int count = 0;
		while (val > 0) { tmp[count++] = (byte)(val & 0x7F); val >>= 7; }
		for (int i = count - 1; i >= 0; i--)
		{
			byte b = tmp[i];
			if (i != 0) b = (byte)(b | 0x80);
			w.Write(b);
		}
	}
}