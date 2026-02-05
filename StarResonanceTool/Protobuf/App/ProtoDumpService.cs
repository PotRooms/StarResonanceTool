// COPYRIGHT 2025 PotRooms

using ProtoBuf;
using google.protobuf;
using ProtoDescDumper.Core.Abstractions;

namespace ProtoDescDumper.App;

public sealed class ProtoDumpService(
	IFileSystem fileSystem,
	ILogger logger,
	IProtoDescriptorAnalyzer analyzer,
	IProtoDescriptorFormatter formatter) : IProtoDumpService
{
	private readonly IFileSystem fileSystem = fileSystem;
	private readonly ILogger logger = logger;
	private readonly IProtoDescriptorAnalyzer analyzer = analyzer;
	private readonly IProtoDescriptorFormatter formatter = formatter;

	public int Run(byte[] pbBytes, string outputDir)
	{
		try
		{
			logger.Info($"Loading FileDescriptorSet from byte array...");
			using var stream = new MemoryStream(pbBytes);
			FileDescriptorSet set = Serializer.Deserialize<FileDescriptorSet>(stream);

			if (!analyzer.Analyze(set.file))
			{
				logger.Error("Dump failed. Not all dependencies and types were found.");
				return -1;
			}

			logger.Info("Analysis succeeded. Dumping proto files...");

			foreach (var buffer in set.file)
			{
				var packageParts = (buffer.package ?? string.Empty)
					.Split('.', StringSplitOptions.RemoveEmptyEntries);

				// dirty hack to remove double google.protobuf from the path
				if (packageParts.Length >= 2 && packageParts[0] == "google" && packageParts[1] == "protobuf")
					packageParts = packageParts[2..];

				var outDir = Path.Combine([outputDir, .. packageParts]);
				var outputFile = Path.Combine(outDir, buffer.name);

				fileSystem.EnsureDirectory(Path.GetDirectoryName(outputFile)!);
				var protoText = formatter.FormatFile(buffer);

				logger.Info($"Outputting proto to \"{outputFile}\"");
				fileSystem.WriteAllText(outputFile, protoText);
			}

			logger.Info("Dump completed successfully.");
			return 0;
		}
		catch (Exception ex)
		{
			logger.Error("[FATAL] Failed", ex);
			return -1;
		}
	}
}
