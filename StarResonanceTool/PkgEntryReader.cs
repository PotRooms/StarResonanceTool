// COPYRIGHT 2025 Hiro420

using Newtonsoft.Json;
// COPYRIGHT 2025 PotRooms

using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;

namespace StarResonanceTool.PkgEntryReader;

public class Program
{
	private static Dictionary<uint, string> _patchFileCache = new Dictionary<uint, string>();

	private static void LoadPatchFileCache()
	{
		string patchDir = Path.Combine(MainApp.containerPath, "Patch");
		if (Directory.Exists(patchDir))
		{
			foreach (var file in Directory.GetFiles(patchDir))
			{
				var fileName = Path.GetFileNameWithoutExtension(file);
				if (uint.TryParse(fileName, out uint key) && !_patchFileCache.ContainsKey(key))
				{
					_patchFileCache[key] = file;
				}
			}
		}
	}

	public struct PkgEntry
	{
		public uint Key;
		public int Offset;
		public ushort Index;
		public int Length;
		public byte Type;
	}

	public static Dictionary<uint, PkgEntry> InitPkg(string filePath)
	{
		var entries = new Dictionary<uint, PkgEntry>();

		using (var reader = new BinaryReader(File.OpenRead(filePath)))
		{
			int unk1 = reader.ReadInt32();
			int unk2 = reader.ReadInt32();
			int unk3 = reader.ReadInt32();
			long unk4 = reader.ReadInt64();
			uint unk5 = reader.ReadUInt32();
			short unk6 = reader.ReadInt16();

			Console.WriteLine($"unk1={unk1}, unk2={unk2}, unk3={unk3}, unk4={unk4}, unk5={unk5}, unk6={unk6}");

			reader.BaseStream.Seek(16 * unk6, SeekOrigin.Current);

			int numEntries = reader.ReadInt32();
			Console.WriteLine($"num_entries: {numEntries}");

			for (int i = 0; i < numEntries; i++)
			{
				uint key = reader.ReadUInt32();
				byte type = reader.ReadByte();
				ushort index = reader.ReadUInt16();
				int offset = reader.ReadInt32();
				int length = reader.ReadInt32();

				entries[key] = new PkgEntry
				{
					Key = key,
					Offset = offset,
					Index = index,
					Length = length,
					Type = type
				};
			}

			int numEntries2 = reader.ReadInt32();
			Console.WriteLine($"num_entries2: {numEntries2}");

			for (int i = 0; i < numEntries2; i++)
			{
				uint key = reader.ReadUInt32();
				byte type = reader.ReadByte();
				ushort index = reader.ReadUInt16();
				int offset = reader.ReadInt32();
				int length = reader.ReadInt32();

				entries[key] = new PkgEntry
				{
					Key = key,
					Offset = offset,
					Index = index,
					Length = length,
					Type = type
				};
			}
		}

		LoadPatchFileCache();

		return entries;
	}

	public static byte[] ReadFromEntry(PkgEntry entry)
	{
		if (_patchFileCache != null && _patchFileCache.TryGetValue(entry.Key, out var patchFile))
		{
			Console.WriteLine($"Read {entry.Key} from patch file: {patchFile}");
			return File.ReadAllBytes(patchFile);
		}

		string containerPath = Path.Combine(MainApp.containerPath, $"m{entry.Index}.pkg");

		if (!File.Exists(containerPath))
			return [];

		using (var reader = new BinaryReader(File.OpenRead(containerPath)))
		{
			reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
			byte[] data = reader.ReadBytes(entry.Length);
			return data;
		}
	}

	public static bool StartsWith(byte[] data, string magic)
	{
		var magicBytes = System.Text.Encoding.ASCII.GetBytes(magic);
		return StartsWith(data, magicBytes);
	}

	public static bool StartsWith(byte[] data, byte[] magic)
	{
		if (data.Length < magic.Length) return false;
		for (int i = 0; i < magic.Length; i++)
		{
			if (data[i] != magic[i])
				return false;
		}
		return true;
	}
}