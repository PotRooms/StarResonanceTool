// COPYRIGHT 2025 PotRooms

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static StarResonanceTool.PkgEntryReader.Program;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using google.protobuf;
using System.Text;
using ProtoDescDumper.App;
using ProtoDescDumper.Core;

namespace StarResonanceTool;

internal class Config
{
	public string PkgPath { get; set; } = String.Empty;
	public string OutputPath { get; set; } = String.Empty;
	public string DummyDllPath { get; set; } = String.Empty;
	public bool ExtractAssetBundles { get; set; } = false;
	public bool ProcessAllEntries { get; set; } = false;
}

internal class MainApp
{
	public static Dictionary<uint, PkgEntry> entries = new Dictionary<uint, PkgEntry>();
	public static string containerPath = string.Empty;

	public static string defaultlang = "english"; // can change to chinese

	public static KeyValuePair<int, int>[] Indexes = [];
	public static string[] AllLocalizationStrings = [];
	public static Dictionary<int, int> FlowConflict = [];
	public static Dictionary<int, int> ManualConflict = [];

	public static void Main(string[] args)
	{
		// Parse command line arguments
		var config = ParseArguments(args);
		if (config == null)
		{
			PrintUsage();
			return;
		}

		containerPath = Path.GetDirectoryName(config.PkgPath)!;
		string filePath = config.PkgPath;
		string basePath = config.OutputPath;
		string dummyDllPath = config.DummyDllPath;

		// Display configuration
		Console.WriteLine("StarResonanceTool Configuration:");
		Console.WriteLine($"  PKG File: {filePath}");
		Console.WriteLine($"  Output Directory: {basePath}");
		Console.WriteLine($"  DummyDll Directory: {dummyDllPath}");
		Console.WriteLine($"  Extract Asset Bundles: {config.ExtractAssetBundles}");
		Console.WriteLine($"  Process All Entries: {config.ProcessAllEntries}");
		Console.WriteLine();

		// Validate paths
		if (!File.Exists(filePath))
		{
			Console.Error.WriteLine($"Error: PKG file not found: {filePath}");
			return;
		}

		if (!Directory.Exists(dummyDllPath))
		{
			Console.Error.WriteLine($"Error: DummyDll directory not found: {dummyDllPath}");
			return;
		}

		// Create output directory if it doesn't exist
		if (!Directory.Exists(basePath))
		{
			Directory.CreateDirectory(basePath);
			Console.WriteLine($"Created output directory: {basePath}");
		}

		entries = PkgEntryReader.Program.InitPkg(filePath);
		var settings = new JsonSerializerSettings() { Converters = { new StringEnumConverter() } };
		//File.WriteAllText("entries.json", JsonConvert.SerializeObject(entries, Formatting.Indented, settings));

		DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
		resolver.AddSearchDirectory(Directory.GetParent(dummyDllPath)?.FullName ?? string.Empty);
		ReaderParameters readerParams = new ReaderParameters { AssemblyResolver = resolver };
		ModuleDefinition metaData = AssemblyDefinition.ReadAssembly(Path.Combine(dummyDllPath, "Panda.Table.dll"), readerParams).MainModule;
		TypeDefinition LoaderType = metaData.GetType("Panda.TableInitUtility").NestedTypes.First(t => t.Name == "<>c");

		byte[] cnBytes = ReadFromEntry(entries[HashModule.Hash33($"{defaultlang}.bytes")]);
		LoadLocalizationFromStream(new MemoryStream(cnBytes));

		foreach (MethodDefinition method in LoaderType.Methods)
		{
			if (!method.IsAssembly)
				continue;
			//	continue;
			TableParser parser = new TableParser();
			string tableName = string.Join("", method.ReturnType.Name.SkipLast(4));
			TypeDefinition targetType = method.ReturnType.Resolve();
			//if (tableName != "MonsterTable")
			//	continue;
			parser.ParseFromName(tableName, targetType);
		}

		Directory.CreateDirectory(Path.Combine(basePath, "Bundles"));
		Directory.CreateDirectory(Path.Combine(basePath, "Lua"));
		Directory.CreateDirectory(Path.Combine(basePath, "Unk"));
		// Apply filtering based on configuration

		if (!config.ProcessAllEntries)
			return;

		Console.WriteLine("Generating the rest of output...");

		foreach (var kv in entries)
		{
			uint key = kv.Key;
			PkgEntry entry = kv.Value;

			byte[] data = ReadFromEntry(entry);

			string outputPath;
			if (StartsWith(data, "UnityFS")) // assetbundles, those are NOT in m0.pkg
			{
				outputPath = Path.Combine(basePath, "Bundles", $"{key}.ab");
				if (!config.ExtractAssetBundles)
					continue; // skip asset bundles unless explicitly requested
			}
			else if (StartsWith(data, new byte[] { 0x1B, 0x4C, 0x75, 0x61 })) // Lua
			{
				LuaModule.OutputLua(basePath, data, key);
				continue;
			}
			else // protobufs or tables etc
			{
				outputPath = Path.Combine(basePath, "Unk", $"{key}.bin");

				if (ContainsString(data, "proto3") || ContainsString(data, "proto2"))
					DumpProtoFromBin(data);
			}

			if (File.Exists(outputPath))
				continue;

			File.WriteAllBytes(outputPath, data);
			Console.WriteLine($"Extracted {key} to {outputPath}");
		}
	}

