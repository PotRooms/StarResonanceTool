// COPYRIGHT 2025 PotRooms

using System;
using System.IO;
using System.Text;

namespace StarResonanceTool;

internal class LuaModule
{
	private static readonly string outPath = "Lua";

	public static void OutputLua(string baseModule, byte[] data, uint hash)
	{
		const int offset = 0x22;

		using var reader = new BinaryReader(new MemoryStream(data));

		if (reader.BaseStream.Length <= offset)
		{
			Console.WriteLine($"[WARN] Lua with hash {hash} is too small to contain path.");
			return;
		}

		reader.BaseStream.Seek(offset, SeekOrigin.Begin);
		byte len = reader.ReadByte();

		if (len == 0)
		{
			Console.WriteLine($"[WARN] Lua with hash {hash} has no embedded path.");
			return;
		}

		if (reader.BaseStream.Length < offset + 1 + len)
		{
			Console.WriteLine($"[WARN] Lua with hash {hash} is malformed or truncated.");
			return;
		}

		byte[] pathBytes = reader.ReadBytes(len - 1);
		string readablePath = Encoding.UTF8.GetString(pathBytes);

		string relativePath = readablePath.Contains("Standalone/container/lua/")
				? readablePath.Split("Standalone/container/lua/")[1]
				: readablePath;

		string outputPath = Path.Combine(baseModule, outPath, relativePath+"c");

		// Ensure target directory exists
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

		using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
		fs.Write(data);
		if (fs.Length > 0x5)
		{
			fs.Position = 0x5;
			fs.WriteByte(0);
		}

		Console.WriteLine($"Extracted {hash} to {outputPath}");
	}
}
