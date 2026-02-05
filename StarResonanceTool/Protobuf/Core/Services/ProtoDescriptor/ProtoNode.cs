// COPYRIGHT 2025 PotRooms

using google.protobuf;

namespace ProtoDescDumper.Core.Services.ProtoDescriptor;

sealed class ProtoNode
{
	public string? Name;
	public FileDescriptorProto? Proto;
	public List<ProtoNode> Dependencies = [];
	public HashSet<FileDescriptorProto> AllPublicDependencies = [];
	public List<ProtoTypeNode> Types = [];
	public bool Defined;
}
