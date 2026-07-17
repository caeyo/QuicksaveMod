namespace Celeste.Mod.QuicksaveMod.Quicksave.Storage;

internal static class QuicksaveRepository {
    public static void MoveQuicksave(string sourcePath, string targetDirectory) {
        string source = QuicksavePath.ResolveQuicksaveFilePath(sourcePath, mustExist: true);
        string targetDir = QuicksavePath.ResolveSaveDirectory(targetDirectory);
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

        QuicksavePath.ValidateSingleName(newName, nameof(newName));

        string fullPath = Path.GetFullPath(path);
        if (!QuicksavePath.IsUnderQuicksavesRoot(fullPath)) {
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
        if (!QuicksavePath.IsUnderQuicksavesRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Quicksaves folder.", nameof(path));
        }

        if (File.Exists(fullPath)) {
            if (!fullPath.EndsWith(QuicksaveConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("Quicksave path must point to a .qs file.", nameof(path));
            }

            File.Delete(fullPath);
            return;
        }

        if (Directory.Exists(fullPath)) {
            Directory.Delete(QuicksavePath.ResolveDeletableDirectoryPath(fullPath), recursive: true);
            return;
        }

        throw new FileNotFoundException($"Quicksave path not found: {path}");
    }

    public static void CreateQuicksaveFolder(string folderName, string? parentSubdirectory = null) {
        if (string.IsNullOrWhiteSpace(folderName)) {
            throw new ArgumentException("Folder name must not be empty.", nameof(folderName));
        }

        QuicksavePath.ValidateSingleName(folderName, nameof(folderName));
        QuicksavePath.ValidateCreatableFolderName(folderName);

        string parentDirectory = QuicksavePath.ResolveSaveDirectory(parentSubdirectory);
        string fullPath = Path.Combine(parentDirectory, folderName);

        if (Directory.Exists(fullPath)) {
            throw new IOException($"A quicksave folder already exists at '{fullPath}'.");
        }

        Directory.CreateDirectory(fullPath);
    }

    private static void RenameQuicksaveFile(string fullPath, string newName) {
        string renamed = QuicksavePath.EnsureQuicksaveExtension(newName);
        QuicksavePath.ValidateSingleName(renamed, nameof(newName));

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
}
