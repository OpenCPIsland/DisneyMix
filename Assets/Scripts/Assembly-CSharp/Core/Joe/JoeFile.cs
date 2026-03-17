using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.MetaData;

namespace Core.Joe
{
	public class JoeFile
	{
		private const int MAX_SUPPORTED_VERSION = 1;

		private const int EXTRA_ARRAYS_VERSION = 1;

		private Sheet[] sheets;

		private byte[] bytes;

		private int pos;

		private int rem;

		private int ver = -1;

		private string[] strings;

		private int[] ints;

		private float[] floats;

		private string[][] stringArrays;

		private int[][] nonNegativeIntArrays;

		private int[][] rawIntArrays;

		private float[][] floatArrays;

		private int stringCount;

		private int intCount;

		private int floatCount;

		private int stringArrayCount;

		private int nonNegativeIntArrayCount;

		private int rawIntArrayCount;

		private int floatArrayCount;

		public JoeFile(byte[] rawFileBytes)
		{
			bytes = rawFileBytes;
			if (bytes != null)
			{
				Parse();
				bytes = null;
			}
		}

		public Sheet GetSheet(int i)
		{
			return (sheets != null && i >= 0 && i < sheets.Length) ? sheets[i] : null;
		}

		public Sheet[] GetAllSheets()
		{
			return sheets;
		}

		private bool FatalError(string error)
		{
			sheets = null;
			bytes = null;
			strings = null;
			ints = null;
			floats = null;
			stringArrays = null;
			nonNegativeIntArrays = null;
			rawIntArrays = null;
			floatArrays = null;
			return false;
		}

		private bool RangeError(string kind, int index, int count)
		{
			return FatalError(string.Format("invalid {0} index: {1}:{2}", kind, index, count));
		}

		private bool Parse()
		{
			int num = bytes.Length;
			pos = 0;
			rem = num;
			ver = -1;
			if (!ParseSignature())
			{
				return FatalError("invalid signature");
			}
			if (!ParseVersion())
			{
				return FatalError("unsupported version: " + ver);
			}
			if (!ParseStringTable())
			{
				return FatalError("unable to parse string table");
			}
			if (!ParseIntegerTable())
			{
				return FatalError("unable to parse integer table");
			}
			if (!ParseFloatTable())
			{
				return FatalError("unable to parse float table");
			}
			if (!ParseStringArrayTable())
			{
				return FatalError("unable to parse string array table");
			}
			if (ver >= 1)
			{
				if (!ParseNonNegativeIntArrayTable())
				{
					return FatalError("unable to parse non-negative int array table");
				}
				if (!ParseRawIntArrayTable())
				{
					return FatalError("unable to parse raw int array table");
				}
				if (!ParseFloatArrayTable())
				{
					return FatalError("unable to parse float array table");
				}
			}
			if (!ParseSheetNames())
			{
				return FatalError("unable to parse sheet names");
			}
			int i = 0;
			for (int num2 = sheets.Length; i < num2; i++)
			{
				Sheet sheet = sheets[i];
				if (!ParseSheetColumns(sheet))
				{
					return FatalError("unable to parse sheet columns: " + sheet.SheetName);
				}
				if (!ParseSheetCells(sheet))
				{
					return FatalError("unable to parse sheet cells: " + sheet.SheetName);
				}
				sheet.SetupComplete();
			}
			if (!ParseFinalByte())
			{
				return FatalError(string.Format("unable to parse final byte {0},{1},{2}", rem, pos, num));
			}
			if (rem != 0 || pos != num)
			{
			}
			return true;
		}

		private bool ParseSignature()
		{
			if ((rem -= 3) < 0)
			{
				return FatalError("not enough bytes for signature");
			}
			if (bytes[pos++] != 74)
			{
				return FatalError("invalid first signature byte");
			}
			if (bytes[pos++] != 79)
			{
				return FatalError("invalid second signature byte");
			}
			if (bytes[pos++] != 69)
			{
				return FatalError("invalid third signature byte");
			}
			return true;
		}

		private bool ParseVersion()
		{
			if (--rem < 0)
			{
				return FatalError("not enough bytes for version");
			}
			ver = bytes[pos++];
			if (ver > 1)
			{
				return FatalError(string.Format("unsupported version {0} > {1}", ver, 1));
			}
			return true;
		}

