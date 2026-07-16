using System.Text;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Recording;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public static class QuicksaveService {
    public static QuicksaveData? Current => QuicksaveTracker.Instance.Current;
    public static bool IsTracking => QuicksaveTracker.Instance.IsTracking;
    public static bool IsTrackingSuspended => GameplayInputRecorder.IsSuspended;

    public static string QuicksavesRoot => Path.Combine(Everest.PathGame, "Quicksaves");

    public static void SuspendTracking() => GameplayInputRecorder.Suspend();

    public static void ResumeTracking() => GameplayInputRecorder.Resume();

    public static void SaveQuicksave(string? fileName = null, string? subdirectory = null) {
        var data = QuicksaveTracker.Instance.Current
            ?? throw new InvalidOperationException("No quicksave tracking session is active.");

        string directory = ResolveSaveDirectory(subdirectory);
        Directory.CreateDirectory(directory);

        fileName ??= $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.qs";
        if (!fileName.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)) {
            fileName += ".qs";
        }

        data.SaveUid = CelesteSaveSlotResolver.EnsureCurrentSaveUid();
        data.CreatedUtc = DateTime.UtcNow;
        string path = Path.Combine(directory, fileName);
        QuicksaveSerializer.Write(path, data);
        Logger.Info(nameof(QuicksaveService), $"Saved quicksave to {path}");
    }

    public static void LoadQuicksave(string filePath) {
        string fullPath = ResolveQuicksaveFilePath(filePath, mustExist: true);
        var data = QuicksaveSerializer.Read(fullPath);
        int targetSlot = CelesteSaveSlotResolver.ResolveSlot(data.SaveUid);
        ActivateSaveSlot(targetSlot);
        Session session = data.Start.BuildSession();

        string tempDir = Path.Combine(QuicksavesRoot, ".temp");
        Directory.CreateDirectory(tempDir);

        string tempTasPath = Path.Combine(tempDir, $"playback_{Guid.NewGuid():N}.tas");
        WriteTempTasFile(tempTasPath, data);

        Engine.Scene = new LevelLoader(session);
        QuicksavePlayback.Start(tempTasPath);
        Logger.Info(
            nameof(QuicksaveService),
            $"Loading quicksave playback from {fullPath} in save slot {targetSlot}"
        );
    }

    public static void MoveQuicksave(string sourcePath, string targetDirectory) {
        string source = ResolveQuicksaveFilePath(sourcePath, mustExist: true);
        string targetDir = ResolveSaveDirectory(targetDirectory);
        Directory.CreateDirectory(targetDir);

        string destination = Path.Combine(targetDir, Path.GetFileName(source));
        if (File.Exists(destination)) {
            throw new IOException($"A quicksave already exists at '{destination}'.");
        }

        File.Move(source, destination);
    }

    public static void RenameQuicksave(string path, string newName) {
        if (string.IsNullOrWhiteSpace(newName)) {
            throw new ArgumentException("New name must not be empty.", nameof(newName));
        }

        ValidateSingleName(newName, nameof(newName));

        string fullPath = Path.GetFullPath(path);
        if (!IsUnderQuicksavesRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Quicksaves folder.", nameof(path));
        }

        if (File.Exists(fullPath)) {
            RenameQuicksaveFile(fullPath, newName);
            return;
        }

        if (Directory.Exists(fullPath)) {
            RenameQuicksaveDirectory(fullPath, newName);
            return;
        }

        throw new FileNotFoundException($"Quicksave path not found: {path}");
    }

    public static void DeleteQuicksave(string path) {
        string fullPath = Path.GetFullPath(path);
        if (!IsUnderQuicksavesRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Quicksaves folder.", nameof(path));
        }

        if (File.Exists(fullPath)) {
            if (!fullPath.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("Quicksave path must point to a .qs file.", nameof(path));
            }

            File.Delete(fullPath);
            return;
        }

        if (Directory.Exists(fullPath)) {
            Directory.Delete(ResolveDeletableDirectoryPath(fullPath), recursive: true);
            return;
        }

        throw new FileNotFoundException($"Quicksave path not found: {path}");
    }

    public static void CreateQuicksaveFolder(string folderName, string? parentSubdirectory = null) {
        if (string.IsNullOrWhiteSpace(folderName)) {
            throw new ArgumentException("Folder name must not be empty.", nameof(folderName));
        }

        ValidateSingleName(folderName, nameof(folderName));
        ValidateCreatableFolderName(folderName);

        string parentDirectory = ResolveSaveDirectory(parentSubdirectory);
        string fullPath = Path.Combine(parentDirectory, folderName);

        if (Directory.Exists(fullPath)) {
            throw new IOException($"A quicksave folder already exists at '{fullPath}'.");
        }

        Directory.CreateDirectory(fullPath);
    }

    public static bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) {
        subdirectory = null;

        if (!TryGetPathUnderRoot(absolutePath, out string fullPath, out string relativePath)) {
            return false;
        }

        if (File.Exists(fullPath)) {
            relativePath = Path.GetDirectoryName(relativePath) ?? "";
        } else if (!Directory.Exists(fullPath)) {
            return Path.HasExtension(fullPath) && fullPath.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)
                ? TryGetRelativeSubdirectory(Path.GetDirectoryName(fullPath)!, out subdirectory)
                : false;
        }

        relativePath = NormalizeRelativePath(relativePath);
        if (relativePath.Length == 0 || relativePath is ".") {
            return true;
        }

        subdirectory = relativePath;
        return true;
    }

    private static void RenameQuicksaveFile(string fullPath, string newName) {
        string renamed = EnsureQuicksaveExtension(newName);
        ValidateSingleName(renamed, nameof(newName));

        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, renamed);
        if (File.Exists(destination)) {
            throw new IOException($"A quicksave already exists at '{destination}'.");
        }

        File.Move(fullPath, destination);
    }

    private static void RenameQuicksaveDirectory(string fullPath, string newName) {
        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, newName);
        if (Directory.Exists(destination)) {
            throw new IOException($"A quicksave folder already exists at '{destination}'.");
        }

        Directory.Move(fullPath, destination);
    }

    private static string ResolveDeletableDirectoryPath(string fullPath) {
        fullPath = Path.GetFullPath(fullPath);
        string root = GetQuicksavesRootFullPath();

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("The Quicksaves root folder cannot be deleted.");
        }

        if (!Directory.Exists(fullPath)) {
            throw new FileNotFoundException($"Quicksave folder not found: {fullPath}");
        }

        string folderName = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (folderName.StartsWith('.')) {
            throw new InvalidOperationException($"The folder '{folderName}' cannot be deleted.");
        }

        return fullPath;
    }

    private static void ValidateCreatableFolderName(string folderName) {
        if (folderName.StartsWith('.')) {
            throw new ArgumentException("Folder name must not start with '.'.", nameof(folderName));
        }
    }

    private static string ResolveQuicksaveFilePath(string path, bool mustExist) {
        string fullPath = Path.GetFullPath(path);
        if (!IsUnderQuicksavesRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Quicksaves folder.", nameof(path));
        }

        if (!fullPath.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Quicksave path must point to a .qs file.", nameof(path));
        }

        if (mustExist && !File.Exists(fullPath)) {
            throw new FileNotFoundException($"Quicksave file not found: {path}");
        }

        return fullPath;
    }

    private static string ResolveSaveDirectory(string? subdirectory) {
        string root = GetQuicksavesRootFullPath();

        if (string.IsNullOrWhiteSpace(subdirectory)) {
            return root;
        }

        subdirectory = NormalizeRelativePath(subdirectory);

        if (subdirectory.Split(Path.DirectorySeparatorChar).Any(part => part is "." or "..")) {
            throw new ArgumentException("Invalid quicksave subdirectory.", nameof(subdirectory));
        }

        string resolved = Path.GetFullPath(Path.Combine(root, subdirectory));
        if (!IsUnderQuicksavesRoot(resolved)) {
            throw new ArgumentException("Quicksave subdirectory must stay within the Quicksaves folder.", nameof(subdirectory));
        }

        return resolved;
    }

    private static bool TryGetPathUnderRoot(string path, out string fullPath, out string relativePath) {
        fullPath = Path.GetFullPath(path);
        string root = GetQuicksavesRootFullPath();

        if (!IsUnderQuicksavesRoot(fullPath)) {
            relativePath = "";
            return false;
        }

        relativePath = Path.GetRelativePath(root, fullPath);
        return true;
    }

    private static bool IsUnderQuicksavesRoot(string fullPath) {
        string root = GetQuicksavesRootFullPath();
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetQuicksavesRootFullPath() =>
        Path.GetFullPath(QuicksavesRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

    private static void ValidateSingleName(string name, string paramName) {
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            throw new ArgumentException("Name contains invalid characters.", paramName);
        }

        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar)) {
            throw new ArgumentException("Name must not contain path separators.", paramName);
        }

        if (name is "." or "..") {
            throw new ArgumentException("Name must not be '.' or '..'.", paramName);
        }
    }

    private static string EnsureQuicksaveExtension(string fileName) {
        if (!fileName.EndsWith(".qs", StringComparison.OrdinalIgnoreCase)) {
            return fileName + ".qs";
        }

        return fileName;
    }

    private static void ActivateSaveSlot(int targetSlot) {
        if (SaveData.Instance?.FileSlot == targetSlot) {
            return;
        }

        if (targetSlot == -1) {
            SaveData.InitializeDebugMode();
            return;
        }

        string saveName = SaveData.GetFilename(targetSlot);
        SaveData? saveData = UserIO.Load<SaveData>(saveName);
        if (saveData == null) {
            Logger.Warn(
                nameof(QuicksaveService),
                $"Save slot {targetSlot} could not be loaded; falling back to debug."
            );
            SaveData.InitializeDebugMode();
            return;
        }

        SaveData.Start(saveData, targetSlot);
    }

    private static void WriteTempTasFile(string path, QuicksaveData data) {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (string line in data.Inputs) {
            writer.WriteLine(TasLineFormatter.FormatFileLine(line));
        }

        writer.WriteLine(GetPlaybackBreakpointLine());
        writer.WriteLine(TasLineFormatter.FormatFileLine("1"));
    }

    private static string GetPlaybackBreakpointLine() {
        PlaybackSpeed speed = QuicksaveModModule.Settings.PlaybackSpeed;
        return speed == PlaybackSpeed.Max ? "***" : $"***{(int)speed}";
    }
}
