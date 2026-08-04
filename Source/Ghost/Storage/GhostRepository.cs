namespace Celeste.Mod.QuicksaveMod.Ghost.Storage;

internal static class GhostRepository {
    public static void MoveGhost(string sourcePath, string targetDirectory) {
        string source = GhostPath.ResolveGhostFilePath(sourcePath, mustExist: true);
        string targetDir = GhostPath.ResolveSaveDirectory(targetDirectory);
        Directory.CreateDirectory(targetDir);

        string destination = Path.Combine(targetDir, Path.GetFileName(source));
        if (File.Exists(destination)) {
            throw new IOException($"A ghost file already exists at '{destination}'.");
        }

        File.Move(source, destination);
    }

    public static void RenameGhost(string path, string newName) {
        if (string.IsNullOrWhiteSpace(newName)) {
            throw new ArgumentException("New name must not be empty.", nameof(newName));
        }

        GhostPath.ValidateSingleName(newName, nameof(newName));

        string fullPath = Path.GetFullPath(path);
        if (!GhostPath.IsUnderGhostsRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Ghosts folder.", nameof(path));
        }

        if (File.Exists(fullPath)) {
            RenameGhostFile(fullPath, newName);
            return;
        }

        if (Directory.Exists(fullPath)) {
            RenameGhostDirectory(fullPath, newName);
            return;
        }

        throw new FileNotFoundException($"Ghost path not found: {path}");
    }

    public static void DeleteGhost(string path) {
        string fullPath = Path.GetFullPath(path);
        if (!GhostPath.IsUnderGhostsRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Ghosts folder.", nameof(path));
        }

        if (File.Exists(fullPath)) {
            if (!fullPath.EndsWith(GhostConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("Ghost path must point to a .ghost file.", nameof(path));
            }

            File.Delete(fullPath);
            return;
        }

        if (Directory.Exists(fullPath)) {
            Directory.Delete(GhostPath.ResolveDeletableDirectoryPath(fullPath), recursive: true);
            return;
        }

        throw new FileNotFoundException($"Ghost path not found: {path}");
    }

    public static void CreateGhostFolder(string folderName, string? parentSubdirectory = null) {
        if (string.IsNullOrWhiteSpace(folderName)) {
            throw new ArgumentException("Folder name must not be empty.", nameof(folderName));
        }

        GhostPath.ValidateSingleName(folderName, nameof(folderName));
        GhostPath.ValidateCreatableFolderName(folderName);

        string parentDirectory = GhostPath.ResolveSaveDirectory(parentSubdirectory);
        string fullPath = Path.Combine(parentDirectory, folderName);

        if (Directory.Exists(fullPath)) {
            throw new IOException($"A ghost folder already exists at '{fullPath}'.");
        }

        Directory.CreateDirectory(fullPath);
    }

    private static void RenameGhostFile(string fullPath, string newName) {
        string renamed = GhostPath.EnsureGhostExtension(newName);
        GhostPath.ValidateSingleName(renamed, nameof(newName));

        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, renamed);
        if (File.Exists(destination)) {
            throw new IOException($"A ghost file already exists at '{destination}'.");
        }

        File.Move(fullPath, destination);
    }

    private static void RenameGhostDirectory(string fullPath, string newName) {
        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, newName);
        if (Directory.Exists(destination)) {
            throw new IOException($"A ghost folder already exists at '{destination}'.");
        }

        Directory.Move(fullPath, destination);
    }
}
