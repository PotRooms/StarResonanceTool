// COPYRIGHT 2025 PotRooms

using System.Globalization;
using System.Text;
using google.protobuf;
using ProtoBuf;

namespace ProtoDescDumper.Core;

public sealed partial class ProtoDescriptorService
{
	static bool IsNamedType(FieldDescriptorProto.Type type)
	{
		return type == FieldDescriptorProto.Type.TYPE_MESSAGE || type == FieldDescriptorProto.Type.TYPE_ENUM;
	}

	static string GetPackagePath(string package, string name)
	{
		package = package.Length == 0 || package.StartsWith('.') ? package : $".{package}";
		return name.StartsWith('.') ? name : $"{package}.{name}";
	}

	static string GetLabel(FieldDescriptorProto.Label label)
	{
		return label switch
		{
			FieldDescriptorProto.Label.LABEL_REQUIRED => "required",
			FieldDescriptorProto.Label.LABEL_REPEATED => "repeated",
			_ => "optional",
		};
	}

	static string GetType(FieldDescriptorProto.Type type)
	{
		return type switch
		{
			FieldDescriptorProto.Type.TYPE_INT32 => "int32",
			FieldDescriptorProto.Type.TYPE_INT64 => "int64",
			FieldDescriptorProto.Type.TYPE_SINT32 => "sint32",
			FieldDescriptorProto.Type.TYPE_SINT64 => "sint64",
			FieldDescriptorProto.Type.TYPE_UINT32 => "uint32",
			FieldDescriptorProto.Type.TYPE_UINT64 => "uint64",
			FieldDescriptorProto.Type.TYPE_STRING => "string",
			FieldDescriptorProto.Type.TYPE_BOOL => "bool",
			FieldDescriptorProto.Type.TYPE_BYTES => "bytes",
			FieldDescriptorProto.Type.TYPE_DOUBLE => "double",
			FieldDescriptorProto.Type.TYPE_ENUM => "enum",
			FieldDescriptorProto.Type.TYPE_FLOAT => "float",
			FieldDescriptorProto.Type.TYPE_GROUP => "GROUP",
			FieldDescriptorProto.Type.TYPE_MESSAGE => "message",
			FieldDescriptorProto.Type.TYPE_FIXED32 => "fixed32",
			FieldDescriptorProto.Type.TYPE_FIXED64 => "fixed64",
			FieldDescriptorProto.Type.TYPE_SFIXED32 => "sfixed32",
			FieldDescriptorProto.Type.TYPE_SFIXED64 => "sfixed64",
			_ => type.ToString(),
		};
	}

	static bool ExtractType(IExtensible data, FieldDescriptorProto field, out string? value)
	{
		switch (field.type)
		{
			case FieldDescriptorProto.Type.TYPE_INT32:
			case FieldDescriptorProto.Type.TYPE_UINT32:
			case FieldDescriptorProto.Type.TYPE_FIXED32:
				if (Extensible.TryGetValue(data, field.number, out uint int32))
				{
					value = System.Convert.ToString(int32);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_INT64:
			case FieldDescriptorProto.Type.TYPE_UINT64:
			case FieldDescriptorProto.Type.TYPE_FIXED64:
				if (Extensible.TryGetValue(data, field.number, out ulong int64))
				{
					value = System.Convert.ToString(int64);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_SINT32:
			case FieldDescriptorProto.Type.TYPE_SFIXED32:
				if (Extensible.TryGetValue(data, field.number, out int sint32))
				{
					value = System.Convert.ToString(sint32);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_SINT64:
			case FieldDescriptorProto.Type.TYPE_SFIXED64:
				if (Extensible.TryGetValue(data, field.number, out long sint64))
				{
					value = System.Convert.ToString(sint64);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_STRING:
				if (Extensible.TryGetValue(data, field.number, out string str))
				{
					value = Util.ToLiteral(str);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_BOOL:
				if (Extensible.TryGetValue(data, field.number, out bool boolean))
				{
					value = boolean ? "true" : "false";
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_BYTES:
				if (Extensible.TryGetValue(data, field.number, out byte[] bytes))
				{
					value = System.Convert.ToString(bytes);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_DOUBLE:
				if (Extensible.TryGetValue(data, field.number, out double dbl))
				{
					value = System.Convert.ToString(dbl, CultureInfo.InvariantCulture);
					return true;
				}
				break;
			case FieldDescriptorProto.Type.TYPE_FLOAT:
				if (Extensible.TryGetValue(data, field.number, out float flt))
				{
					value = System.Convert.ToString(flt, CultureInfo.InvariantCulture);
					return true;
				}
				break;
			default:
				value = null;
				return false;
		}

		value = null;
		return false;
	}

	static string ResolveType(FieldDescriptorProto field)
	{
		if (IsNamedType(field.type))
		{
			return field.type_name;
		}

		return GetType(field.type);
	}

	static void AppendHeadingSpace(StringBuilder sb, ref bool marker)
	{
		if (marker)
		{
			sb.AppendLine();
			marker = false;
		}
	}

	void PushDescriptorName(FileDescriptorProto file)
	{
		messageNameStack.Push(file.package);
	}

	void PushDescriptorName(DescriptorProto proto)
	{
		messageNameStack.Push(proto.name);
	}

	void PushDescriptorName(FieldDescriptorProto field)
	{
		messageNameStack.Push(field.name);
	}

	void PopDescriptorName()
	{
		messageNameStack.Pop();
	}
}
