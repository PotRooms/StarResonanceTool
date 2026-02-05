// COPYRIGHT 2025 PotRooms

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public sealed class ZMemory_SpanReader_o
{
	private readonly ReadOnlyMemory<byte> _memory;
	private ReadOnlySpan<byte> Span => _memory.Span;

	public int Position { get; set; }
	public int Length => _memory.Length;

	public ZMemory_SpanReader_o(ReadOnlyMemory<byte> memory)
	{
		_memory = memory;
		Position = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long ReadInt64()
	{
		EnsureAvailable(8);
		long value = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(Position, 8));
		Position += 8;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadInt32()
	{
		EnsureAvailable(4);
		int value = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(Position, 4));
		Position += 4;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public short ReadInt16()
	{
		EnsureAvailable(2);
		short value = BinaryPrimitives.ReadInt16LittleEndian(Span.Slice(Position, 2));
		Position += 2;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ReadBool()
	{
		EnsureAvailable(1);
		byte b = Span[Position++];
		return b != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float ReadFloat()
	{
		EnsureAvailable(4);
		float value = BinaryPrimitives.ReadSingleLittleEndian(Span.Slice(Position, 4));
		Position += 4;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte ReadByte()
	{
		EnsureAvailable(1);
		return Span[Position++];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyMemory<byte> ReadBytesMemory(int length)
	{
		EnsureAvailable(length);
		var slice = _memory.Slice(Position, length);
		Position += length;
		return slice;
	}

	public byte[] ReadBytes(int length)
	{
		return ReadBytesMemory(length).ToArray();
	}

	public void Seek(int newPosition)
	{
		if ((uint)newPosition > (uint)Length)
			throw new ArgumentOutOfRangeException(nameof(newPosition), "Position out of bounds.");
		Position = newPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureAvailable(int count)
	{
		// unsigned compare is a common fast bounds pattern
		if ((uint)(Position + count) > (uint)Length)
			throw new EndOfStreamException($"Cannot read {count} bytes: End of stream.");
	}

	public struct Vector2
	{
		public float x;
		public float y;

		public Vector2(float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString() => $"({x}, {y})";
	}

	public struct Vector3
	{
		public float x;
		public float y;
		public float z;

		public Vector3(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public override string ToString() => $"({x}, {y}, {z})";
	}
}
