using System.Collections.Concurrent;
using System.Text;
using Godot;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime;

internal static class TrainerLog
{
    private const int MaxEntries = 256;
    private static readonly ConcurrentQueue<string> Entries = new();

    public static void Info(string message)
    {
        Write("INFO", message, isError: false);
    }

    public static void Warn(string message)
    {
        Write("WARN", message, isError: false);
    }

    public static void Error(string message)
    {
        Write("ERROR", message, isError: true);
    }

    public static IReadOnlyList<string> Snapshot()
    {
        return Entries.ToArray();
    }

    public static string Export(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        var filePath = Path.Combine(baseDirectory, $"trainer-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        File.WriteAllText(filePath, string.Join(System.Environment.NewLine, Snapshot()), Encoding.UTF8);
        return filePath;
    }

    private static void Write(string level, string message, bool isError)
    {
        var line = level == "INFO"
            ? $"[{DateTime.Now:HH:mm:ss}] {message}"
            : $"[{DateTime.Now:HH:mm:ss}] {level} {message}";
        Entries.Enqueue(line);

        while (Entries.Count > MaxEntries && Entries.TryDequeue(out _))
        {
        }

        if (isError)
        {
            GD.PrintErr($"[{TrainerConstants.ModId}] {line}");
        }
        else
        {
            GD.Print($"[{TrainerConstants.ModId}] {line}");
        }
    }
}
