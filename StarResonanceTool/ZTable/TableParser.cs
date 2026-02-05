// COPYRIGHT 2025 PotRooms

using Newtonsoft.Json;
using StarResonanceTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;
using static StarResonanceTool.PkgEntryReader.Program;

internal class TableParser
{
	private static readonly string outDir = "ZTable";

	public void ParseFromName(string name, TypeDefinition targetType)
	{
		uint hash = HashModule.Hash33(name + ".ctb");

		if (!MainApp.entries.ContainsKey(hash))
		{
			Console.WriteLine($"[ERR] Hash {hash} for \"{name}\" doesn't exist, abort.");
			return;
		}

		if (!Directory.Exists(outDir))
			Directory.CreateDirectory(outDir);

		PkgEntry pkgEntry = MainApp.entries[hash];
		byte[] data = ReadFromEntry(pkgEntry);

		//File.WriteAllBytes($"{name}.bin", data);

		Console.WriteLine($"Parsing data for '{name}' ({data.Length} bytes)...");

		BokuraTableLoader loader = new BokuraTableLoader(targetType);
		Dictionary<long, Dictionary<string, object>> datas = loader.Load(data);

		File.WriteAllText(Path.Combine(outDir, $"{name}.json"), JsonConvert.SerializeObject(datas, Formatting.Indented));

		Console.WriteLine($"Parsing complete for '{name}'.");

		//Console.WriteLine(JsonConvert.SerializeObject(loader._offsets, Formatting.Indented));
	}
}