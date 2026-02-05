// COPYRIGHT 2025 PotRooms

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using google.protobuf;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace ProtoDescDumper.Core;

public sealed partial class ProtoDescriptorService
{
	private static readonly IReadOnlyDictionary<string, FileDescriptor> WellKnownByName = new Dictionary<string, FileDescriptor>(StringComparer.Ordinal)
	{
		{ "google/protobuf/descriptor.proto", DescriptorReflection.Descriptor },
		{ "google/protobuf/any.proto", AnyReflection.Descriptor },
		{ "google/protobuf/timestamp.proto", TimestampReflection.Descriptor },
		{ "google/protobuf/duration.proto", DurationReflection.Descriptor },
		{ "google/protobuf/wrappers.proto", WrappersReflection.Descriptor },
		{ "google/protobuf/struct.proto", StructReflection.Descriptor },
		{ "google/protobuf/empty.proto", EmptyReflection.Descriptor },
	};

	private static google.protobuf.FileDescriptorProto Convert(FileDescriptor fd)
	{
		var bytes = fd.ToProto().ToByteArray();
		using var ms = new MemoryStream(bytes);
		return Serializer.Deserialize<google.protobuf.FileDescriptorProto>(ms);
	}

	private static void EnsureGoogleWellKnownProtos(List<google.protobuf.FileDescriptorProto> protos)
	{
		var present = new HashSet<string>(protos.Select(p => p.name), StringComparer.Ordinal);

		var needed = new HashSet<string>(StringComparer.Ordinal);
		foreach (var p in protos)
		{
			foreach (var dep in p.dependency)
			{
				if (dep.StartsWith("google/protobuf/", StringComparison.Ordinal) && !present.Contains(dep))
					needed.Add(dep);
			}
		}

		if (needed.Count == 0) return;

		var queue = new Queue<string>(needed);
		while (queue.Count > 0)
		{
			var name = queue.Dequeue();
			if (present.Contains(name)) continue;

			if (!WellKnownByName.TryGetValue(name, out var fd))
			{
				continue;
			}

			var converted = Convert(fd);
			protos.Add(converted);
			present.Add(converted.name);

			foreach (var dep in converted.dependency)
			{
				if (dep.StartsWith("google/protobuf/", StringComparison.Ordinal) && !present.Contains(dep))
					queue.Enqueue(dep);
			}
		}
	}
}
