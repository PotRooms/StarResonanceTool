// COPYRIGHT 2025 PotRooms

using Mono.Cecil;
using System;
using System.Collections.Generic;

public sealed class BokuraTableLoader
{
	private readonly TypeDefinition _parseType;
	private ZMemory_SpanReader_o _reader = null!;

	public int RowSize { get; private set; }
	public ReadOnlyMemory<byte> Memory { get; private set; }

	private readonly Dictionary<long, int> _rowIndex = new();
	private readonly Dictionary<int, IPool> _pools;

	public IntArrayPool IntArrayPool { get; } = new();
	public Int64ArrayPool Int64ArrayPool { get; } = new();
	public NumberArrayPool NumberArrayPool { get; } = new();
	public IntIntMapPool MapIntIntPool { get; } = new();
	public MapIntNumberPool MapIntNumberPool { get; } = new();
	public StringPool StringPool { get; } = new();
	public Vector2ArrayPool Vector2ArrayPool { get; } = new();
	public Vector3ArrayPool Vector3ArrayPool { get; } = new();
	public MapIntVector2Pool MapIntVector2Pool { get; } = new();
	public MapIntVector3Pool MapIntVector3Pool { get; } = new();

	public BokuraTableLoader(TypeDefinition parseType)
	{
		_parseType = parseType;
		_pools = PoolRegistry.Create(this);
	}

	public Dictionary<long, Dictionary<string, object>> Load(byte[] data)
	{
		Memory = data;
		_reader = new ZMemory_SpanReader_o(data);

		_reader.ReadInt64();
		int rowCount = _reader.ReadInt32();
		int poolCount = _reader.ReadInt32();
		int dataOffset = _reader.ReadInt32();

		RowSize = rowCount > 0 ? dataOffset / rowCount : 0;

		for (int i = 0; i < rowCount; i++)
			_rowIndex[_reader.ReadInt64()] = i;

		int rowBase = _reader.Position;
		_reader.Position = rowBase + dataOffset;

		LoadPools(poolCount);

		var result = new Dictionary<long, Dictionary<string, object>>(rowCount);

		foreach (var (key, index) in _rowIndex)
		{
			_reader.Position = rowBase + index * RowSize;

			var parser = new FieldParser(this, _reader);
			var row = new Dictionary<string, object>(_parseType.Properties.Count);

			foreach (var prop in _parseType.Properties)
				parser.Parse(row, prop);

			result[key] = row;
		}

		return result;
	}

	private void LoadPools(int count)
	{
		for (int i = 0; i < count; i++)
		{
			int type = _reader.ReadInt32();
			int length = _reader.ReadInt32();
			var data = _reader.ReadBytesMemory(length);

			if (type != 0 && _pools.TryGetValue(type, out var pool))
				pool.Populate(data);
		}
	}
}
