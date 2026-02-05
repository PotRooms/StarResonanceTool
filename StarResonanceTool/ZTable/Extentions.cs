// COPYRIGHT 2025 PotRooms

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using StarResonanceTool;

internal static class ReaderExtensions
{
	private static Dictionary<int, int>? _locIndex;

	public static string ReadString(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		if (index < 0) return string.Empty;

		ReadOnlySpan<byte> pool = loader.StringPool.Memory.Span;

		if ((uint)(index + 2) > (uint)pool.Length)
			return LookupLocalized(index);

		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len < 0) return string.Empty;

		int start = index + 2;
		if ((uint)(start + len) > (uint)pool.Length) return string.Empty;

		return Encoding.UTF8.GetString(pool.Slice(start, len));
	}

	public static int[] ReadIntArray(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<int>() : ReadIntArrayFromPool(loader.IntArrayPool.Memory.Span, index);
	}

	public static long[] ReadInt64Array(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<long>() : ReadInt64ArrayFromPool(loader.Int64ArrayPool.Memory.Span, index);
	}

	public static float[] ReadNumberArray(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<float>() : ReadNumberArrayFromPool(loader.NumberArrayPool.Memory.Span, index);
	}

	public static ZMemory_SpanReader_o.Vector2[] ReadVector2Array(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<ZMemory_SpanReader_o.Vector2>() : ReadVector2ArrayFromPool(loader.Vector2ArrayPool.Memory.Span, index);
	}

	public static ZMemory_SpanReader_o.Vector3[] ReadVector3Array(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<ZMemory_SpanReader_o.Vector3>() : ReadVector3ArrayFromPool(loader.Vector3ArrayPool.Memory.Span, index);
	}

	public static string[] ReadStringArray(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		if (index <= 0) return Array.Empty<string>();

		return ReadStringArrayFromPools(loader.IntArrayPool.Memory.Span, loader.StringPool.Memory.Span, index);
	}

	public static string[] ReadMLStringArray(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		if (index <= 0) return Array.Empty<string>();

		ReadOnlySpan<byte> intPool = loader.IntArrayPool.Memory.Span;
		if ((uint)(index + 2) > (uint)intPool.Length) return Array.Empty<string>();

		short len = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(index, 2));
		if (len <= 0) return Array.Empty<string>();

		string[] result = new string[len];
		for (int i = 0; i < len; i++)
		{
			int pos = index + 2 + i * 4;
			if ((uint)(pos + 4) > (uint)intPool.Length) break;

			int hash = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(pos, 4));
			result[i] = LookupLocalized(hash);
		}
		return result;
	}

	public static int[][] ReadInt32Table(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<int[]>() : ReadIntTableFromPool(loader.IntArrayPool.Memory.Span, index);
	}

	public static string[][] ReadStringTable(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		return index <= 0 ? Array.Empty<string[]>() : ReadStringTableFromPools(loader.StringPool.Memory.Span, loader.IntArrayPool.Memory.Span, index);
	}

	public static float[][] ReadNumberTable(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		if (index <= 0) return Array.Empty<float[]>();

		ReadOnlySpan<byte> intPool = loader.IntArrayPool.Memory.Span;
		if ((uint)(index + 2) > (uint)intPool.Length) return Array.Empty<float[]>();

		short len = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(index, 2));
		if (len <= 0) return Array.Empty<float[]>();

		var result = new float[len][];
		for (int i = 0; i < len; i++)
		{
			int entryPos = index + 2 + i * 4;
			if ((uint)(entryPos + 4) > (uint)intPool.Length) break;

			int entryIndex = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(entryPos, 4));
			result[i] = entryIndex > 0
				? ReadNumberArrayFromPool(loader.NumberArrayPool.Memory.Span, entryIndex)
				: Array.Empty<float>();
		}
		return result;
	}

	public static string[][] ReadStringTripleArray(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int start = reader.ReadInt32();
		if (start <= 0) return Array.Empty<string[]>();

		ReadOnlySpan<byte> intPool = loader.IntArrayPool.Memory.Span;
		ReadOnlySpan<byte> strPool = loader.StringPool.Memory.Span;

		if ((uint)(start + 2) > (uint)intPool.Length) return Array.Empty<string[]>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(start, 2));
		if (len <= 0) return Array.Empty<string[]>();

		var rows = new List<string[]>(len);
		int entriesStart = start + 2;

		for (int j = 0; j < len; j++)
		{
			int idxPos = entriesStart + j * 4;
			if ((uint)(idxPos + 4) > (uint)intPool.Length) break;

			int strArrIndex = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(idxPos, 4));
			if (strArrIndex <= 0) break;

			if ((uint)(strArrIndex + 2) > (uint)intPool.Length) break;
			short count = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(strArrIndex, 2));
			if (count <= 0) continue;

			int offsetBase = strArrIndex + 2;
			for (int a = 0; a < count; a++)
			{
				int pos = offsetBase + a * 4;
				if ((uint)(pos + 4) > (uint)intPool.Length) break;

				int inner = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(pos, 4));
				if (inner <= 0) continue;

				rows.Add(ReadStringArrayFromPools(intPool, strPool, inner));
			}
		}

		return rows.ToArray();
	}

	public static string[][] ReadMLStringTable(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		if (index <= 0) return Array.Empty<string[]>();

		ReadOnlySpan<byte> intPool = loader.IntArrayPool.Memory.Span;
		if ((uint)(index + 2) > (uint)intPool.Length) return Array.Empty<string[]>();

		short tableLen = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(index, 2));
		if (tableLen <= 0) return Array.Empty<string[]>();

		var ret = new List<string[]>(tableLen);

		for (int i = 0; i < tableLen; i++)
		{
			int ipos = index + 2 + i * 4;
			if ((uint)(ipos + 4) > (uint)intPool.Length) break;

			int readPos = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(ipos, 4));
			if (readPos <= 0) { ret.Add(Array.Empty<string>()); continue; }

			if ((uint)(readPos + 2) > (uint)intPool.Length) { ret.Add(Array.Empty<string>()); continue; }
			short arrayLength = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(readPos, 2));
			if (arrayLength <= 0) { ret.Add(Array.Empty<string>()); continue; }

			string[] result = new string[arrayLength];
			for (int j = 0; j < arrayLength; j++)
			{
				int pos = readPos + 2 + j * 4;
				if ((uint)(pos + 4) > (uint)intPool.Length) break;

				int hash = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(pos, 4));
				result[j] = LookupLocalized(hash);
			}

			ret.Add(result);
		}

		return ret.ToArray();
	}

	public static Dictionary<int, int> ReadKVIntInt(this ZMemory_SpanReader_o reader, BokuraTableLoader loader)
	{
		int index = reader.ReadInt32();
		var ret = new Dictionary<int, int>();

		ReadOnlySpan<byte> pool = loader.MapIntIntPool.Memory.Span;
		if (index <= 0 || (uint)(index + 2) > (uint)pool.Length) return ret;

		short tableLen = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (tableLen <= 0) return ret;

		for (int i = 0; i < tableLen; i++)
		{
			int keypos = index + 2 + i * 4;
			int valpos = index + 2 + (i + 1) * 4;
			if ((uint)(valpos + 4) > (uint)pool.Length) break;

			int key = BinaryPrimitives.ReadInt32LittleEndian(pool.Slice(keypos, 4));
			int val = BinaryPrimitives.ReadInt32LittleEndian(pool.Slice(valpos, 4));
			ret[key] = val;
		}

		return ret;
	}

	public static object ReadTable(this ZMemory_SpanReader_o reader, string typeName, BokuraTableLoader loader)
		=> reader.ReadInt32();

	private static string LookupLocalized(int key)
	{
		_locIndex ??= BuildIndexMap(MainApp.Indexes);

		if (_locIndex.TryGetValue(key, out int stringIndex))
		{
			var arr = MainApp.AllLocalizationStrings;
			if ((uint)stringIndex < (uint)arr.Length)
				return arr[stringIndex];
		}

		return string.Empty;
	}

	private static Dictionary<int, int> BuildIndexMap(KeyValuePair<int, int>[]? indexes)
	{
		if (indexes is null || indexes.Length == 0)
			return new Dictionary<int, int>(0);

		var map = new Dictionary<int, int>(indexes.Length);
		for (int i = 0; i < indexes.Length; i++)
			map[indexes[i].Key] = indexes[i].Value;

		return map;
	}

	private static int[] ReadIntArrayFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<int>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<int>();

		int[] result = new int[len];
		int offset = index + 2;
		for (int i = 0; i < len; i++, offset += 4)
		{
			if ((uint)(offset + 4) > (uint)pool.Length) break;
			result[i] = BinaryPrimitives.ReadInt32LittleEndian(pool.Slice(offset, 4));
		}
		return result;
	}

	private static long[] ReadInt64ArrayFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<long>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<long>();

		long[] result = new long[len];
		int offset = index + 2;
		for (int i = 0; i < len; i++, offset += 8)
		{
			if ((uint)(offset + 8) > (uint)pool.Length) break;
			result[i] = BinaryPrimitives.ReadInt64LittleEndian(pool.Slice(offset, 8));
		}
		return result;
	}

	private static float[] ReadNumberArrayFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<float>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<float>();

		float[] result = new float[len];
		int offset = index + 2;
		for (int i = 0; i < len; i++, offset += 4)
		{
			if ((uint)(offset + 4) > (uint)pool.Length) break;
			result[i] = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset, 4));
		}
		return result;
	}

	private static ZMemory_SpanReader_o.Vector2[] ReadVector2ArrayFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<ZMemory_SpanReader_o.Vector2>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<ZMemory_SpanReader_o.Vector2>();

		var result = new ZMemory_SpanReader_o.Vector2[len];
		int offset = index + 2;
		for (int i = 0; i < len; i++, offset += 8)
		{
			if ((uint)(offset + 8) > (uint)pool.Length) break;
			float x = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset, 4));
			float y = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset + 4, 4));
			result[i] = new ZMemory_SpanReader_o.Vector2(x, y);
		}
		return result;
	}

	private static ZMemory_SpanReader_o.Vector3[] ReadVector3ArrayFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<ZMemory_SpanReader_o.Vector3>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<ZMemory_SpanReader_o.Vector3>();

		var result = new ZMemory_SpanReader_o.Vector3[len];
		int offset = index + 2;
		for (int i = 0; i < len; i++, offset += 12)
		{
			if ((uint)(offset + 12) > (uint)pool.Length) break;
			float x = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset, 4));
			float y = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset + 4, 4));
			float z = BinaryPrimitives.ReadSingleLittleEndian(pool.Slice(offset + 8, 4));
			result[i] = new ZMemory_SpanReader_o.Vector3(x, y, z);
		}
		return result;
	}

	private static string[] ReadStringArrayFromPools(ReadOnlySpan<byte> intPool, ReadOnlySpan<byte> strPool, int index)
	{
		if ((uint)(index + 2) > (uint)intPool.Length) return Array.Empty<string>();
		short count = BinaryPrimitives.ReadInt16LittleEndian(intPool.Slice(index, 2));
		if (count <= 0) return Array.Empty<string>();

		string[] result = new string[count];
		int baseOffset = index + 2;

		for (int i = 0; i < count; i++)
		{
			int pos = baseOffset + i * 4;
			if ((uint)(pos + 4) > (uint)intPool.Length) break;

			int strIndex = BinaryPrimitives.ReadInt32LittleEndian(intPool.Slice(pos, 4));
			result[i] = ReadPlainStringFromPool(strPool, strIndex);
		}

		return result;
	}

	private static string ReadPlainStringFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return string.Empty;
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len < 0) return string.Empty;

		int start = index + 2;
		if ((uint)(start + len) > (uint)pool.Length) return string.Empty;

		return Encoding.UTF8.GetString(pool.Slice(start, len));
	}

	private static int[][] ReadIntTableFromPool(ReadOnlySpan<byte> pool, int index)
	{
		if ((uint)(index + 2) > (uint)pool.Length) return Array.Empty<int[]>();
		short len = BinaryPrimitives.ReadInt16LittleEndian(pool.Slice(index, 2));
		if (len <= 0) return Array.Empty<int[]>();

		int[][] table = new int[len][];
		for (int i = 0; i < len; i++)
		{
			int subPos = index + 2 + i * 4;
			if ((uint)(subPos + 4) > (uint)pool.Length) break;

			int subIndex = BinaryPrimitives.ReadInt32LittleEndian(pool.Slice(subPos, 4));
			if (subIndex < 0) return Array.Empty<int[]>();

			table[i] = ReadIntArrayFromPool(pool, subIndex);
		}
		return table;
	}

	private static string[][] ReadStringTableFromPools(ReadOnlySpan<byte> strPool, ReadOnlySpan<byte> intPool, int index)
	{
		if (index == 0) return Array.Empty<string[]>();

		int[] subIndices = ReadIntArrayFromPool(intPool, index);
		if (subIndices.Length == 0) return Array.Empty<string[]>();

		string[][] table = new string[subIndices.Length][];
		for (int i = 0; i < subIndices.Length; i++)
			table[i] = ReadStringTableArrayFromPools(strPool, intPool, subIndices[i]);

		return table;
	}

	private static string[] ReadStringTableArrayFromPools(ReadOnlySpan<byte> strPool, ReadOnlySpan<byte> intPool, int index)
	{
		if (index == 0) return Array.Empty<string>();

		int[] subIndices = ReadIntArrayFromPool(intPool, index);
		if (subIndices.Length == 0) return Array.Empty<string>();

		string[] strings = new string[subIndices.Length];
		for (int i = 0; i < subIndices.Length; i++)
		{
			int strIndex = subIndices[i];
			strings[i] = (uint)(strIndex + 2) > (uint)strPool.Length
				? LookupLocalized(strIndex)
				: ReadPlainStringFromPool(strPool, strIndex);
		}
		return strings;
	}
}
