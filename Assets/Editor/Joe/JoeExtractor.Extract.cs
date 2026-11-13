using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Joe;
using Core.MetaData;
using UnityEditor;
using UnityEngine;

/// <summary>.joe -> CSV folder. See JoeExtractor.cs for the file layout.</summary>
public static partial class JoeExtractor
{
	[MenuItem("Tools/Mix/Joe/Extract contentfile.joe to CSV")]
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

		List<string> manifest = new List<string>();
		HashSet<string> usedFileNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < sheets.Length; i++)
		{
			Sheet sheet = sheets[i];
			Column[] columns = sheet.InternalGetAllColumns();
			var rows = sheet.GetAllRows();

			if (columns == null || rows == null)
			{
				Debug.LogWarning("Skipping sheet " + i + " (" + sheet.SheetName + "): no columns/rows.");
				continue;
			}

			StringBuilder sb = new StringBuilder();

			// Header: "Name:Type" so the CSV is self-describing.
			for (int c = 0; c < columns.Length; c++)
			{
				if (c > 0)
				{
					sb.Append(",");
				}
				sb.Append(EscapeCsv(columns[c].ColName + ":" + columns[c].ColType));
			}
			sb.AppendLine();

			foreach (var kv in rows)
			{
				Row row = kv.Value;

				for (int c = 0; c < columns.Length; c++)
				{
					if (c > 0)
					{
						sb.Append(",");
					}
					sb.Append(EscapeCsv(GetCellValue(row, columns[c], c)));
				}

				sb.AppendLine();
			}

			string fileName = UniqueFileName(MakeSafeFileName(sheet.SheetName), usedFileNames) + ".csv";
			File.WriteAllText(Path.Combine(outputDir, fileName), sb.ToString(), Encoding.UTF8);
			manifest.Add(fileName + "\t" + sheet.SheetName);
		}

		File.WriteAllLines(Path.Combine(outputDir, ManifestFileName), manifest.ToArray(), new UTF8Encoding(false));

		AssetDatabase.Refresh();
		Debug.Log("JOE extraction complete (" + manifest.Count + " sheets).");
	}

	private static string GetCellValue(Row row, Column column, int colIndex)
	{
		switch (column.ColType)
		{
			case ColumnType.Float:
			{
				// NaN as the "missing" default so null cells export as empty. Invariant culture so the CSV
				// round-trips on machines that use a decimal comma.
				float f = row.TryGetFloat(colIndex, float.NaN);
				return float.IsNaN(f) ? string.Empty : FormatFloat(f);
			}
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
					sb.Append(arr[i].ToString(CultureInfo.InvariantCulture));
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
					sb.Append(FormatFloat(arr[i]));
				}
				return sb.ToString();
			}
			default:
				return row.TryGetString(colIndex, string.Empty);
		}
	}

	private static string FormatFloat(float f)
	{
		return f.ToString("R", CultureInfo.InvariantCulture);
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

	private static string UniqueFileName(string baseName, HashSet<string> used)
	{
		string candidate = baseName;
		int n = 2;
		while (!used.Add(candidate))
		{
			candidate = baseName + "_" + n++;
		}
		return candidate;
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
}