	public static void LoadLocalizationFromStream(Stream stream)
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

		// Read number of index pairs
		int indexCount = reader.ReadInt32();
		Indexes = new KeyValuePair<int, int>[indexCount];

		for (int i = 0; i < indexCount; i++)
		{
			int key = reader.ReadInt32();
			int value = reader.ReadInt32();
			Indexes[i] = new KeyValuePair<int, int>(key, value);
		}

		// Read localization strings
		int stringCount = reader.ReadInt32();
		AllLocalizationStrings = new string[stringCount];

		for (int i = 0; i < stringCount; i++)
		{
			AllLocalizationStrings[i] = reader.ReadString();
		}

		// Read flowConflict dictionary
		int flowConflictCount = reader.ReadInt32();
		if (flowConflictCount > 0)
		{
			FlowConflict = new Dictionary<int, int>(flowConflictCount);
			for (int i = 0; i < flowConflictCount; i++)
			{
				int key = reader.ReadInt32();
				int value = reader.ReadInt32();
				FlowConflict[key] = value;
			}
		}

		// Read manualConflict dictionary
		int manualConflictCount = reader.ReadInt32();
		if (manualConflictCount > 0)
		{
			ManualConflict = new Dictionary<int, int>(manualConflictCount);
			for (int i = 0; i < manualConflictCount; i++)
			{
				int key = reader.ReadInt32();
				int value = reader.ReadInt32();
				ManualConflict[key] = value;
			}
		}
	}

	private static Config? ParseArguments(string[] args)
	{
		var config = new Config();

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i].ToLower())
			{
				case "-h":
				case "--help":
					return null;

				case "-p":
				case "--pkg":
					if (i + 1 >= args.Length)
					{
						Console.Error.WriteLine("Error: --pkg requires a file path");
						return null;
					}
					config.PkgPath = args[++i];
					break;

				case "-o":
				case "--output":
					if (i + 1 >= args.Length)
					{
						Console.Error.WriteLine("Error: --output requires a directory path");
						return null;
					}
					config.OutputPath = args[++i];
					break;

				case "-d":
				case "--dll":
					if (i + 1 >= args.Length)
					{
						Console.Error.WriteLine("Error: --dll requires a directory path");
						return null;
					}
					config.DummyDllPath = args[++i];
					break;

				case "-a":
				case "--assetbundles":
					config.ExtractAssetBundles = true;
					break;

				case "--all":
					config.ProcessAllEntries = true;
					break;

				default:
					Console.Error.WriteLine($"Error: Unknown argument '{args[i]}'");
					return null;
			}
		}

		return config;
	}

	private static void PrintUsage()
	{
		Console.WriteLine("StarResonanceTool - Extract and process game assets");
		Console.WriteLine();
		Console.WriteLine("Usage: StarResonanceTool.exe [options]");
		Console.WriteLine();
		Console.WriteLine("Options:");
		Console.WriteLine("  -h, --help              Show this help message");
		Console.WriteLine("  -p, --pkg <path>        Path to the meta.pkg file");
		Console.WriteLine("  -o, --output <path>     Output directory path");
		Console.WriteLine("  -d, --dll <path>        Path to DummyDll directory");
		Console.WriteLine("  -k, --keys <keys>       Comma-separated list of entry keys to process");
		Console.WriteLine("  -a, --assetbundles      Extract asset bundles (default: skip)");
		Console.WriteLine("  --all                   Process all entries (ignores key filtering)");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  StarResonanceTool.exe --pkg \"C:\\game\\meta.pkg\" --output \"C:\\output\"");
		Console.WriteLine("  StarResonanceTool.exe --keys \"1952927057,2697780389\" --assetbundles");
		Console.WriteLine("  StarResonanceTool.exe --all --output \"C:\\extracted\"");
	}

	private static void DumpProtoFromBin(byte[] data)
	{

		var logger = new ConsoleLogger();
		var fileSystem = new LocalFileSystem();
		var coreService = new ProtoDescriptorService([], logger);
		var app = new ProtoDumpService(fileSystem, logger, coreService, coreService);

		app.Run(data, "Proto");
	}

	private static bool ContainsString(byte[] data, string text)
	{
		var str = System.Text.Encoding.UTF8.GetString(data);
		return str.Contains(text, StringComparison.Ordinal);
	}

}