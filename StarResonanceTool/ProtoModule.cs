// COPYRIGHT 2025 PotRooms

using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace StarResonanceTool;

internal class ProtoModule
{

	public static FileDescriptorSet TryParseFileDescriptorSet(byte[] data)
	{
		if (data == null || data.Length == 0) return null;
		try
		{
			return FileDescriptorSet.Parser.ParseFrom(data);
		}
		catch { return null; }
	}

	public static byte[] TryGunzip(byte[] data)
	{
		if (data == null || data.Length < 2) return Array.Empty<byte>();
		if (data[0] != 0x1F || data[1] != 0x8B) return Array.Empty<byte>();
		try
		{
			using var ms = new MemoryStream(data);
			using var gz = new GZipStream(ms, CompressionMode.Decompress);
			using var outMs = new MemoryStream();
			gz.CopyTo(outMs);
			return outMs.ToArray();
		}
		catch { return Array.Empty<byte>(); }
	}

	public static FileDescriptorSet TryScanForEmbeddedFds(byte[] data)
	{
		if (data == null || data.Length == 0) return null;

		foreach (var segment in ExtractLengthDelimitedSegments(data))
		{
			var f = TryParseFileDescriptorSet(segment);
			if (f != null) return f;
		}

		for (int offset = 0; offset < data.Length; offset += 1)
		{
			int maxLen = Math.Min(4_000_000, data.Length - offset);
			for (int len = 32; len <= maxLen; len = (int)(len * 1.5) + 1)
			{
				var span = new ReadOnlySpan<byte>(data, offset, len);
				var bytes = span.ToArray();
				var f = TryParseFileDescriptorSet(bytes);
				if (f != null) return f;
			}
		}

		return null;
	}

	private static IEnumerable<byte[]> ExtractLengthDelimitedSegments(byte[] data)
	{
		int i = 0;
		while (i < data.Length)
		{
			int keyStart = i;
			if (!TryReadVarint(data, ref i, out ulong key)) { i = keyStart + 1; continue; }
			int wireType = (int)(key & 0x7);
			if (wireType == 2)
			{
				if (!TryReadVarint(data, ref i, out ulong len)) { continue; }
				long l = (long)len;
				if (l <= 0 || i + l > data.Length) { continue; }
				var segment = new byte[l];
				Buffer.BlockCopy(data, i, segment, 0, (int)l);
				yield return segment;
				i += (int)l;
			}
			else
			{
				switch (wireType)
				{
					case 0:
						if (!TryReadVarint(data, ref i, out _)) { i++; }
						break;
					case 1:
						i += 8; break;
					case 5:
						i += 4; break;
					default:
						i++; break;
				}
			}
		}
	}

	private static bool TryReadVarint(byte[] data, ref int index, out ulong value)
	{
		value = 0;
		int shift = 0;
		int i = index;
		while (i < data.Length && shift < 64)
		{
			byte b = data[i++];
			value |= (ulong)(b & 0x7F) << shift;
			if ((b & 0x80) == 0) { index = i; return true; }
			shift += 7;
		}
		return false;
	}
}

internal sealed class ProtoSchemaWriter
{
	public IReadOnlyList<string> WriteFiles(FileDescriptorSet set, string outDir)
	{
		var paths = new List<string>();
		foreach (var fd in set.File)
		{
			var relPath = string.IsNullOrWhiteSpace(fd.Name)
				? BuildPathFromPackage(fd, InferFileName(fd))
				: BuildPathFromPackage(fd, fd.Name);

			var path = Path.Combine(outDir, NormalizePath(relPath));

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			var text = RenderFile(fd);
			File.WriteAllText(path, text, new UTF8Encoding(false));
			paths.Add(path);
		}
		return paths;
	}

	private static string NormalizePath(string p)
	{
		p = p.Replace('\\', '/');
		if (Path.IsPathRooted(p)) p = p.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return p;
	}

