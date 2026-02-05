// COPYRIGHT 2025 PotRooms

using System;
using System.Collections.Generic;

public interface IPool
{
	void Populate(ReadOnlyMemory<byte> data);
}

public static class PoolRegistry
{
	public static Dictionary<int, IPool> Create(BokuraTableLoader loader) => new()
	{
		{ 1,  loader.IntArrayPool },
		{ 2,  loader.Int64ArrayPool },
		{ 3,  loader.NumberArrayPool },
		{ 4,  loader.MapIntIntPool },
		{ 5,  loader.MapIntNumberPool },
		{ 6,  loader.StringPool },
		{ 7,  loader.Vector2ArrayPool },
		{ 8,  loader.Vector3ArrayPool },
		{ 9,  loader.MapIntVector2Pool },
		{ 10, loader.MapIntVector3Pool },
	};
}

public sealed class IntArrayPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class Int64ArrayPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class NumberArrayPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class IntIntMapPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class MapIntNumberPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class StringPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class Vector2ArrayPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class Vector3ArrayPool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class MapIntVector2Pool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}

public sealed class MapIntVector3Pool : IPool
{
	public ReadOnlyMemory<byte> Memory { get; private set; }
	public void Populate(ReadOnlyMemory<byte> data) => Memory = data;
}
