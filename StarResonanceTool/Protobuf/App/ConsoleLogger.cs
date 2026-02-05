// COPYRIGHT 2025 PotRooms

using ProtoDescDumper.Core.Abstractions;

namespace ProtoDescDumper.App;

public sealed class ConsoleLogger : ILogger
{
	private static readonly object _lock = new();

	public void Info(string message)
	{
		lock (_lock)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}
	}

	public void Warn(string message)
	{
		lock (_lock)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}
	}

	public void Error(string message)
	{
		lock (_lock)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}
	}

	public void Error(string message, Exception ex)
	{
		Error($"{message}: {ex.Message}");
	}
}