// COPYRIGHT 2025 PotRooms


using Newtonsoft.Json;
using Mono.Cecil;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using StarResonanceTool; // For MemoryMarshal

public class Bokura_Table_ZLoader_o
{
	public byte[] _memoryOwner; // Placeholder for System.Buffers.IMemoryOwner_byte__o
	public ReadOnlyMemory<byte> Memory; // Mimics System_Memory_byte__o

	public int DataSize;
	public Dictionary<long, int> _offsets;
	public Tuple<int, int> _bufferRange; // For __PAIR64__(v23, spanReader.fields.Position);
	public ZMemory_SpanReader_o spanReader;

	// Pool fields
	public IntArrayPool IntArrayPool = new IntArrayPool();
	public Int64ArrayPool Int64ArrayPool = new Int64ArrayPool();
	public NumberArrayPool NumberArrayPool = new NumberArrayPool();
	public MapIntIntPool MapIntIntPool = new MapIntIntPool();
	public MapIntNumberPool MapIntNumberPool = new MapIntNumberPool();
	public StringPool StringPool = new StringPool();
	public Vector2ArrayPool Vector2ArrayPool = new Vector2ArrayPool();
	public Vector3ArrayPool Vector3ArrayPool = new Vector3ArrayPool();
	public MapIntVector2Pool MapIntVector2Pool = new MapIntVector2Pool();
	public MapIntVector3Pool MapIntVector3Pool = new MapIntVector3Pool();

	private TypeDefinition parseType;

	public Bokura_Table_ZLoader_o(TypeDefinition _parseType) { this.parseType = _parseType; }

	// Mimics Bokura_Table_ZLoader__LoadPools
	public void LoadPools(int count, ZMemory_SpanReader_o spanReader)
	{
		//Console.WriteLine($"[LoadPools] Loading {count} pools.");
		for (int i = 0; i < count; i++)
		{
			int poolType = spanReader.ReadInt32(); // Reads the pool type (v8)
			//Console.WriteLine($"  [LoadPools] Reading Pool {i + 1}/{count}. Type ID: {poolType}. Current Reader Position (after type): {spanReader.Position}");

			int length = spanReader.ReadInt32(); // Read the length of the pool data;
			ReadOnlyMemory<byte> memorySlice = spanReader.ReadBytes(length);

			if (!memorySlice.IsEmpty && poolType != 0) // Don't try to populate for type 0 (it didn't get a slice)
			{
				switch (poolType)
				{
					case 1: IntArrayPool._memory = memorySlice; IntArrayPool.Populate(); break;
					case 2: Int64ArrayPool._memory = memorySlice; Int64ArrayPool.Populate(); break;
					case 3: NumberArrayPool._memory = memorySlice; NumberArrayPool.Populate(); break;
					case 4: MapIntIntPool._memory = memorySlice; MapIntIntPool.Populate(); break;
					case 5: MapIntNumberPool._memory = memorySlice; MapIntNumberPool.Populate(); break;
					case 6: StringPool._memory = memorySlice; StringPool.Populate(); break;
					case 7: Vector2ArrayPool._memory = memorySlice; Vector2ArrayPool.Populate(); break;
					case 8: Vector3ArrayPool._memory = memorySlice; Vector3ArrayPool.Populate(); break;
					case 9: MapIntVector2Pool._memory = memorySlice; MapIntVector2Pool.Populate(); break;
					case 10: MapIntVector3Pool._memory = memorySlice; MapIntVector3Pool.Populate(); break;
					default:
						// No specific populate for unknown types beyond consuming their bytes
						break;
				}
			}

			//Console.WriteLine(Convert.ToHexString(memorySlice.ToArray()));
		}
	}


	// Mimics Bokura_Table_ZLoader__Load
	public Dictionary<long, Dictionary<string, object>> Load(byte[] memoryManagerData)
	{
		// Simulate Il2Cpp type info loading (ignored for this C# impl)
		// sub_A2FD60, _InterlockedOr etc.

		_memoryOwner = memoryManagerData; // Store the raw byte array
		Memory = new ReadOnlyMemory<byte>(memoryManagerData); // Get ReadOnlyMemory<byte> from the raw data

		// Initialize SpanReader
		spanReader = new ZMemory_SpanReader_o(Memory);

		// Read Header Information
		long discardableHeader = spanReader.ReadInt64();  // Mimics sub_86F590
		int entryCount = spanReader.ReadInt32();          // Mimics v20 = sub_86F660
		int poolCount = spanReader.ReadInt32();           // Mimics v21 = sub_86F660
		int dataBufferOffset = spanReader.ReadInt32();    // Mimics v22/v23 = sub_86F660

		DataSize = (entryCount > 0) ? dataBufferOffset / entryCount : 0;
		//Console.WriteLine($"[Load] Header: EntryCount={entryCount}, PoolCount={poolCount}, DataBufferOffset={dataBufferOffset}, DataSizePerEntry={DataSize}");

		_offsets = new Dictionary<long, int>(entryCount);
		//Console.WriteLine($"[Load] Reading {entryCount} offsets...");

		for (int i = 0; i < entryCount; i++)
		{
			//Dictionary<string,object> dict = new Dictionary<string, object>();
			long key = spanReader.ReadInt64(); // Mimics sub_86F590
			long Id = key;

			_offsets.Add(key, i);
			//Console.WriteLine($"  Offset[{i}]: Key={key}"); // Uncomment for detailed debug
		}
		//Console.WriteLine($"[Load] Finished reading offsets. Current reader position: {spanReader.Position}");

		int positionAfterOffsets = spanReader.Position; // This is the "Position" in the decompiled code

		spanReader.Position = positionAfterOffsets + dataBufferOffset;

		this.LoadPools(poolCount, spanReader); // Mimics Bokura_Table_ZLoader__LoadPools call
		//Console.WriteLine($"[Load] Finished loading pools. Current reader position: {spanReader.Position}, going back to {positionAfterOffsets}");

		Dictionary<long,Dictionary<string,object>> datas = new Dictionary<long,Dictionary<string,object>>();

		//Console.WriteLine("[Load] Iterating through rows and invoking callbacks...");
		try
		{
			foreach (var kvp in _offsets)
			{
				spanReader.Position = positionAfterOffsets + (kvp.Value * DataSize);
				Dictionary<string, object> item = new Dictionary<string, object>();
				FieldParser parser = new FieldParser(this);

				foreach (PropertyDefinition prop in parseType.Properties)
				{
					parser.ParseField(item, prop);
				}

				//Console.WriteLine(JsonConvert.SerializeObject(item));

				datas[kvp.Key] = item;

				//break;
			}

			//Console.WriteLine($"[Load] Finished parsing {string.Join("", parseType.Name.SkipLast(4))}.");
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		return datas;
	}
}