		private bool ParseStringTable()
		{
			if (!DecodeDword(out stringCount) || stringCount < 0)
			{
				return FatalError("invalid string table size");
			}
			strings = new string[stringCount];
			for (int i = 0; i < stringCount; i++)
			{
				uint val;
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode string length");
				}
				int num = (int)val;
				if (num < 0)
				{
					return FatalError("invalid string length");
				}
				if ((rem -= num) < 0)
				{
					return FatalError("not enough bytes for string");
				}
				strings[i] = Encoding.UTF8.GetString(bytes, pos, num);
				pos += num;
			}
			return true;
		}

		private bool ParseIntegerTable()
		{
			if (!DecodeDword(out intCount) || intCount < 0)
			{
				return FatalError("invalid integer table size");
			}
			ints = new int[intCount];
			for (int i = 0; i < intCount; i++)
			{
				if ((rem -= 4) < 0)
				{
					return FatalError("not enough bytes for integer");
				}
				ints[i] = BitConverter.ToInt32(bytes, pos);
				pos += 4;
			}
			return true;
		}

		private bool ParseFloatTable()
		{
			if (!DecodeDword(out floatCount) || floatCount < 0)
			{
				return FatalError("invalid float table size");
			}
			floats = new float[floatCount];
			for (int i = 0; i < floatCount; i++)
			{
				if ((rem -= 4) < 0)
				{
					return FatalError("not enough bytes for float");
				}
				floats[i] = BitConverter.ToSingle(bytes, pos);
				pos += 4;
			}
			return true;
		}

