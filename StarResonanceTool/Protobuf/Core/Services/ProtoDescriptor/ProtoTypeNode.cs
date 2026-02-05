// COPYRIGHT 2025 PotRooms

using google.protobuf;

namespace ProtoDescDumper.Core.Services.ProtoDescriptor;

sealed class ProtoTypeNode
{
	public string? Name;
	public FileDescriptorProto? Proto;
	public object? Source;
	public bool Defined;
}
