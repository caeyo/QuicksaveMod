namespace Celeste.Mod.QuicksaveMod.Ghost.Storage;

internal static class GhostPath {
    private static string? cachedGhostsRootFullPath;

    public static string GhostsRoot =>
        Path.Combine(Everest.PathGame, "Ghosts");

    public static string GhostsRootFullPath =>
        cachedGhostsRootFullPath ??= Path.GetFullPath(GhostsRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string TempDirectory =>
        Path.Combine(GhostsRoot, GhostConstants.TempFolderName);

    private static string TempDirectoryFullPath =>
        Path.GetFullPath(TempDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static bool IsTempPlaybackPath(string path) {
        string fullPath = Path.GetFullPath(path);
        string tempRoot = TempDirectoryFullPath;

        if (!fullPath.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(tempRoot, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string fileName = Path.GetFileName(fullPath);
        return fileName.StartsWith(GhostConstants.TempTasPrefix, StringComparison.Ordinal)
            && fileName.EndsWith(".tas", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) {
        subdirectory = null;

        if (!TryGetPathUnderRoot(absolutePath, out string fullPath, out string relativePath)) {
            return false;
        }

        if (File.Exists(fullPath)) {
            relativePath = Path.GetDirectoryName(relativePath) ?? "";
        } else if (!Directory.Exists(fullPath)) {
            return Path.HasExtension(fullPath)
                && fullPath.EndsWith(GhostConstants.Extension, StringComparison.OrdinalIgnoreCase)
                && TryGetRelativeSubdirectory(Path.GetDirectoryName(fullPath)!, out subdirectory);
        }

        relativePath = NormalizeRelativePath(relativePath);
        if (relativePath.Length == 0 || relativePath is ".") {
            return true;
        }

        subdirectory = relativePath;
        return true;
    }

    public static string ResolveGhostFilePath(string path, bool mustExist) {
        string fullPath = Path.GetFullPath(path);
        if (!IsUnderGhostsRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Ghosts folder.", nameof(path));
        }

        if (!fullPath.EndsWith(GhostConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Ghost path must point to a .ghost file.", nameof(path));
        }

        if (mustExist && !File.Exists(fullPath)) {
            throw new FileNotFoundException($"Ghost file not found: {path}");
        }

        return fullPath;
    }

    public static string ResolveSaveDirectory(string? subdirectory) {
        string root = GhostsRootFullPath;

        if (string.IsNullOrWhiteSpace(subdirectory)) {
            return root;
        }

        subdirectory = NormalizeRelativePath(subdirectory);

        if (subdirectory.Split(Path.DirectorySeparatorChar).Any(part => part is "." or "..")) {
            throw new ArgumentException("Invalid ghost subdirectory.", nameof(subdirectory));
        }

        string resolved = Path.GetFullPath(Path.Combine(root, subdirectory));
        if (!IsUnderGhostsRoot(resolved)) {
            throw new ArgumentException(
                "Ghost subdirectory must stay within the Ghosts folder.",
                nameof(subdirectory)
            );
        }

        return resolved;
    }

    public static bool IsUnderGhostsRoot(string fullPath) {
        string root = GhostsRootFullPath;
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

    public static void ValidateSingleName(string name, string paramName) {
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

    public static void ValidateCreatableFolderName(string folderName) {
        if (folderName.StartsWith('.')) {
            throw new ArgumentException("Folder name must not start with '.'.", nameof(folderName));
        }
    }

    public static string EnsureGhostExtension(string fileName) {
        if (!fileName.EndsWith(GhostConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            return fileName + GhostConstants.Extension;
        }

        return fileName;
    }

    public static string ResolveDeletableDirectoryPath(string fullPath) {
        fullPath = Path.GetFullPath(fullPath);
        string root = GhostsRootFullPath;

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("The Ghosts root folder cannot be deleted.");
        }

        if (!Directory.Exists(fullPath)) {
            throw new FileNotFoundException($"Ghost folder not found: {fullPath}");
        }

        string folderName = Path.GetFileName(
            fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        if (folderName.StartsWith('.')) {
            throw new InvalidOperationException($"The folder '{folderName}' cannot be deleted.");
        }

        return fullPath;
    }

    private static bool TryGetPathUnderRoot(string path, out string fullPath, out string relativePath) {
        fullPath = Path.GetFullPath(path);
        string root = GhostsRootFullPath;

        if (!IsUnderGhostsRoot(fullPath)) {
            relativePath = "";
            return false;
        }

        relativePath = Path.GetRelativePath(root, fullPath);
        return true;
    }
}
