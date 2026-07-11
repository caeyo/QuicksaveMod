using Celeste.Mod.QuicksaveMod.Playback;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public static class QuicksaveService {
    public static QuicksaveData? Current => QuicksaveTracker.Instance.Current;
    public static bool IsTracking => QuicksaveTracker.Instance.IsTracking;

    public static string QuicksavesRoot => Path.Combine(Everest.PathGame, "Quicksaves");

    public static void SaveQuicksave(string? fileName = null, string? subdirectory = null) {
        var data = QuicksaveTracker.Instance.Current
            ?? throw new InvalidOperationException("No quicksave tracking session is active.");

        string directory = ResolveSaveDirectory(subdirectory);
        Directory.CreateDirectory(directory);

        fileName ??= $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.qs";
        if (!fileName.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)) {
            fileName += ".qs";
        }

        data.CreatedUtc = DateTime.UtcNow;
        string path = Path.Combine(directory, fileName);
        QuicksaveSerializer.Write(path, data);
        Logger.Info(nameof(QuicksaveService), $"Saved quicksave to {path}");
    }

    public static void LoadQuicksave(string filePath) {
        string fullPath = Path.GetFullPath(filePath);
        var data = QuicksaveSerializer.Read(fullPath);

        string tempDir = Path.Combine(QuicksavesRoot, ".temp");
        Directory.CreateDirectory(tempDir);

        string tempTasPath = Path.Combine(tempDir, $"playback_{Guid.NewGuid():N}.tas");
        WriteTempTasFile(tempTasPath, data);

        QuicksavePlayback.Start(tempTasPath);
        Logger.Info(nameof(QuicksaveService), $"Loading quicksave playback from {fullPath}");
    }

    private static string ResolveSaveDirectory(string? subdirectory) {
        string root = Path.GetFullPath(QuicksavesRoot);

        if (string.IsNullOrWhiteSpace(subdirectory)) {
            return root;
        }

        subdirectory = subdirectory
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

        if (subdirectory.Split(Path.DirectorySeparatorChar).Any(part => part is "." or "..")) {
            throw new ArgumentException("Invalid quicksave subdirectory.", nameof(subdirectory));
        }

        string resolved = Path.GetFullPath(Path.Combine(root, subdirectory));
        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Quicksave subdirectory must stay within the Quicksaves folder.", nameof(subdirectory));
        }

        return resolved;
    }

    private static void WriteTempTasFile(string path, QuicksaveData data) {
        using var writer = new StreamWriter(path, false);
        writer.WriteLine(data.Start.BuildConsoleLoadCommand());
        writer.WriteLine("1");

        foreach (string line in data.Inputs) {
            writer.WriteLine(line);
        }

        writer.WriteLine("***");
        writer.WriteLine("1");
    }
}
