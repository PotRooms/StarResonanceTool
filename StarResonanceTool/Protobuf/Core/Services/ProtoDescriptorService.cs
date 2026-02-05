// COPYRIGHT 2025 PotRooms

using google.protobuf;
using ProtoDescDumper.Core.Abstractions;
using ProtoDescDumper.Core.Services.ProtoDescriptor;

namespace ProtoDescDumper.Core;

public sealed partial class ProtoDescriptorService(IEnumerable<FileDescriptorProto> protobufs, ILogger logger) : IProtoDescriptorAnalyzer, IProtoDescriptorFormatter
{
	public delegate void ProcessProtobuf(FileDescriptorProto buffer, string proto);

	readonly List<FileDescriptorProto> protobufs = [.. protobufs];
	readonly Stack<string> messageNameStack = [];
	readonly Dictionary<string, ProtoNode> protobufMap = [];
	readonly Dictionary<string, ProtoTypeNode> protobufTypeMap = [];
	readonly ILogger logger = logger;
}
