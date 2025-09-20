// COPYRIGHT 2025 PotRooms

using System;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics.Metrics;

public class ZMemory_SpanReader_o
{
	private readonly ReadOnlyMemory<byte> _memory;
	private ReadOnlySpan<byte> Span => _memory.Span;
	public int Position { get; set; }
	public int Length => _memory.Length;

	public ZMemory_SpanReader_o(ReadOnlyMemory<byte> memory)
	{
		_memory = memory;
		Position = 0;
	}

	public long ReadInt64()
	{
		EnsureAvailable(8);
		long value = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(Position, 8));
		Position += 8;
		return value;
	}

	public int ReadInt32()
	{
		EnsureAvailable(4);
		int value = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(Position, 4));
		Position += 4;
		return value;
	}

	public short ReadInt16()
	{
		EnsureAvailable(2);
		short value = BinaryPrimitives.ReadInt16LittleEndian(Span.Slice(Position, 2));
		Position += 2;
		return value;
	}

	public bool ReadBool()
	{
		EnsureAvailable(1);
		if (Span[Position] != 1 && Span[Position] != 0)
			return false;
		bool value = Span[Position] != 0;
		Position += 1;
		return value;
	}

	public float ReadFloat()
	{
		EnsureAvailable(4);
		float value = BinaryPrimitives.ReadSingleLittleEndian(Span.Slice(Position, 4));
		Position += 4;
		return value;
	}

	public byte ReadByte()
	{
		EnsureAvailable(1);
		return Span[Position++];
	}

	public byte[] ReadBytes(int length)
	{
		EnsureAvailable(length);
		byte[] result = Span.Slice(Position, length).ToArray();
		Position += length;
		return result;
	}

	public ReadOnlyMemory<byte> GetMemorySlice(int length)
	{
		EnsureAvailable(length);
		var slice = Span.Slice(Position, length);
		Position += length;
		return new ReadOnlyMemory<byte>(slice.ToArray());
	}

	public void Seek(int newPosition)
	{
		if (newPosition < 0 || newPosition > Length)
			throw new ArgumentOutOfRangeException(nameof(newPosition), "Position out of bounds.");
		Position = newPosition;
	}

	// Helpers that still use arrays because pools are arrays
	private string ReadStringFromPool(byte[] pool, int index)
	{
		if (index + 2 > pool.Length) return "";
		short len = BitConverter.ToInt16(pool, index);
		if (len < 0 || index + 2 + len > pool.Length) return "";
		return Encoding.UTF8.GetString(pool, index + 2, len);
	}

	private int[] ReadIntArrayFromPool(byte[] pool, int index)
	{
		if (index + 2 > pool.Length) return Array.Empty<int>();
		short len = BitConverter.ToInt16(pool, index);
		if (len <= 0) return Array.Empty<int>();

		int[] result = new int[len];
		for (int i = 0; i < len; i++)
		{
			int offset = index + 2 + i * 4;
			if (offset + 4 > pool.Length) break;
			result[i] = BitConverter.ToInt32(pool, offset);
		}
		return result;
	}

	private float[] ReadNumberArrayFromPool(byte[] pool, int index)
	{
		if (index + 2 > pool.Length) return Array.Empty<float>();
		short len = BitConverter.ToInt16(pool, index);
		if (len <= 0) return Array.Empty<float>();
		float[] result = new float[len];
		for (int i = 0; i < len; i++)
		{
			int offset = index + 2 + i * 4;
			if (offset + 4 > pool.Length) break;
			result[i] = BitConverter.ToSingle(pool, offset);
		}
		return result;
	}

	public string[][] ReadStringTableFromPool(byte[] strPool, byte[] intPool, int index)
	{
		if (index == 0) return Array.Empty<string[]>();
		int[] subIndices = ReadIntArrayFromPool(intPool, index);
		string[][] table = new string[subIndices.Length][];
		for (int i = 0; i < subIndices.Length; i++)
		{
			table[i] = ReadStringTableArrayFromPool(strPool, intPool, subIndices[i]);
		}
		return table;
	}

	public string[] ReadStringTableArrayFromPool(byte[] strPool, byte[] intPool, int index)
	{
		if (index == 0) { return Array.Empty<string>(); }
		int[] subIndices = ReadIntArrayFromPool(intPool, index);
		string[] strings = new string[subIndices.Length];
		for (int i = 0; i < subIndices.Length; i++)
		{
			strings[i] = ReadStringFromPool(strPool, subIndices[i]);
		}
		;
		return strings;
	}

	public string[] ReadStringArrayFromPool(byte[] pool, int index)
	{
		if (index + 2 > pool.Length) return Array.Empty<string>();
		short len = BitConverter.ToInt16(pool, index);
		if (len <= 0) return Array.Empty<string>();

		using var reader = new BinaryReader(new MemoryStream(pool));
		reader.BaseStream.Seek(index + 2, SeekOrigin.Begin);

		string[] result = new string[len];
		for (int i = 0; i < len; i++)
		{
			if (reader.BaseStream.Position + 2 > reader.BaseStream.Length) break;
			short strLen = reader.ReadInt16();
			if (reader.BaseStream.Position + strLen > reader.BaseStream.Length) break;
			byte[] strBytes = reader.ReadBytes(strLen);
			result[i] = Encoding.UTF8.GetString(strBytes);
		}
		return result;
	}

	public int[][] ReadIntTableFromPool(byte[] pool, int index)
	{
		if (index + 2 > pool.Length) return Array.Empty<int[]>();
		short len = BitConverter.ToInt16(pool, index);
		if (len <= 0) return Array.Empty<int[]>();

		using var reader = new BinaryReader(new MemoryStream(pool));
		reader.BaseStream.Seek(index + 2, SeekOrigin.Begin);

		int[][] table = new int[len][];

		for (int i = 0; i < len; i++)
		{
			int subIndex = BitConverter.ToInt32(pool, index + 2 + i * 4);
			if (subIndex < 0)
				return Array.Empty<int[]>();
			table[i] = ReadIntArrayFromPool(pool, subIndex);
		}

		return table;
	}

	public string ReadString(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		return ReadStringFromPool(loader.StringPool._memory.ToArray(), index);
	}

	public int[][] ReadInt32Table(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		return ReadIntTableFromPool(loader.IntArrayPool._memory.ToArray(), index);
	}

	public string[] ReadStringTripleArray(Bokura_Table_ZLoader_o loader)
	{
		int start = ReadInt32();

		return [];

		//byte[] intArrayPool = loader.IntArrayPool._memory.ToArray();
		//byte[] stringPool = loader.StringPool._memory.ToArray();

		//List<string> strings = new List<string>();

		//int cursor = start;

		//for (int i = 0; i < 3; i++) // TRIPLE array
		//{
		//	// need at least 2 bytes for length
		//	if (cursor + 2 > intArrayPool.Length)
		//	{
		//		Console.WriteLine($"[i={i}] Out of bounds reading length at {cursor}.");
		//		break;
		//	}

		//	short len = BitConverter.ToInt16(intArrayPool, cursor);
		//	Console.WriteLine($"[i={i}] arrayLen {len}");

		//	List<string[]> strings1 = new();

		//	int entriesStart = cursor + 2;
		//	int bytesNeeded = len * 4;

		//	for (int j = 0; j < len; j++)
		//	{
		//		int strArrIndex = BitConverter.ToInt32(intArrayPool, entriesStart + j * 4);
		//		Console.WriteLine($"[i={i}][j={j}] strArrIndex {strArrIndex}");
		//	}

		//	// advance cursor to the next array header
		//	cursor = entriesStart + bytesNeeded; // cursor += 2 + len*4
		//}

		//return strings.ToArray();
	}

	public float[][] ReadNumberTable(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		byte[] intArrayPool = loader.IntArrayPool._memory.ToArray();

		List<float[]> result = new List<float[]>();

		var len = BitConverter.ToInt16(intArrayPool, index);

		for (int i = 0; i < len; i++)
		{
			int entryIndex = BitConverter.ToInt32(intArrayPool, index + 2 + i * 4);
			float[] floats = ReadNumberArrayFromPool(loader.NumberArrayPool._memory.ToArray(), entryIndex);
			result.Add(floats);
		}

		return result.ToArray();
	}

	public string[][] ReadStringTable(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		return ReadStringTableFromPool(loader.StringPool._memory.ToArray(), loader.IntArrayPool._memory.ToArray(), index);
	}


	public int[] ReadIntArray(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		//Console.WriteLine(index);
		if (index <= 0) return Array.Empty<int>();
		byte[] pool = loader.IntArrayPool._memory.ToArray();
		short arrayLength = BitConverter.ToInt16(pool, index);
		int currentIntArrayOffset = index + 2;
		int[] ret = new int[arrayLength];
		if (arrayLength <= 0)
		{
			return Array.Empty<int>();
		}
		for (int i = 0; i < arrayLength; i++)
		{
			int val = BitConverter.ToInt32(pool, currentIntArrayOffset);
			currentIntArrayOffset += 4;
			ret[i] = val;
		}
		return ret;
	}


	public long[] ReadInt64Array(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		if (index <= 0) return Array.Empty<long>();
		byte[] pool = loader.Int64ArrayPool._memory.ToArray();
		short arrayLength = BitConverter.ToInt16(pool, index);
		int currentIntArrayOffset = index + 2;
		long[] ret = new long[arrayLength];
		if (arrayLength <= 0)
		{
			return Array.Empty<long>();
		}
		for (int i = 0; i < arrayLength; i++)
		{
			long val = BitConverter.ToInt64(pool, currentIntArrayOffset);
			currentIntArrayOffset += 8;
			ret[i] = val;
		}
		return ret;
	}

	public string[] ReadStringArray(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		//Console.WriteLine(index);
		if (index <= 0) return Array.Empty<string>();
		byte[] pool = loader.IntArrayPool._memory.ToArray();
		short arrayLength = BitConverter.ToInt16(pool, index);
		int currentIntArrayOffset = index + 2;
		long[] ret = new long[arrayLength];
		if (arrayLength <= 0)
		{
			return Array.Empty<string>();
		}
		for (int i = 0; i < arrayLength; i++)
		{
			
		}
		return [];
	}


	public float[] ReadNumberArray(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();
		//if (index <= 0) return Array.Empty<float>();
		return ReadNumberArrayFromPool(loader.NumberArrayPool._memory.ToArray(), index);
	}

	public Vector2[] ReadVector2Array(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();

		if (loader == null || loader.Vector2ArrayPool?._memory == null)
		{
			Console.WriteLine("Loader or Vector2ArrayPool are null.");
			return Array.Empty<Vector2>();
		}

		byte[] vector2ArrayPool = loader.Vector2ArrayPool._memory.ToArray();

		// Read number of elements from the IntArrayPool
		short arrayLength = BitConverter.ToInt16(vector2ArrayPool, index);
		//Console.WriteLine($"Read array length: {arrayLength} from Vector2ArrayPool at offset {index}");

		if (arrayLength <= 0)
		{
			return Array.Empty<Vector2>();
		}

		Vector2[] result = new Vector2[arrayLength];

		int currentIntArrayOffset = index + 2; // move past the short length

		const int floatSize = 4;
		const int vector2Size = floatSize * 2;

		for (int i = 0; i < arrayLength; i++)
		{
			// Each entry here is an Int32 offset into Vector2ArrayPool
			int vectorOffsetInPool = currentIntArrayOffset + vector2Size * i;

			float x = BitConverter.ToSingle(vector2ArrayPool, vectorOffsetInPool);
			float y = BitConverter.ToSingle(vector2ArrayPool, vectorOffsetInPool + floatSize);

			result[i] = new Vector2(x, y);
			//Console.WriteLine($"  Read Vector2[{i}]: {result[i]} from Vector2ArrayPool at offset {vectorOffsetInPool}");
		}

		return result;
	}

	public int[] ReadMLStringArray(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();

		byte[] intArrayPool = loader.IntArrayPool._memory.ToArray();

		short arrayLength = BitConverter.ToInt16(intArrayPool, index);

		int[] result = new int[arrayLength];

		if (arrayLength <= 0)
			return Array.Empty<int>();

		for (int i = 0; i < arrayLength; i++)
		{
			int pos = index + 2 + i * 4;
			int hash = BitConverter.ToInt32(intArrayPool, pos);
			result[i] = hash;
		}

		return result;
	}

	public Vector3[] ReadVector3Array(Bokura_Table_ZLoader_o loader)
	{
		int index = ReadInt32();

		if (loader == null || loader.Vector3ArrayPool?._memory == null)
		{
			Console.WriteLine("Loader or Vector3ArrayPool are null.");
			return Array.Empty<Vector3>();
		}

		byte[] vector3ArrayPool = loader.Vector3ArrayPool._memory.ToArray();

		// Read number of elements from the IntArrayPool
		short arrayLength = BitConverter.ToInt16(vector3ArrayPool, index);
		//Console.WriteLine($"Read array length: {arrayLength} from Vector3ArrayPool at offset {index}");

		if (arrayLength <= 0)
		{
			return Array.Empty<Vector3>();
		}

		Vector3[] result = new Vector3[arrayLength];

		int currentIntArrayOffset = index + 2; // move past the short length

		const int floatSize = 4;
		const int vector3Size = floatSize * 3;

		for (int i = 0; i < arrayLength; i++)
		{
			// Each entry here is an Int32 offset into Vector3ArrayPool
			int vectorOffsetInPool = currentIntArrayOffset + vector3Size * i;

			float x = BitConverter.ToSingle(vector3ArrayPool, vectorOffsetInPool);
			float y = BitConverter.ToSingle(vector3ArrayPool, vectorOffsetInPool + floatSize);
			float z = BitConverter.ToSingle(vector3ArrayPool, vectorOffsetInPool + floatSize * 2);

			result[i] = new Vector3(x, y, z);
			//Console.WriteLine($"  Read Vector3[{i}]: {result[i]} from Vector3ArrayPool at offset {vectorOffsetInPool}");
		}

		return result;
	}

	private void EnsureAvailable(int count)
	{
		if (Position + count > Length)
			throw new EndOfStreamException($"Cannot read {count} bytes: End of stream.");
	}


	public struct Vector2
	{
		public float x;
		public float y;

		public Vector2(float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString()
		{
			return $"({x}, {y})";
		}
	}

	public struct Vector3
	{
		public float x;
		public float y;
		public float z;

		public Vector3(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public override string ToString()
		{
			return $"({x}, {y}, {z})";
		}
	}
}
