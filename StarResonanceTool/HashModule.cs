// COPYRIGHT 2025 PotRooms

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarResonanceTool;

internal class HashModule
{
	public static void Test()
	{
		string name = "AvatarShowTable.ctb";

		Console.WriteLine(Hash33(name));
		//Console.WriteLine(Hash33Lower(name));
	}

	public static uint Hash33Lower(ReadOnlySpan<char> value)
	{
		uint hash = 5381;

		foreach (char c in value)
		{
			char lowerChar = char.ToLower(c);
			hash = (hash * 33) + lowerChar;
		}

		return hash;
	}

	public static uint Hash33(ReadOnlySpan<char> value)
	{
		uint hash = 5381;

		foreach (char c in value)
		{
			hash = (hash * 33) + c;
		}

		return hash;
	}

}
