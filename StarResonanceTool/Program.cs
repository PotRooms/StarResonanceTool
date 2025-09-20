// COPYRIGHT 2025 PotRooms

using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.IO;
using static StarResonanceTool.PkgEntryReader.Program;
using static StarResonanceTool.ProtoModule;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Google.Protobuf.Reflection;

namespace StarResonanceTool;

internal class MainApp
{
	public static Dictionary<uint, PkgEntry> entries = new Dictionary<uint, PkgEntry>();
	public static readonly string filePath = @"E:\StarLauncher\game\publish_apjcbt_1.0\game_mini\Star_Data\StreamingAssets\container\meta.pkg";
	public static readonly string basePath = @"E:\StarLauncher\game\publish_apjcbt_1.0\game_mini\Star_Data\StreamingAssets\container";
	public static readonly string dummyDllPath = @"C:\Users\hiro\Documents\Il2CppDumper\Il2CppDumper\bin\Debug\net8.0\sr2\DummyDll";

	public static void Main(string[] args)
	{
		entries = PkgEntryReader.Program.InitPkg(filePath);
		var settings = new JsonSerializerSettings() { Converters = { new StringEnumConverter() } };
		//File.WriteAllText("entries.json", JsonConvert.SerializeObject(entries, Formatting.Indented, settings));

		DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
		resolver.AddSearchDirectory(Directory.GetParent(dummyDllPath)?.FullName ?? string.Empty);
		ReaderParameters readerParams = new ReaderParameters { AssemblyResolver = resolver };
		ModuleDefinition metaData = AssemblyDefinition.ReadAssembly(Path.Combine(dummyDllPath, "Panda.Table.dll"), readerParams).MainModule;
		TypeDefinition LoaderType = metaData.GetType("Panda.TableInitUtility").NestedTypes.First(t => t.Name == "<>c");

		foreach (MethodDefinition method in LoaderType.Methods)
		{
			if (!method.IsAssembly)
				continue;
			TableParser parser = new TableParser();
			string tableName = string.Join("", method.ReturnType.Name.SkipLast(4));
			TypeDefinition targetType = method.ReturnType.Resolve();
			//if (targetType.Name == "AwardTableBase")
			//	continue;
			parser.ParseFromName(tableName, targetType);
		}

		Directory.CreateDirectory(Path.Combine(basePath, "bundles"));
		Directory.CreateDirectory(Path.Combine(basePath, "luas"));
		Directory.CreateDirectory(Path.Combine(basePath, "unk"));

		Console.WriteLine("Generating the rest of output...");

		foreach (var kv in entries)
		{
			uint key = kv.Key;
			PkgEntry entry = kv.Value;

			if (key != 1952927057 &&
				key != 2697780389 &&
				key != 621018379
			) continue;

			byte[] data = ReadFromEntry(entry);

			string outputPath;
			if (StartsWith(data, "UnityFS")) // assetbundles, those are NOT in m0.pkg
			{
				outputPath = Path.Combine(basePath, "bundles", $"{key}.ab");
				continue; // we don't need that, comment out if you do
			}
			else if (StartsWith(data, new byte[] { 0x1B, 0x4C, 0x75, 0x61 })) // Lua
			{
				outputPath = Path.Combine(basePath, "luas", $"{key}.luac");
			}
			else // protobufs or tables etc
			{
				outputPath = Path.Combine(basePath, "unk", $"{key}.bin");

				if (ContainsString(data, "proto3") || ContainsString(data, "proto2"))
					DumpProtoFromBin(data);
			}

			if (File.Exists(outputPath))
				continue;

			File.WriteAllBytes(outputPath, data);
			Console.WriteLine($"Extracted {key} to {outputPath}");
		}

		// if you already have them split
		LuaModule.RenameLuas(basePath);
	}

	private static void DumpProtoFromBin(byte[] data)
	{
		FileDescriptorSet set = TryParseFileDescriptorSet(data)
							 ?? TryParseFileDescriptorSet(TryGunzip(data))
							 ?? TryScanForEmbeddedFds(data)
							 ?? TryScanForEmbeddedFds(TryGunzip(data));

		if (set == null)
		{
			Console.Error.WriteLine("Could not parse a FileDescriptorSet from the input using any strategy.");
			return;
		}

		var writer = new ProtoSchemaWriter();
		var written = writer.WriteFiles(set, "proto");
	}

	private static bool ContainsString(byte[] data, string text)
	{
		var str = System.Text.Encoding.UTF8.GetString(data);
		return str.Contains(text, StringComparison.Ordinal);
	}

	public static bool EndsWith(byte[] source, byte[] suffix)
	{
		// If either array is null, or if the suffix is longer than the source,
		// it cannot end with the suffix.
		if (source == null || suffix == null || suffix.Length > source.Length)
		{
			return false;
		}

		// If the suffix is empty, it's considered to end with it.
		if (suffix.Length == 0)
		{
			return true;
		}

		// Compare the last 'suffix.Length' bytes of the source array
		// with the bytes in the suffix array.
		for (int i = 0; i < suffix.Length; i++)
		{
			if (source[source.Length - suffix.Length + i] != suffix[i])
			{
				return false; // Mismatch found
			}
		}

		return true; // All bytes matched
	}
}