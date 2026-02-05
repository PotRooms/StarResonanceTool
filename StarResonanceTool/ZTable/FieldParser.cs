// COPYRIGHT 2025 PotRooms

using Mono.Cecil;
using System;
using System.Collections.Generic;

internal sealed class FieldParser
{
	private readonly BokuraTableLoader _loader;
	private readonly ZMemory_SpanReader_o _reader;
	private int _bytesRead;

	public FieldParser(BokuraTableLoader loader, ZMemory_SpanReader_o reader)
	{
		_loader = loader;
		_reader = reader;
	}

	public void Parse(Dictionary<string, object> row, PropertyDefinition prop)
	{
		if (!prop.GetMethod!.IsPublic || prop.Name == "Key")
			return;

		if (_bytesRead >= _loader.RowSize)
			return;

		object value = prop.PropertyType.FullName switch
		{
			"System.Int32" => Read(4, _reader.ReadInt32),
			"System.Int64" => Read(8, _reader.ReadInt64),
			"System.Boolean" => Read(1, _reader.ReadBool),
			"System.Single" => Read(4, _reader.ReadFloat),
			"System.String" => Read(4, () => _reader.ReadString(_loader)),

			// Vectors
			"UnityEngine.Vector2" => Read(8, () => new Dictionary<string, float>
			{
				["x"] = _reader.ReadFloat(),
				["y"] = _reader.ReadFloat(),
			}),
			"UnityEngine.Vector3" => Read(12, () => new Dictionary<string, float>
			{
				["x"] = _reader.ReadFloat(),
				["y"] = _reader.ReadFloat(),
				["z"] = _reader.ReadFloat(),
			}),

			// Special types
			"Bokura.Table.Int32Array" => Read(4, () => _reader.ReadIntArray(_loader)),
			"Bokura.Table.Int64Array" => Read(4, () => _reader.ReadInt64Array(_loader)),
			"Bokura.Table.NumberArray" => Read(4, () => _reader.ReadNumberArray(_loader)),
			"Bokura.Table.StringArray" => Read(4, () => _reader.ReadStringArray(_loader)),
			"Bokura.Table.Vector2Array" => Read(4, () => _reader.ReadVector2Array(_loader)),
			"Bokura.Table.Vector3Array" => Read(4, () => _reader.ReadVector3Array(_loader)),
			"Bokura.Table.MLStringArray" => Read(4, () => _reader.ReadMLStringArray(_loader)),

			"Bokura.Table.Int32Table" => Read(4, () => _reader.ReadInt32Table(_loader)),
			"Bokura.Table.StringTable" => Read(4, () => _reader.ReadStringTable(_loader)),
			"Bokura.Table.NumberTable" => Read(4, () => _reader.ReadNumberTable(_loader)),
			"Bokura.Table.MLStringTable" => Read(4, () => _reader.ReadMLStringTable(_loader)),
			"Bokura.Table.StringTripleArray" => Read(4, () => _reader.ReadStringTripleArray(_loader)),
			"Bokura.Table.KVIntInt" => Read(4, () => _reader.ReadKVIntInt(_loader)),

			_ => Read(4, () => _reader.ReadTable(prop.PropertyType.FullName, _loader))
		};

		row[prop.Name] = value;
	}

	private T Read<T>(int size, Func<T> fn)
	{
		_bytesRead += size;
		return fn();
	}
}
