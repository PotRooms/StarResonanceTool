// COPYRIGHT 2025 PotRooms

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarResonanceTool;

public static class BinaryReaderExtensions
{
	public static List<int> ReadInt32Table(this BinaryReader br)
	{
		long curPos = br.BaseStream.Position;

		int count = 1; // first int is usually count
		var list = new List<int>(count);
		for (int i = 0; i < count; i++)
		{
			var val = br.ReadInt32();
			Console.WriteLine($"- {val}");
			list.Add(val);
		}

		//br.BaseStream.Seek(returnPos, SeekOrigin.Begin);
		return list;
	}
}

