// COPYRIGHT 2025 PotRooms

namespace ProtoDescDumper.App;

public sealed class LocalFileSystem : IFileSystem
{
	public Stream OpenRead(string path) => File.OpenRead(path);

	public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

	public void EnsureDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}
}
