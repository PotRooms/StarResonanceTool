// COPYRIGHT 2025 PotRooms

using System;
using System.IO;
using System.Text;

namespace StarResonanceTool;

internal class LuaModule
{
	private static readonly string outPath = "Luac";

	public static void RenameLuas(string baseModule)
	{
		string inputPath = Path.Combine(baseModule, "luas");
		if (!Directory.Exists(inputPath))
		{
			Console.WriteLine($"[ERROR] Directory not found: {inputPath}");
			return;
		}

		foreach (string filePath in Directory.GetFiles(inputPath))
		{
			const int offset = 0x22;

			using var reader = new BinaryReader(File.OpenRead(filePath));

			if (reader.BaseStream.Length <= offset)
			{
				Console.WriteLine($"[WARN] {Path.GetFileName(filePath)} too small to contain path.");
				continue;
			}

			reader.BaseStream.Seek(offset, SeekOrigin.Begin);
			byte len = reader.ReadByte();

			if (len == 0)
			{
				Console.WriteLine($"[WARN] {Path.GetFileName(filePath)} has no embedded path.");
				continue;
			}

			if (reader.BaseStream.Length < offset + 1 + len)
			{
				Console.WriteLine($"[WARN] {Path.GetFileName(filePath)} is malformed or truncated.");
				continue;
			}

			byte[] pathBytes = reader.ReadBytes(len-1);
			string readablePath = Encoding.UTF8.GetString(pathBytes);

			string relativePath = readablePath.Contains("Standalone/container/")
				? readablePath.Split("Standalone/container/")[1]
				: readablePath;

			string outputPath = Path.Combine(baseModule, outPath, relativePath);

			// Ensure target directory exists
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

			// Copy the original file to the new location
			File.Copy(filePath, outputPath, overwrite: true);

			// Open the copied file and patch byte at offset 0x5 to 0x00
			using (var writer = new FileStream(outputPath, FileMode.Open, FileAccess.Write))
			{
				if (writer.Length > 0x5)
				{
					writer.Seek(0x5, SeekOrigin.Begin);
					writer.WriteByte(0x00);
				}
			}

			Console.WriteLine($"[INFO] Renamed and patched: {outputPath}");
		}
	}
}
