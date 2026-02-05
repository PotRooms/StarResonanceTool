// COPYRIGHT 2025 PotRooms

namespace ProtoDescDumper.App;

public interface IFileSystem
{
	Stream OpenRead(string path);
	void WriteAllText(string path, string contents);
	void EnsureDirectory(string path);
}
