using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Joe;
using Core.MetaData;
using UnityEditor;
using UnityEngine;

/// <summary>CSV folder -> .joe, no template required. See JoeExtractor.cs for the file layout.</summary>
public static partial class JoeExtractor
{
	[MenuItem("Tools/Mix/Joe/Build .joe from CSV folder")]
	public static void BuildJoeFromCsvFolder()
	{
		string csvFolder = EditorUtility.OpenFolderPanel("Select CSV folder", "", "");
		if (string.IsNullOrEmpty(csvFolder))
		{
			return;
		}

		string outputPath = EditorUtility.SaveFilePanel("Save output .joe", csvFolder, "contentdata", "joe");
		if (string.IsNullOrEmpty(outputPath))
		{
			return;
		}

		List<CsvSheetData> sheets = LoadSheets(csvFolder);
		if (sheets == null || sheets.Count == 0)
		{
			Debug.LogError("Nothing to build (see messages above).");
			return;
		}

		byte[] joeBytes = EncodeCsvSheets(sheets);

		// Sanity check with the real parser before touching the output file.
		Sheet[] parsed = new JoeFile(joeBytes).GetAllSheets();
		if (parsed == null || parsed.Length != sheets.Count)
		{
			Debug.LogError("Built .joe failed to parse back; output NOT written.");
			return;
		}

		File.WriteAllBytes(outputPath, joeBytes);
		AssetDatabase.Refresh();
		Debug.Log("JOE build complete: " + outputPath + " (" + sheets.Count + " sheets)");
	}

	// ------------------------------------------------------------------
	// CSV folder -> sheet data
	// ------------------------------------------------------------------

	private sealed class CsvSheetData
	{
		public string SheetName;
		public Column[] Columns;
		public List<string[]> Rows;
	}

	private static List<CsvSheetData> LoadSheets(string folder)
	{
		// entry = { full path, sheet name }
		List<string[]> entries = new List<string[]>();
		HashSet<string> listed = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

		string manifestPath = Path.Combine(folder, ManifestFileName);
		bool hasManifest = File.Exists(manifestPath);

		if (hasManifest)
		{
			string[] lines = File.ReadAllLines(manifestPath);
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (line.Length == 0)
				{
					continue;
				}

				int tab = line.IndexOf('\t');
				string file = tab < 0 ? line : line.Substring(0, tab);
				string name = tab < 0 ? Path.GetFileNameWithoutExtension(file) : line.Substring(tab + 1);
				string path = Path.Combine(folder, file);

				if (!File.Exists(path))
				{
					// Dropping a sheet would shift every later sheet index, so this is fatal.
					Debug.LogError(ManifestFileName + " lists '" + file + "' but it is missing. Aborting (sheet indices would shift).");
					return null;
				}

				entries.Add(new string[] { path, name });
				listed.Add(file);
			}
		}
		else
		{
			Debug.LogWarning("No " + ManifestFileName + " found: sheets will be ordered by file name and named after their files.");
		}

		string[] allCsv = Directory.GetFiles(folder, "*.csv");
		System.Array.Sort(allCsv, System.StringComparer.Ordinal);
		for (int i = 0; i < allCsv.Length; i++)
		{
			string fileName = Path.GetFileName(allCsv[i]);
			if (listed.Contains(fileName))
			{
				continue;
			}

			if (hasManifest)
			{
				Debug.LogWarning("'" + fileName + "' is not in " + ManifestFileName + "; appending it as the last sheet(s).");
			}
			entries.Add(new string[] { allCsv[i], Path.GetFileNameWithoutExtension(fileName) });
		}

