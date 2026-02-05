// COPYRIGHT 2025 PotRooms

namespace ProtoDescDumper.App;

public interface IProtoDumpService
{
	int Run(byte[] pbBytes, string outputDir);
}