	private string RenderFile(FileDescriptorProto fd)
	{
		var sb = new StringBuilder();
		var syntax = string.IsNullOrEmpty(fd.Syntax) ? "proto2" : fd.Syntax;
		sb.AppendLine($"syntax = \"{syntax}\";");
		var isProto3 = string.Equals(syntax, "proto3", StringComparison.OrdinalIgnoreCase);
		if (!string.IsNullOrEmpty(fd.Package))
			sb.AppendLine($"\npackage {fd.Package};");
		if (fd.Dependency.Count > 0)
		{
			sb.AppendLine();
			foreach (var dep in fd.Dependency)
				sb.AppendLine($"import \"{dep}\";");
		}
		if (fd.Options != null)
		{
			if (!string.IsNullOrEmpty(fd.Options?.JavaPackage))
				sb.AppendLine($"option java_package = \"{fd.Options.JavaPackage}\";");
			if (!string.IsNullOrEmpty(fd.Options?.CsharpNamespace))
				sb.AppendLine($"option csharp_namespace = \"{fd.Options.CsharpNamespace}\";");
		}
		if (sb.Length > 0) sb.AppendLine();

		foreach (var en in fd.EnumType)
		{
			RenderEnum(sb, en, 0);
			sb.AppendLine();
		}
		foreach (var msg in fd.MessageType)
		{
			RenderMessage(sb, msg, 0, isProto3, false);
			sb.AppendLine();
		}
		foreach (var svc in fd.Service)
		{
			RenderService(sb, svc, 0);
			sb.AppendLine();
		}

		return sb.ToString();
	}

	private void RenderMessage(StringBuilder sb, DescriptorProto msg, int indent, bool isProto3, bool isNested = false)
	{
		var ind = new string(' ', indent * 2);
		sb.AppendLine($"{ind}message {msg.Name} {{");

		var oneofGroups = new Dictionary<int, List<FieldDescriptorProto>>();
		foreach (var f in msg.Field)
		{
			if (f.HasOneofIndex)
			{
				int idx = f.OneofIndex;
				if (!oneofGroups.TryGetValue(idx, out var list))
				{
					list = new List<FieldDescriptorProto>();
					oneofGroups[idx] = list;
				}
				list.Add(f);
			}
		}

		foreach (var f in msg.Field.Where(f => !f.HasOneofIndex))
		{
			if (IsMapField(f, msg, out var keyType, out var valueType))
			{
				sb.AppendLine($"{ind}  map<{keyType}, {valueType}> {EnsureSnakeCase(f.Name)} = {f.Number};");
			}
			else
			{
				var label = FieldLabelPrefix(f, isProto3, isNested);
				var type = FieldTypeName(f);
				sb.AppendLine($"{ind}  {label}{type} {EnsureSnakeCase(f.Name)} = {f.Number};");
			}
		}

		foreach (var kvp in oneofGroups.OrderBy(k => k.Key))
		{
			string oneofName = msg.OneofDecl[kvp.Key].Name;
			sb.AppendLine($"{ind}  oneof {oneofName} {{");
			foreach (var f in kvp.Value)
			{
				var type = FieldTypeName(f);
				sb.AppendLine($"{ind}    {type} {EnsureSnakeCase(f.Name)} = {f.Number};");
			}
			sb.AppendLine($"{ind}  }}");
		}

		foreach (var en in msg.EnumType)
		{
			sb.AppendLine();
			RenderEnum(sb, en, indent + 1);
		}

		foreach (var nested in msg.NestedType)
		{
			if (nested.Options?.MapEntry == true) continue;
			sb.AppendLine();
			RenderMessage(sb, nested, indent + 1, isProto3, true);
		}

		sb.AppendLine($"{ind}}}");
	}

	private void RenderEnum(StringBuilder sb, EnumDescriptorProto en, int indent)
	{
		var ind = new string(' ', indent * 2);
		sb.AppendLine($"{ind}enum {en.Name} {{");
		foreach (var v in en.Value)
		{
			sb.AppendLine($"{ind}  {v.Name} = {v.Number};");
		}
		sb.AppendLine($"{ind}}}");
	}

	private void RenderService(StringBuilder sb, ServiceDescriptorProto svc, int indent)
	{
		var ind = new string(' ', indent * 2);
		sb.AppendLine($"{ind}service {svc.Name} {{");
		foreach (var m in svc.Method)
		{
			string clientStream = m.ClientStreaming ? "stream " : string.Empty;
			string serverStream = m.ServerStreaming ? "stream " : string.Empty;
			sb.AppendLine($"{ind}  rpc {m.Name} ({clientStream}{m.InputType}) returns ({serverStream}{m.OutputType});");
		}
		sb.AppendLine($"{ind}}}");
	}

