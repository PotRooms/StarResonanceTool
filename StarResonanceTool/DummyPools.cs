// COPYRIGHT 2025 PotRooms

using System;
using System.Collections.Generic;

// Placeholder for various pool types
public class IntArrayPool
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into int[] */ 
		//Console.WriteLine("[MemoryPool] Populating IntArrayPool"); 
	}
}

public class Int64ArrayPool
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into long[] */ 
		//Console.WriteLine("[MemoryPool] Populating Int64ArrayPool");
	}
}

public class NumberArrayPool // Assuming float or double
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate()
	{ /* Deserialize _memory into float[] or double[] */
		//Console.WriteLine("[MemoryPool] Populating NumberArrayPool");
	}
}

public class MapIntIntPool // Dictionary<int, int>
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Dictionary<int, int> */ 
		//Console.WriteLine("[MemoryPool] Populating MapIntIntPool");
	}
}

public class MapIntNumberPool // Dictionary<int, float/double>
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Dictionary<int, float/double> */ 
		//Console.WriteLine("[MemoryPool] Populating MapIntNumberPool");
	}
}

public class StringPool
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into string[] */
		//Console.WriteLine("[MemoryPool] [MemoryPool] Populating StringPool");
	}
}

public struct Vector2 { public float x, y; }
public class Vector2ArrayPool
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Vector2[] */
		//Console.WriteLine("[MemoryPool] Populating Vector2ArrayPool");
	}
}

public struct Vector3 { public float x, y, z; }
public class Vector3ArrayPool
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Vector3[] */
		//Console.WriteLine("[MemoryPool] Populating Vector3ArrayPool");
	}
}

public class MapIntVector2Pool // Dictionary<int, Vector2>
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Dictionary<int, Vector2> */
		//Console.WriteLine("[MemoryPool] Populating MapIntVector2Pool");
	}
}

public class MapIntVector3Pool // Dictionary<int, Vector3>
{
	public ReadOnlyMemory<byte> _memory;
	public void Populate() { /* Deserialize _memory into Dictionary<int, Vector3> */ 
		//Console.WriteLine("[MemoryPool] Populating MapIntVector3Pool");
	}
}