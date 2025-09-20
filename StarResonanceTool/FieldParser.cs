// COPYRIGHT 2025 PotRooms

using Mono.Cecil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarResonanceTool;

internal class FieldParser
{
	private Bokura_Table_ZLoader_o loader;
	private ZMemory_SpanReader_o reader;
	private int totalForRow;

	public FieldParser(Bokura_Table_ZLoader_o _loader)
	{
		this.loader = _loader;
		this.reader = loader.spanReader;
	}

	public void ParseField(Dictionary<string, object> instance, PropertyDefinition prop)
	{
		if (!prop.GetMethod!.IsPublic || prop.Name == "Key")
			return;
		if (this.totalForRow >= loader.DataSize)
		{
			//Console.WriteLine($"[WARN] Row tried to exceed it's {loader.DataSize} size, not reading the field");
			return;
		}
		object value = new object();
		//Console.WriteLine(reader.Position);
		switch (prop.PropertyType.FullName)
		{
			case "System.Int32":
				value = reader.ReadInt32();
				totalForRow += 4;
				break;
			case "System.Int64":
				value = reader.ReadInt64();
				totalForRow += 8;
				break;
			case "System.String":
				value = reader.ReadString(loader);
				totalForRow += 4;
				break;
			case "System.Boolean":
				value = reader.ReadBool();
				totalForRow += 1;
				break;
			case "System.Single":
				value = reader.ReadFloat();
				totalForRow += 4;
				break;
			case "UnityEngine.Vector3":
				Dictionary<string,float> values = new Dictionary<string,float>();
				values["x"] = reader.ReadFloat();
				values["y"] = reader.ReadFloat();
				values["z"] = reader.ReadFloat();
				value = values;
				totalForRow += 4*3;
				break;
			case "UnityEngine.Vector2":
				Dictionary<string, float> valuesv2 = new Dictionary<string, float>();
				valuesv2["x"] = reader.ReadFloat();
				valuesv2["y"] = reader.ReadFloat();
				value = valuesv2;
				totalForRow += 4*2;
				break;

			// idk how to parse them correctly, expect errors out the ass

			case "Bokura.Table.Int32Array":
				value = reader.ReadIntArray(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.Int64Array":
				value = reader.ReadInt64Array(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.StringArray":
				value = reader.ReadStringArray(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.NumberArray":
				value = reader.ReadNumberArray(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.Vector2Array":
				value = reader.ReadVector2Array(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.Vector3Array":
				value = reader.ReadVector3Array(loader);
				totalForRow += 4;
				break;
			case "Bokura.Table.MLStringArray":
				value = reader.ReadMLStringArray(loader);
				totalForRow += 4; // ???
				break;
			case "Bokura.Table.Int32Table":
				value = reader.ReadInt32Table(loader);
				totalForRow += 4; // ???
				break;
			case "Bokura.Table.StringTable":
				value = reader.ReadStringTable(loader);
				totalForRow += 4; // ???
				break;
			case "Bokura.Table.NumberTable":
				value = reader.ReadNumberTable(loader);
				totalForRow += 4; // ???
				break;

			// special ones ig?
			case "Bokura.Table.StringTripleArray":
				value = reader.ReadStringTripleArray(loader);
				totalForRow += 4; // ???
				break;


			default:
				throw new NotImplementedException($"Type {prop.PropertyType.FullName} not implemented yet");
		}
		//Console.WriteLine($"{prop.Name} {JsonConvert.SerializeObject(value)}");
		instance.Add(prop.Name, value);
	}
}