	private static string FieldLabelPrefix(FieldDescriptorProto f, bool isProto3, bool isNested)
	{
		if (isProto3)
		{
			if (f.Label == FieldDescriptorProto.Types.Label.Repeated)
				return "repeated ";
			return string.Empty;
		}

		if (isNested)
		{
			return f.Label == FieldDescriptorProto.Types.Label.Repeated ? "repeated " : string.Empty;
		}

		return f.Label switch
		{
			FieldDescriptorProto.Types.Label.Repeated => "repeated ",
			FieldDescriptorProto.Types.Label.Required => "required ",
			FieldDescriptorProto.Types.Label.Optional => "optional ",
			_ => string.Empty
		};
	}

	private static string FieldTypeName(FieldDescriptorProto f)
	{
		if (f.Type == FieldDescriptorProto.Types.Type.Message || f.Type == FieldDescriptorProto.Types.Type.Enum)
		{
			return TrimLeadingDot(f.TypeName);
		}
		return f.Type switch
		{
			FieldDescriptorProto.Types.Type.Double => "double",
			FieldDescriptorProto.Types.Type.Float => "float",
			FieldDescriptorProto.Types.Type.Int64 => "int64",
			FieldDescriptorProto.Types.Type.Uint64 => "uint64",
			FieldDescriptorProto.Types.Type.Int32 => "int32",
			FieldDescriptorProto.Types.Type.Fixed64 => "fixed64",
			FieldDescriptorProto.Types.Type.Fixed32 => "fixed32",
			FieldDescriptorProto.Types.Type.Bool => "bool",
			FieldDescriptorProto.Types.Type.String => "string",
			FieldDescriptorProto.Types.Type.Bytes => "bytes",
			FieldDescriptorProto.Types.Type.Uint32 => "uint32",
			FieldDescriptorProto.Types.Type.Sfixed32 => "sfixed32",
			FieldDescriptorProto.Types.Type.Sfixed64 => "sfixed64",
			FieldDescriptorProto.Types.Type.Sint32 => "sint32",
			FieldDescriptorProto.Types.Type.Sint64 => "sint64",
			_ => "bytes"
		};
	}

	private static bool IsMapField(FieldDescriptorProto f, DescriptorProto parent, out string keyType, out string valueType)
	{
		keyType = valueType = string.Empty;
		if (f.Type != FieldDescriptorProto.Types.Type.Message || string.IsNullOrEmpty(f.TypeName)) return false;
		var typeName = TrimLeadingDot(f.TypeName);
		var nested = parent.NestedType.FirstOrDefault(n => n.Name == typeName.Split('.').Last());
		if (nested?.Options?.MapEntry == true)
		{
			var keyField = nested.Field.FirstOrDefault(ff => ff.Name == "key") ?? nested.Field.First();
			var valField = nested.Field.FirstOrDefault(ff => ff.Name == "value") ?? nested.Field.Skip(1).FirstOrDefault();
			keyType = FieldTypeName(keyField);
			valueType = FieldTypeName(valField);
			return true;
		}
		return false;
	}

	private static string TrimLeadingDot(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;
		return s[0] == '.' ? s.Substring(1) : s;
	}

	private static string EnsureSnakeCase(string fieldStr)
	{
		if (fieldStr.StartsWith("_"))
		{
			fieldStr = fieldStr.Substring(1);
		}
		if (fieldStr.Contains("_") || fieldStr.All(char.IsLower))
		{
			return fieldStr;
		}
		return Regex.Replace(fieldStr, @"(([a-z])(?=[A-Z][a-zA-Z])|([A-Z])(?=[A-Z][a-z]))", "$1_").ToLower();
	}

	private static string BuildPathFromPackage(FileDescriptorProto fd, string fileName)
	{
		var pkg = string.IsNullOrWhiteSpace(fd.Package) ? string.Empty : fd.Package.Replace('.', '/');
		if (string.IsNullOrEmpty(pkg)) return fileName;
		return $"{pkg}/{fileName}";
	}

	private static string InferFileName(FileDescriptorProto fd)
	{
		var name = fd.MessageType.FirstOrDefault()?.Name
					?? fd.EnumType.FirstOrDefault()?.Name
					?? fd.Service.FirstOrDefault()?.Name
					?? "types";
		return name + ".proto";
	}
}
