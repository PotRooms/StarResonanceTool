// COPYRIGHT 2025 PotRooms

using System.Diagnostics;
using System.Text;
using google.protobuf;
using ProtoDescDumper.Core.Services.ProtoDescriptor;

namespace ProtoDescDumper.Core;

public sealed partial class ProtoDescriptorService
{
	ProtoTypeNode GetOrCreateTypeNode(string name, FileDescriptorProto? proto = null, object? source = null)
	{
		if (!protobufTypeMap.TryGetValue(name, out var node))
		{
			node = new ProtoTypeNode()
			{
				Name = name,
				Proto = proto,
				Source = source,
				Defined = source != null
			};

			protobufTypeMap.Add(name, node);
		}
		else if (source != null)
		{
			Debug.Assert(node.Defined == false);

			node.Proto = proto;
			node.Source = source;
			node.Defined = true;
		}

		return node;
	}

	public bool Analyze(IReadOnlyList<FileDescriptorProto> input)
	{
		protobufs.Clear();
		protobufs.AddRange(input);
		messageNameStack.Clear();
		protobufMap.Clear();
		protobufTypeMap.Clear();

		EnsureGoogleWellKnownProtos(protobufs);

		foreach (var proto in protobufs)
		{
			var protoNode = new ProtoNode()
			{
				Name = proto.name,
				Proto = proto,
				Dependencies = [],
				AllPublicDependencies = [],
				Types = [],
				Defined = true
			};

			protobufMap.Add(proto.name, protoNode);

			foreach (var extension in proto.extension)
			{
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, extension.name), proto, extension));

				if (IsNamedType(extension.type) && !string.IsNullOrEmpty(extension.type_name))
					protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, extension.type_name)));

				if (!string.IsNullOrEmpty(extension.extendee))
					protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, extension.extendee)));
			}

			foreach (var enumType in proto.enum_type)
			{
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, enumType.name), proto, enumType));
			}

			foreach (var messageType in proto.message_type)
			{
				RecursiveAnalyzeMessageDescriptor(messageType, protoNode, proto.package);
			}

			foreach (var service in proto.service)
			{
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, service.name), proto, service));

				foreach (var method in service.method)
				{
					if (!string.IsNullOrEmpty(method.input_type))
						protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, method.input_type)));

					if (!string.IsNullOrEmpty(method.output_type))
						protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(proto.package, method.output_type)));
				}
			}
		}

		var missingDependencies = new List<ProtoNode>();

		foreach (var pair in protobufMap)
		{
			foreach (var dependency in pair.Value.Proto!.dependency)
			{
				if (dependency.StartsWith("google", StringComparison.Ordinal))
					continue;

				if (protobufMap.TryGetValue(dependency, out var depends))
				{
					pair.Value.Dependencies.Add(depends);
				}
				else
				{
					logger.Warn($"Unknown dependency: {dependency} for {pair.Value.Proto.name}");

					var missing = missingDependencies.Find(x => x.Name == dependency);
					if (missing == null)
					{
						missing = new ProtoNode()
						{
							Name = dependency,
							Proto = null,
							Dependencies = [],
							AllPublicDependencies = [],
							Types = [],
							Defined = false
						};
						missingDependencies.Add(missing);
					}

					pair.Value.Dependencies.Add(missing);
				}
			}
		}

		foreach (var depend in missingDependencies)
		{
			protobufMap.Add(depend.Name!, depend);
		}

		foreach (var pair in protobufMap)
		{
			var undefinedFiles = pair.Value.Dependencies.Where(x => !x.Defined).ToList();

			if (undefinedFiles.Count > 0)
			{
				logger.Error($"Not all dependencies were found for {pair.Key}");

				foreach (var file in undefinedFiles)
				{
					var _ = protobufMap[file.Name!];
					logger.Error($"Dependency not found: {file.Name}");
				}

				return false;
			}

			var undefinedTypes = pair.Value.Types.Where(x => !x.Defined).ToList();

			if (undefinedTypes.Count > 0)
			{
				logger.Error($"Not all types were resolved for {pair.Key}");

				foreach (var type in undefinedTypes)
				{
					var _ = protobufTypeMap[type.Name!];
					logger.Error($"Type not found: {type.Name}");
				}

				return false;
			}

			RecursiveAddPublicDependencies(pair.Value.AllPublicDependencies, pair.Value, 0);
		}

		return true;
	}

	public bool Analyze()
	{
		return Analyze(protobufs);
	}

	public string FormatFile(FileDescriptorProto proto)
	{
		var sb = new StringBuilder();
		DumpFileDescriptor(proto, sb);
		return sb.ToString();
	}

	public void DumpFiles(ProcessProtobuf callback)
	{
		foreach (var proto in protobufs)
		{
			var text = FormatFile(proto);
			callback(proto, text);
		}
	}

}