		private bool ParseStringArrayTable()
		{
			if (!DecodeDword(out stringArrayCount) || stringArrayCount < 0)
			{
				return FatalError("invalid stringArray table size");
			}
			stringArrays = new string[stringArrayCount][];
			for (int i = 0; i < stringArrayCount; i++)
			{
				uint val;
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode stringArray length");
				}
				int num = (int)val;
				if (num < 0)
				{
					return FatalError("invalid stringArray length");
				}
				string[] array = new string[num];
				for (int j = 0; j < num; j++)
				{
					if (!DecodeVariableLength(out val))
					{
						return FatalError("unable to decode stringArray string index");
					}
					int num2 = (int)val;
					if (num2 < 0 || num2 > stringCount)
					{
						return RangeError("stringArray string", num2, stringCount);
					}
					array[j] = strings[num2];
				}
				stringArrays[i] = array;
			}
			return true;
		}

		private bool ParseNonNegativeIntArrayTable()
		{
			if (!DecodeDword(out nonNegativeIntArrayCount) || nonNegativeIntArrayCount < 0)
			{
				return FatalError("invalid nonNegativeIntArray table size");
			}
			nonNegativeIntArrays = new int[nonNegativeIntArrayCount][];
			for (int i = 0; i < nonNegativeIntArrayCount; i++)
			{
				uint val;
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode nonNegativeIntArray length");
				}
				int num = (int)val;
				if (num < 0)
				{
					return FatalError("invalid nonNegativeIntArray length");
				}
				int[] array = new int[num];
				for (int j = 0; j < num; j++)
				{
					if (!DecodeVariableLength(out val))
					{
						return FatalError("unable to decode nonNegativeIntArray int");
					}
					array[j] = (int)val;
				}
				nonNegativeIntArrays[i] = array;
			}
			return true;
		}

		private bool ParseRawIntArrayTable()
		{
			if (!DecodeDword(out rawIntArrayCount) || rawIntArrayCount < 0)
			{
				return FatalError("invalid rawIntArray table size");
			}
			rawIntArrays = new int[rawIntArrayCount][];
			for (int i = 0; i < rawIntArrayCount; i++)
			{
				uint val;
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode rawIntArray length");
				}
				int num = (int)val;
				if (num < 0)
				{
					return FatalError("invalid rawIntArray length");
				}
				int[] array = new int[num];
				for (int j = 0; j < num; j++)
				{
					if (!DecodeVariableLength(out val))
					{
						return FatalError("unable to decode rawIntArray int index");
					}
					int num2 = (int)val;
					if (num2 < 0 || num2 > intCount)
					{
						return RangeError("rawIntArray int", num2, intCount);
					}
					array[j] = ints[num2];
				}
				rawIntArrays[i] = array;
			}
			return true;
		}

		private bool ParseFloatArrayTable()
		{
			if (!DecodeDword(out floatArrayCount) || floatArrayCount < 0)
			{
				return FatalError("invalid floatArray table size");
			}
			floatArrays = new float[floatArrayCount][];
			for (int i = 0; i < floatArrayCount; i++)
			{
				uint val;
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode floatArray length");
				}
				int num = (int)val;
				if (num < 0)
				{
					return FatalError("invalid floatArray length");
				}
				float[] array = new float[num];
				for (int j = 0; j < num; j++)
				{
					if (!DecodeVariableLength(out val))
					{
						return FatalError("unable to decode floatArray float index");
					}
					int num2 = (int)val;
					if (num2 < 0 || num2 > floatCount)
					{
						return RangeError("floatArray float", num2, floatCount);
					}
					array[j] = floats[num2];
				}
				floatArrays[i] = array;
			}
			return true;
		}

		private bool ParseSheetNames()
		{
			uint val;
			if (!DecodeVariableLength(out val))
			{
				return FatalError("unable to decode sheet count");
			}
			int num = (int)val;
			if (num < 0)
			{
				return FatalError("invalid sheet count");
			}
			sheets = new Sheet[num];
			for (int i = 0; i < num; i++)
			{
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode sheet string index");
				}
				int num2 = (int)val;
				if (num2 < 0 || num2 > stringCount)
				{
					return RangeError("sheet string", num2, stringCount);
				}
				string sheetName = strings[num2];
				Sheet sheet = new Sheet(sheetName, strings, floats, stringArrays, nonNegativeIntArrays, rawIntArrays, floatArrays);
				sheets[i] = sheet;
			}
			return true;
		}

		private bool ParseSheetColumns(Sheet sheet)
		{
			uint val;
			if (!DecodeVariableLength(out val))
			{
				return FatalError("unable to decode column count");
			}
			int num = (int)val;
			if (num < 0)
			{
				return FatalError("invalid column count");
			}
			Column[] array = new Column[num];
			for (int i = 0; i < num; i++)
			{
				if (--rem < 0)
				{
					return FatalError("not enough bytes for column type");
				}
				ColumnType colType = (ColumnType)bytes[pos++];
				if (!DecodeVariableLength(out val))
				{
					return FatalError("unable to decode column string index");
				}
				int num2 = (int)val;
				if (num2 < 0 || num2 > stringCount)
				{
					return RangeError("column string", num2, stringCount);
				}
				string colName = strings[num2];
				array[i] = new Column(colName, colType);
			}
			sheet.SetupColumns(array);
			return true;
		}

		private bool ParseSheetCells(Sheet sheet)
		{
			uint val;
			if (!DecodeVariableLength(out val))
			{
				return FatalError("unable to decode cell count");
			}
			int num = (int)val;
			if (num < 0)
			{
				return FatalError("invalid cell count");
			}
			uint[] array = new uint[num];
			Column[] array2 = sheet.InternalGetAllColumns();
			int num2 = array2.Length;
			int num3 = 0;
			if (num2 == 0 || num % num2 != 0)
			{
				return FatalError(string.Format("cell count {0} is not a multiple of column count {1}", num, num2));
			}
			for (int i = 0; i < num; i++)
			{
				if (rem == 0)
				{
					return FatalError("not enough bytes for cell");
				}
				if (bytes[pos] == 0)
				{
					array[i] = 0u;
					pos++;
					rem--;
				}
				else
				{
					ColumnType colType = array2[num3].ColType;
					uint cellValue;
					switch (colType)
					{
					case ColumnType.String:
						if (!GetCellValueForTableLookup(out cellValue, colType, stringCount))
						{
							return false;
						}
						break;
					case ColumnType.Boolean:
						if (--rem < 0)
						{
							return FatalError("not enough bytes for bool cell");
						}
						cellValue = bytes[pos++];
						break;
					case ColumnType.NonNegativeInt:
						if (!DecodeVariableLength(out cellValue))
						{
							return FatalError("unable to decode non-negative int cell");
						}
						break;
					case ColumnType.RawInt:
					{
						if (!DecodeVariableLength(out cellValue))
						{
							return FatalError("unable to decode raw int index");
						}
						int num4 = (int)cellValue;
						if (num4 <= 0 || num4 > intCount)
						{
							return RangeError("raw int", num4, intCount);
						}
						cellValue = (uint)ints[num4 - 1];
						if ((cellValue & 0x80000000u) == 0)
						{
							cellValue++;
						}
						break;
					}
					case ColumnType.Float:
						if (!GetCellValueForTableLookup(out cellValue, colType, floatCount))
						{
							return false;
						}
						break;
					case ColumnType.StringArray:
						if (!GetCellValueForTableLookup(out cellValue, colType, stringArrayCount))
						{
							return false;
						}
						break;
					case ColumnType.NonNegativeIntArray:
						if (!GetCellValueForTableLookup(out cellValue, colType, nonNegativeIntArrayCount))
						{
							return false;
						}
						break;
					case ColumnType.RawIntArray:
						if (!GetCellValueForTableLookup(out cellValue, colType, rawIntArrayCount))
						{
							return false;
						}
						break;
					case ColumnType.FloatArray:
						if (!GetCellValueForTableLookup(out cellValue, colType, floatArrayCount))
						{
							return false;
						}
						break;
					default:
						return FatalError("unsupported column type");
					}
					array[i] = cellValue;
				}
				if (++num3 == num2)
				{
					num3 = 0;
				}
			}
			sheet.SetupCells(array);
			return true;
		}

		private bool GetCellValueForTableLookup(out uint cellValue, ColumnType colType, int count)
		{
			if (!DecodeVariableLength(out cellValue))
			{
				return FatalError(string.Format("unable to decode cell {0} index", colType));
			}
			int num = (int)cellValue;
			if (num <= 0 || num > count)
			{
				return RangeError(string.Format("cell {0}", colType), num, count);
			}
			return true;
		}

		private bool ParseFinalByte()
		{
			if (--rem < 0)
			{
				return FatalError("not enough bytes for final byte");
			}
			if (bytes[pos++] != 0)
			{
				return FatalError("invalid final byte");
			}
			return true;
		}

		private bool DecodeDword(out int val)
		{
			val = 0;
			if ((rem -= 4) < 0)
			{
				return FatalError("not enough bytes for dword");
			}
			val = BitConverter.ToInt32(bytes, pos);
			pos += 4;
			return true;
		}

		private bool DecodeVariableLength(out uint val)
		{
			val = 0u;
			uint num;
			while (true)
			{
				if (--rem < 0)
				{
					return FatalError("not enough bytes for variable-length");
				}
				num = bytes[pos++];
				if ((num & 0x80) == 0)
				{
					break;
				}
				val = (val << 7) | (num & 0x7F);
			}
			val = (val << 7) | num;
			return true;
		}

		private sealed class SheetWriteData
		{
			public Sheet Sheet;
			public Column[] Columns;
			public List<Row> Rows;
		}

		private sealed class JoeWriteContext
		{
			public readonly List<string> Strings = new List<string>();
			public readonly Dictionary<string, int> StringIndexes = new Dictionary<string, int>();

			public readonly List<int> Ints = new List<int>();
			public readonly Dictionary<int, int> IntIndexes = new Dictionary<int, int>();

			public readonly List<float> Floats = new List<float>();
			public readonly Dictionary<int, int> FloatIndexesByBits = new Dictionary<int, int>();

			public readonly List<string[]> StringArrays = new List<string[]>();
			public readonly Dictionary<string, int> StringArrayIndexes = new Dictionary<string, int>();

			public readonly List<int[]> NonNegativeIntArrays = new List<int[]>();
			public readonly Dictionary<string, int> NonNegativeIntArrayIndexes = new Dictionary<string, int>();

			public readonly List<int[]> RawIntArrays = new List<int[]>();
			public readonly Dictionary<string, int> RawIntArrayIndexes = new Dictionary<string, int>();

			public readonly List<float[]> FloatArrays = new List<float[]>();
			public readonly Dictionary<string, int> FloatArrayIndexes = new Dictionary<string, int>();
		}

		public byte[] Encode()
		{
			return Encode(sheets);
		}

		public static byte[] Encode(Sheet[] sourceSheets)
		{
			if (sourceSheets == null)
			{
				return null;
			}

			List<SheetWriteData> writeSheets = new List<SheetWriteData>();
			JoeWriteContext context = new JoeWriteContext();

			for (int i = 0; i < sourceSheets.Length; i++)
			{
				Sheet sheet = sourceSheets[i];
				if (sheet == null)
				{
					continue;
				}

				Column[] columns = sheet.InternalGetAllColumns();
				if (columns == null)
				{
					continue;
				}

				List<Row> rows = GetOrderedRows(sheet);
				SheetWriteData writeSheet = new SheetWriteData
				{
					Sheet = sheet,
					Columns = columns,
					Rows = rows
				};
				writeSheets.Add(writeSheet);

				GetOrAddString(context, sheet.SheetName);
				for (int c = 0; c < columns.Length; c++)
				{
					GetOrAddString(context, columns[c].ColName);
				}

				for (int r = 0; r < rows.Count; r++)
				{
					Row row = rows[r];
					for (int c = 0; c < columns.Length; c++)
					{
						ColumnType type = columns[c].ColType;
						switch (type)
						{
						case ColumnType.String:
						{
							string value = row.TryGetString(c, null);
							if (value != null)
							{
								GetOrAddString(context, value);
							}
							break;
						}
						case ColumnType.RawInt:
							GetOrAddInt(context, row.TryGetInt(c, 0));
							break;
						case ColumnType.Float:
							GetOrAddFloat(context, row.TryGetFloat(c, 0f));
							break;
						case ColumnType.StringArray:
						{
							string[] value = row.TryGetStringArray(c);
							if (value != null)
							{
								GetOrAddStringArray(context, value);
							}
							break;
						}
						case ColumnType.NonNegativeIntArray:
						{
							int[] value = row.TryGetIntArray(c);
							if (value != null)
							{
								GetOrAddNonNegativeIntArray(context, value);
							}
							break;
						}
						case ColumnType.RawIntArray:
						{
							int[] value = row.TryGetIntArray(c);
							if (value != null)
							{
								GetOrAddRawIntArray(context, value);
							}
							break;
						}
						case ColumnType.FloatArray:
						{
							float[] value = row.TryGetFloatArray(c);
							if (value != null)
							{
								GetOrAddFloatArray(context, value);
							}
							break;
						}
						}
					}
				}
			}

			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(ms))
				{
					writer.Write((byte)74);
					writer.Write((byte)79);
					writer.Write((byte)69);
					writer.Write((byte)1);

					writer.Write(context.Strings.Count);
					for (int i = 0; i < context.Strings.Count; i++)
					{
						string text = context.Strings[i] ?? string.Empty;
						byte[] utf8 = Encoding.UTF8.GetBytes(text);
						WriteVariableLength(writer, (uint)utf8.Length);
						writer.Write(utf8);
					}

					writer.Write(context.Ints.Count);
					for (int i = 0; i < context.Ints.Count; i++)
					{
						writer.Write(context.Ints[i]);
					}

					writer.Write(context.Floats.Count);
					for (int i = 0; i < context.Floats.Count; i++)
					{
						writer.Write(context.Floats[i]);
					}

					writer.Write(context.StringArrays.Count);
					for (int i = 0; i < context.StringArrays.Count; i++)
					{
						string[] arr = context.StringArrays[i];
						WriteVariableLength(writer, (uint)arr.Length);
						for (int j = 0; j < arr.Length; j++)
						{
							int strIndex = context.StringIndexes[arr[j]];
							WriteVariableLength(writer, (uint)strIndex);
						}
					}

					writer.Write(context.NonNegativeIntArrays.Count);
					for (int i = 0; i < context.NonNegativeIntArrays.Count; i++)
					{
						int[] arr = context.NonNegativeIntArrays[i];
						WriteVariableLength(writer, (uint)arr.Length);
						for (int j = 0; j < arr.Length; j++)
						{
							uint value = (arr[j] < 0) ? 0u : (uint)arr[j];
							WriteVariableLength(writer, value);
						}
					}

					writer.Write(context.RawIntArrays.Count);
					for (int i = 0; i < context.RawIntArrays.Count; i++)
					{
						int[] arr = context.RawIntArrays[i];
						WriteVariableLength(writer, (uint)arr.Length);
						for (int j = 0; j < arr.Length; j++)
						{
							int intIndex = context.IntIndexes[arr[j]];
							WriteVariableLength(writer, (uint)intIndex);
						}
					}

					writer.Write(context.FloatArrays.Count);
					for (int i = 0; i < context.FloatArrays.Count; i++)
					{
						float[] arr = context.FloatArrays[i];
						WriteVariableLength(writer, (uint)arr.Length);
						for (int j = 0; j < arr.Length; j++)
						{
							int floatIndex = context.FloatIndexesByBits[FloatToBits(arr[j])];
							WriteVariableLength(writer, (uint)floatIndex);
						}
					}

					WriteVariableLength(writer, (uint)writeSheets.Count);
					for (int i = 0; i < writeSheets.Count; i++)
					{
						int sheetNameIndex = context.StringIndexes[writeSheets[i].Sheet.SheetName];
						WriteVariableLength(writer, (uint)sheetNameIndex);
					}

					for (int i = 0; i < writeSheets.Count; i++)
					{
						SheetWriteData writeSheet = writeSheets[i];
						Column[] columns = writeSheet.Columns;

						WriteVariableLength(writer, (uint)columns.Length);
						for (int c = 0; c < columns.Length; c++)
						{
							writer.Write((byte)columns[c].ColType);
							int colNameIndex = context.StringIndexes[columns[c].ColName];
							WriteVariableLength(writer, (uint)colNameIndex);
						}

						uint cellCount = (uint)(writeSheet.Rows.Count * columns.Length);
						WriteVariableLength(writer, cellCount);

						for (int r = 0; r < writeSheet.Rows.Count; r++)
						{
							Row row = writeSheet.Rows[r];
							for (int c = 0; c < columns.Length; c++)
							{
								ColumnType type = columns[c].ColType;
								switch (type)
								{
								case ColumnType.String:
								{
									string value = row.TryGetString(c, null);
									if (value == null)
									{
										writer.Write((byte)0);
									}
									else
									{
										WriteVariableLength(writer, (uint)(context.StringIndexes[value] + 1));
									}
									break;
								}
								case ColumnType.Boolean:
								{
									bool value = row.TryGetBool(c);
									writer.Write((byte)(value ? 2 : 1));
									break;
								}
								case ColumnType.NonNegativeInt:
								{
									int value = row.TryGetInt(c, 0);
									uint encoded = (uint)((value < 0) ? 1 : (value + 1));
									WriteVariableLength(writer, encoded);
									break;
								}
								case ColumnType.RawInt:
								{
									int value = row.TryGetInt(c, 0);
									int intIndex = context.IntIndexes[value];
									WriteVariableLength(writer, (uint)(intIndex + 1));
									break;
								}
								case ColumnType.Float:
								{
									float value = row.TryGetFloat(c, 0f);
									int floatIndex = context.FloatIndexesByBits[FloatToBits(value)];
									WriteVariableLength(writer, (uint)(floatIndex + 1));
									break;
								}
								case ColumnType.StringArray:
								{
									string[] value = row.TryGetStringArray(c);
									if (value == null)
									{
										writer.Write((byte)0);
									}
									else
									{
										string key = BuildStringArrayKey(value);
										WriteVariableLength(writer, (uint)(context.StringArrayIndexes[key] + 1));
									}
									break;
								}
								case ColumnType.NonNegativeIntArray:
								{
									int[] value = row.TryGetIntArray(c);
									if (value == null)
									{
										writer.Write((byte)0);
									}
									else
									{
										string key = BuildIntArrayKey(value);
										WriteVariableLength(writer, (uint)(context.NonNegativeIntArrayIndexes[key] + 1));
									}
									break;
								}
								case ColumnType.RawIntArray:
								{
									int[] value = row.TryGetIntArray(c);
									if (value == null)
									{
										writer.Write((byte)0);
									}
									else
									{
										string key = BuildIntArrayKey(value);
										WriteVariableLength(writer, (uint)(context.RawIntArrayIndexes[key] + 1));
									}
									break;
								}
								case ColumnType.FloatArray:
								{
									float[] value = row.TryGetFloatArray(c);
									if (value == null)
									{
										writer.Write((byte)0);
									}
									else
									{
										string key = BuildFloatArrayKey(value);
										WriteVariableLength(writer, (uint)(context.FloatArrayIndexes[key] + 1));
									}
									break;
								}
								default:
									writer.Write((byte)0);
									break;
								}
							}
						}
					}

					writer.Write((byte)0);
				}

				return ms.ToArray();
			}
		}

		private static List<Row> GetOrderedRows(Sheet sheet)
		{
			Dictionary<string, Row> rows = sheet.GetAllRows();
			if (rows == null || rows.Count == 0)
			{
				return new List<Row>();
			}
			return rows.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Value).ToList();
		}

		private static int GetOrAddString(JoeWriteContext context, string value)
		{
			value = value ?? string.Empty;
			int index;
			if (context.StringIndexes.TryGetValue(value, out index))
			{
				return index;
			}
			index = context.Strings.Count;
			context.Strings.Add(value);
			context.StringIndexes[value] = index;
			return index;
		}

		private static int GetOrAddInt(JoeWriteContext context, int value)
		{
			int index;
			if (context.IntIndexes.TryGetValue(value, out index))
			{
				return index;
			}
			index = context.Ints.Count;
			context.Ints.Add(value);
			context.IntIndexes[value] = index;
			return index;
		}

		private static int GetOrAddFloat(JoeWriteContext context, float value)
		{
			int bits = FloatToBits(value);
			int index;
			if (context.FloatIndexesByBits.TryGetValue(bits, out index))
			{
				return index;
			}
			index = context.Floats.Count;
			context.Floats.Add(value);
			context.FloatIndexesByBits[bits] = index;
			return index;
		}

		private static int GetOrAddStringArray(JoeWriteContext context, string[] value)
		{
			string key = BuildStringArrayKey(value);
			int index;
			if (context.StringArrayIndexes.TryGetValue(key, out index))
			{
				return index;
			}

			string[] clone = new string[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				clone[i] = value[i] ?? string.Empty;
				GetOrAddString(context, clone[i]);
			}

			index = context.StringArrays.Count;
			context.StringArrays.Add(clone);
			context.StringArrayIndexes[key] = index;
			return index;
		}

		private static int GetOrAddNonNegativeIntArray(JoeWriteContext context, int[] value)
		{
			string key = BuildIntArrayKey(value);
			int index;
			if (context.NonNegativeIntArrayIndexes.TryGetValue(key, out index))
			{
				return index;
			}

			int[] clone = new int[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				clone[i] = (value[i] < 0) ? 0 : value[i];
			}

			index = context.NonNegativeIntArrays.Count;
			context.NonNegativeIntArrays.Add(clone);
			context.NonNegativeIntArrayIndexes[key] = index;
			return index;
		}

		private static int GetOrAddRawIntArray(JoeWriteContext context, int[] value)
		{
			string key = BuildIntArrayKey(value);
			int index;
			if (context.RawIntArrayIndexes.TryGetValue(key, out index))
			{
				return index;
			}

			int[] clone = new int[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				clone[i] = value[i];
				GetOrAddInt(context, clone[i]);
			}

			index = context.RawIntArrays.Count;
			context.RawIntArrays.Add(clone);
			context.RawIntArrayIndexes[key] = index;
			return index;
		}

		private static int GetOrAddFloatArray(JoeWriteContext context, float[] value)
		{
			string key = BuildFloatArrayKey(value);
			int index;
			if (context.FloatArrayIndexes.TryGetValue(key, out index))
			{
				return index;
			}

			float[] clone = new float[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				clone[i] = value[i];
				GetOrAddFloat(context, clone[i]);
			}

			index = context.FloatArrays.Count;
			context.FloatArrays.Add(clone);
			context.FloatArrayIndexes[key] = index;
			return index;
		}

		private static string BuildStringArrayKey(string[] value)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				string v = value[i] ?? string.Empty;
				sb.Append(v.Length);
				sb.Append(':');
				sb.Append(v);
				sb.Append('|');
			}
			return sb.ToString();
		}

		private static string BuildIntArrayKey(int[] value)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(',');
				}
				sb.Append(value[i]);
			}
			return sb.ToString();
		}

		private static string BuildFloatArrayKey(float[] value)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(',');
				}
				sb.Append(FloatToBits(value[i]));
			}
			return sb.ToString();
		}

		private static int FloatToBits(float value)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
		}

		private static void WriteVariableLength(BinaryWriter writer, uint val)
		{
			if (val == 0u)
			{
				writer.Write((byte)0);
				return;
			}

			byte[] parts = new byte[5];
			int count = 0;
			while (val > 0u)
			{
				parts[count++] = (byte)(val & 0x7Fu);
				val >>= 7;
			}

			for (int i = count - 1; i >= 0; i--)
			{
				byte b = parts[i];
				if (i != 0)
				{
					b = (byte)(b | 0x80);
				}
				writer.Write(b);
			}
		}
	}
}