		List<CsvSheetData> result = new List<CsvSheetData>();
		for (int i = 0; i < entries.Count; i++)
		{
			CsvSheetData sheet = LoadSheet(entries[i][0], entries[i][1]);
			if (sheet == null)
			{
				return null;
			}
			result.Add(sheet);
		}
		return result;
	}

	private static CsvSheetData LoadSheet(string path, string sheetName)
	{
		string fileName = Path.GetFileName(path);
		List<string[]> records = ParseCsv(File.ReadAllText(path));

		// Spreadsheet apps like to leave empty trailing rows behind.
		while (records.Count > 0 && IsBlankRecord(records[records.Count - 1]))
		{
			records.RemoveAt(records.Count - 1);
		}

		if (records.Count == 0)
		{
			Debug.LogError(fileName + ": empty file (a header row is required).");
			return null;
		}

		string[] header = records[0];
		int colCount = header.Length;
		while (colCount > 0 && header[colCount - 1].Length == 0)
		{
			colCount--;
		}

		if (colCount == 0)
		{
			Debug.LogError(fileName + ": header row has no columns.");
			return null;
		}

		string[] names = new string[colCount];
		ColumnType[] types = new ColumnType[colCount];
		bool[] hasType = new bool[colCount];
		for (int c = 0; c < colCount; c++)
		{
			hasType[c] = ParseHeaderCell(header[c], out names[c], out types[c]);
		}

		List<string[]> rows = new List<string[]>();
		for (int r = 1; r < records.Count; r++)
		{
			string[] src = records[r];
			string[] row = new string[colCount];
			for (int c = 0; c < colCount; c++)
			{
				row[c] = c < src.Length ? src[c] : string.Empty;
			}
			rows.Add(row);
		}

		List<string> inferred = new List<string>();
		Column[] columns = new Column[colCount];
		for (int c = 0; c < colCount; c++)
		{
			if (!hasType[c])
			{
				types[c] = InferType(rows, c);
				inferred.Add(names[c] + ":" + types[c]);
			}
			columns[c] = new Column(names[c], types[c]);
		}

		if (inferred.Count > 0)
		{
			Debug.LogWarning(fileName + ": no type in header for " + string.Join(", ", inferred.ToArray())
				+ ". Types were guessed; add the ':Type' suffix to the header to make them exact.");
		}

		CsvSheetData data = new CsvSheetData();
		data.SheetName = sheetName;
		data.Columns = columns;
		data.Rows = rows;
		return data;
	}

	/// <summary>Splits "Name:Type". Returns false (name = whole cell) if there is no valid type suffix.</summary>
	private static bool ParseHeaderCell(string cell, out string name, out ColumnType type)
	{
		int colon = cell.LastIndexOf(':');
		if (colon > 0 && colon < cell.Length - 1)
		{
			ColumnType parsed;
			string typeName = cell.Substring(colon + 1).Trim();
			if (System.Enum.TryParse<ColumnType>(typeName, true, out parsed) && System.Enum.IsDefined(typeof(ColumnType), parsed))
			{
				name = cell.Substring(0, colon);
				type = parsed;
				return true;
			}
		}

		name = cell;
		type = ColumnType.String;
		return false;
	}

	/// <summary>Best-effort fallback for headers without a type. Lossy by nature (a String column of digits looks like an int).</summary>
	private static ColumnType InferType(List<string[]> rows, int col)
	{
		bool any = false;
		bool anyPipe = false;
		bool allBool = true;
		bool allInt = true;
		bool allNonNeg = true;
		bool allFloat = true;

		for (int r = 0; r < rows.Count; r++)
		{
			string v = rows[r][col];
			if (v.Length == 0)
			{
				continue;
			}

			any = true;
			if (v.IndexOf('|') >= 0)
			{
				anyPipe = true;
			}

			if (!(v.Equals("true", System.StringComparison.OrdinalIgnoreCase) || v.Equals("false", System.StringComparison.OrdinalIgnoreCase)))
			{
				allBool = false;
			}

			string[] parts = v.Split('|');
			for (int p = 0; p < parts.Length; p++)
			{
				int iv;
				float fv;
				if (!int.TryParse(parts[p], NumberStyles.Integer, CultureInfo.InvariantCulture, out iv))
				{
					allInt = false;
					allNonNeg = false;
				}
				else if (iv < 0)
				{
					allNonNeg = false;
				}

				if (!float.TryParse(parts[p], NumberStyles.Float, CultureInfo.InvariantCulture, out fv))
				{
					allFloat = false;
				}
			}
		}

		if (!any)
		{
			return ColumnType.String;
		}
		if (!anyPipe && allBool)
		{
			return ColumnType.Boolean;
		}
		if (allInt)
		{
			if (anyPipe)
			{
				return allNonNeg ? ColumnType.NonNegativeIntArray : ColumnType.RawIntArray;
			}
			return allNonNeg ? ColumnType.NonNegativeInt : ColumnType.RawInt;
		}
		if (allFloat)
		{
			return anyPipe ? ColumnType.FloatArray : ColumnType.Float;
		}
		return anyPipe ? ColumnType.StringArray : ColumnType.String;
	}

	private static bool IsBlankRecord(string[] record)
	{
		for (int i = 0; i < record.Length; i++)
		{
			if (record[i].Length != 0)
			{
				return false;
			}
		}
		return true;
	}

	// ------------------------------------------------------------------
	// Encoding
	// ------------------------------------------------------------------

	/// <summary>Deduplicated lookup tables. Array tables store indexes into the scalar tables, exactly as the file format does.</summary>
	private sealed class JoeTables
	{
		public readonly List<string> Strings = new List<string>();
		private readonly Dictionary<string, int> stringIdx = new Dictionary<string, int>();

		public readonly List<int> Ints = new List<int>();
		private readonly Dictionary<int, int> intIdx = new Dictionary<int, int>();

		public readonly List<float> Floats = new List<float>();
		private readonly Dictionary<int, int> floatIdx = new Dictionary<int, int>();

		public readonly List<int[]> StringArrays = new List<int[]>();   // indexes into Strings
		private readonly Dictionary<string, int> stringArrayIdx = new Dictionary<string, int>();

		public readonly List<int[]> NonNegativeIntArrays = new List<int[]>();   // raw values
		private readonly Dictionary<string, int> nnIntArrayIdx = new Dictionary<string, int>();

		public readonly List<int[]> RawIntArrays = new List<int[]>();   // indexes into Ints
		private readonly Dictionary<string, int> rawIntArrayIdx = new Dictionary<string, int>();

		public readonly List<int[]> FloatArrays = new List<int[]>();   // indexes into Floats
		private readonly Dictionary<string, int> floatArrayIdx = new Dictionary<string, int>();

		public int AddString(string v)
		{
			v = v ?? string.Empty;
			int i;
			if (!stringIdx.TryGetValue(v, out i))
			{
				i = Strings.Count;
				Strings.Add(v);
				stringIdx[v] = i;
			}
			return i;
		}

		public int AddInt(int v)
		{
			int i;
			if (!intIdx.TryGetValue(v, out i))
			{
				i = Ints.Count;
				Ints.Add(v);
				intIdx[v] = i;
			}
			return i;
		}

		public int AddFloat(float v)
		{
			int bits = FloatBits(v);
			int i;
			if (!floatIdx.TryGetValue(bits, out i))
			{
				i = Floats.Count;
				Floats.Add(v);
				floatIdx[bits] = i;
			}
			return i;
		}

		public int AddStringArray(string[] a)
		{
			StringBuilder key = new StringBuilder();
			for (int j = 0; j < a.Length; j++)
			{
				key.Append(a[j].Length).Append(':').Append(a[j]).Append('|');
			}

			int i;
			string k = key.ToString();
			if (!stringArrayIdx.TryGetValue(k, out i))
			{
				int[] idx = new int[a.Length];
				for (int j = 0; j < a.Length; j++)
				{
					idx[j] = AddString(a[j]);
				}
				i = StringArrays.Count;
				StringArrays.Add(idx);
				stringArrayIdx[k] = i;
			}
			return i;
		}

		public int AddNonNegativeIntArray(int[] a)
		{
			string k = string.Join(",", System.Array.ConvertAll(a, x => x.ToString(CultureInfo.InvariantCulture)));
			int i;
			if (!nnIntArrayIdx.TryGetValue(k, out i))
			{
				i = NonNegativeIntArrays.Count;
				NonNegativeIntArrays.Add(a);
				nnIntArrayIdx[k] = i;
			}
			return i;
		}

		public int AddRawIntArray(int[] a)
		{
			string k = string.Join(",", System.Array.ConvertAll(a, x => x.ToString(CultureInfo.InvariantCulture)));
			int i;
			if (!rawIntArrayIdx.TryGetValue(k, out i))
			{
				int[] idx = new int[a.Length];
				for (int j = 0; j < a.Length; j++)
				{
					idx[j] = AddInt(a[j]);
				}
				i = RawIntArrays.Count;
				RawIntArrays.Add(idx);
				rawIntArrayIdx[k] = i;
			}
			return i;
		}

		public int AddFloatArray(float[] a)
		{
			string k = string.Join(",", System.Array.ConvertAll(a, x => FloatBits(x).ToString(CultureInfo.InvariantCulture)));
			int i;
			if (!floatArrayIdx.TryGetValue(k, out i))
			{
				int[] idx = new int[a.Length];
				for (int j = 0; j < a.Length; j++)
				{
					idx[j] = AddFloat(a[j]);
				}
				i = FloatArrays.Count;
				FloatArrays.Add(idx);
				floatArrayIdx[k] = i;
			}
			return i;
		}
	}

	private static byte[] EncodeCsvSheets(List<CsvSheetData> sheets)
	{
		JoeTables t = new JoeTables();
		uint[][] cells = new uint[sheets.Count][];

		// Pass 1: fill the tables and turn every cell into its encoded value (0 = empty/null).
		for (int s = 0; s < sheets.Count; s++)
		{
			CsvSheetData sheet = sheets[s];
			int cols = sheet.Columns.Length;

			t.AddString(sheet.SheetName);
			for (int c = 0; c < cols; c++)
			{
				t.AddString(sheet.Columns[c].ColName);
			}

			uint[] encoded = new uint[sheet.Rows.Count * cols];
			int bad = 0;
			for (int r = 0; r < sheet.Rows.Count; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					encoded[r * cols + c] = EncodeCell(t, sheet.Columns[c].ColType, sheet.Rows[r][c], ref bad);
				}
			}

			if (bad > 0)
			{
				Debug.LogWarning("Sheet '" + sheet.SheetName + "': " + bad + " value(s) could not be parsed and were written as 0.");
			}
			cells[s] = encoded;
		}

		// Pass 2: write.
		using (MemoryStream ms = new MemoryStream())
		using (BinaryWriter w = new BinaryWriter(ms))
		{
			w.Write((byte)74);
			w.Write((byte)79);
			w.Write((byte)69);
			w.Write((byte)1);

			w.Write(t.Strings.Count);
			for (int i = 0; i < t.Strings.Count; i++)
			{
				byte[] utf8 = Encoding.UTF8.GetBytes(t.Strings[i]);
				WriteVarLen(w, (uint)utf8.Length);
				w.Write(utf8);
			}

			w.Write(t.Ints.Count);
			for (int i = 0; i < t.Ints.Count; i++)
			{
				w.Write(t.Ints[i]);
			}

			w.Write(t.Floats.Count);
			for (int i = 0; i < t.Floats.Count; i++)
			{
				w.Write(t.Floats[i]);
			}

			WriteArrayTable(w, t.StringArrays);
			WriteArrayTable(w, t.NonNegativeIntArrays);
			WriteArrayTable(w, t.RawIntArrays);
			WriteArrayTable(w, t.FloatArrays);

			WriteVarLen(w, (uint)sheets.Count);
			for (int s = 0; s < sheets.Count; s++)
			{
				WriteVarLen(w, (uint)t.AddString(sheets[s].SheetName));
			}

			for (int s = 0; s < sheets.Count; s++)
			{
				Column[] columns = sheets[s].Columns;

				WriteVarLen(w, (uint)columns.Length);
				for (int c = 0; c < columns.Length; c++)
				{
					w.Write((byte)columns[c].ColType);
					WriteVarLen(w, (uint)t.AddString(columns[c].ColName));
				}

				WriteVarLen(w, (uint)cells[s].Length);
				for (int i = 0; i < cells[s].Length; i++)
				{
					WriteVarLen(w, cells[s][i]);
				}
			}

			w.Write((byte)0);
			return ms.ToArray();
		}
	}

	private static void WriteArrayTable(BinaryWriter w, List<int[]> table)
	{
		w.Write(table.Count);
		for (int i = 0; i < table.Count; i++)
		{
			int[] arr = table[i];
			WriteVarLen(w, (uint)arr.Length);
			for (int j = 0; j < arr.Length; j++)
			{
				WriteVarLen(w, (uint)arr[j]);
			}
		}
	}

	/// <summary>Returns the value to write for a cell: 0 for empty, otherwise (table index + 1) or the type's direct encoding.</summary>
	private static uint EncodeCell(JoeTables t, ColumnType type, string v, ref int bad)
	{
		if (string.IsNullOrEmpty(v))
		{
			return 0u;
		}

		switch (type)
		{
			case ColumnType.String:
				return (uint)(t.AddString(v) + 1);

			case ColumnType.Boolean:
			{
				string b = v.Trim();
				return (b == "1" || b.Equals("true", System.StringComparison.OrdinalIgnoreCase)) ? 2u : 1u;
			}

			case ColumnType.NonNegativeInt:
				return (uint)System.Math.Max(0, ParseInt(v, ref bad)) + 1u;

			case ColumnType.RawInt:
				return (uint)(t.AddInt(ParseInt(v, ref bad)) + 1);

			case ColumnType.Float:
				return (uint)(t.AddFloat(ParseFloat(v, ref bad)) + 1);

			case ColumnType.StringArray:
				return (uint)(t.AddStringArray(v.Split('|')) + 1);

			case ColumnType.NonNegativeIntArray:
			{
				int[] arr = ParseIntArray(v, ref bad);
				for (int i = 0; i < arr.Length; i++)
				{
					if (arr[i] < 0)
					{
						arr[i] = 0;
					}
				}
				return (uint)(t.AddNonNegativeIntArray(arr) + 1);
			}

			case ColumnType.RawIntArray:
				return (uint)(t.AddRawIntArray(ParseIntArray(v, ref bad)) + 1);

			case ColumnType.FloatArray:
				return (uint)(t.AddFloatArray(ParseFloatArray(v, ref bad)) + 1);

			default:
				return 0u;
		}
	}

	private static int ParseInt(string s, ref int bad)
	{
		int v;
		if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
		{
			return v;
		}
		bad++;
		return 0;
	}

	private static float ParseFloat(string s, ref int bad)
	{
		float v;
		if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
		{
			return v;
		}
		bad++;
		return 0f;
	}

	private static int[] ParseIntArray(string s, ref int bad)
	{
		string[] p = s.Split('|');
		int[] a = new int[p.Length];
		for (int i = 0; i < p.Length; i++)
		{
			a[i] = ParseInt(p[i], ref bad);
		}
		return a;
	}

	private static float[] ParseFloatArray(string s, ref int bad)
	{
		string[] p = s.Split('|');
		float[] a = new float[p.Length];
		for (int i = 0; i < p.Length; i++)
		{
			a[i] = ParseFloat(p[i], ref bad);
		}
		return a;
	}

	private static int FloatBits(float v)
	{
		return System.BitConverter.ToInt32(System.BitConverter.GetBytes(v), 0);
	}

	private static void WriteVarLen(BinaryWriter w, uint val)
	{
		if (val == 0)
		{
			w.Write((byte)0);
			return;
		}

		byte[] tmp = new byte[5];
		int count = 0;
		while (val > 0)
		{
			tmp[count++] = (byte)(val & 0x7F);
			val >>= 7;
		}

		for (int i = count - 1; i >= 0; i--)
		{
			byte b = tmp[i];
			if (i != 0)
			{
				b = (byte)(b | 0x80);
			}
			w.Write(b);
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
				if (ch == '"')
				{
					inQuotes = true;
				}
				else if (ch == ',')
				{
					row.Add(cell.ToString());
					cell.Length = 0;
				}
				else if (ch == '\n')
				{
					row.Add(cell.ToString());
					cell.Length = 0;
					rows.Add(row.ToArray());
					row.Clear();
				}
				else if (ch != '\r')
				{
					cell.Append(ch);
				}
			}
		}

		if (cell.Length > 0 || row.Count > 0)
		{
			row.Add(cell.ToString());
			rows.Add(row.ToArray());
		}
		return rows;
	}
}