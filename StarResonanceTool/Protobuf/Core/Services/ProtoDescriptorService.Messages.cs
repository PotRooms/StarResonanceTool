// COPYRIGHT 2025 PotRooms

using google.protobuf;
using ProtoDescDumper.Core.Services.ProtoDescriptor;

namespace ProtoDescDumper.Core;

public sealed partial class ProtoDescriptorService
{
	void RecursiveAnalyzeMessageDescriptor(DescriptorProto messageType, ProtoNode protoNode, string packagePath)
	{
		protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(packagePath, messageType.name), protoNode.Proto, messageType));

		foreach (var extension in messageType.extension)
		{
			if (!string.IsNullOrEmpty(extension.extendee))
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(packagePath, extension.extendee)));
		}

		foreach (var enumType in messageType.enum_type)
		{
			protoNode.Types.Add(GetOrCreateTypeNode(
				GetPackagePath(GetPackagePath(packagePath, messageType.name), enumType.name),
				protoNode.Proto, enumType));
		}

		foreach (var field in messageType.field)
		{
			if (IsNamedType(field.type) && !string.IsNullOrEmpty(field.type_name))
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(packagePath, field.type_name)));

			if (!string.IsNullOrEmpty(field.extendee))
				protoNode.Types.Add(GetOrCreateTypeNode(GetPackagePath(packagePath, field.extendee)));
		}

		foreach (var nested in messageType.nested_type)
		{
			RecursiveAnalyzeMessageDescriptor(nested, protoNode, GetPackagePath(packagePath, messageType.name));
		}
	}

	void RecursiveAddPublicDependencies(HashSet<FileDescriptorProto> set, ProtoNode node, int depth)
	{
		if (node.Proto == null)
			return;

		if (depth == 0)
		{
			foreach (var dep in node.Proto.dependency)
			{
				var depend = protobufMap[dep];
				if (depend.Proto != null && set.Add(depend.Proto))
				{
					RecursiveAddPublicDependencies(set, depend, depth + 1);
				}
			}
		}
		else
		{
			foreach (var idx in node.Proto.public_dependency)
			{
				var depend = protobufMap[node.Proto.dependency[idx]];
				if (depend.Proto != null && set.Add(depend.Proto))
				{
					RecursiveAddPublicDependencies(set, depend, depth + 1);
				}
			}
		}
	}
}
