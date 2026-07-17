namespace Celeste.Mod.QuicksaveMod.Quicksave.Storage;

internal static class QuicksavePath {
    private static string? cachedQuicksavesRootFullPath;

    public static string QuicksavesRoot =>
        Path.Combine(Everest.PathGame, "Quicksaves");

    public static string QuicksavesRootFullPath =>
        cachedQuicksavesRootFullPath ??= Path.GetFullPath(QuicksavesRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string TempDirectory =>
        Path.Combine(QuicksavesRoot, QuicksaveConstants.TempFolderName);

    private static string TempDirectoryFullPath =>
        Path.GetFullPath(TempDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) {
        subdirectory = null;

        if (!TryGetPathUnderRoot(absolutePath, out string fullPath, out string relativePath)) {
            return false;
        }

        if (File.Exists(fullPath)) {
            relativePath = Path.GetDirectoryName(relativePath) ?? "";
        } else if (!Directory.Exists(fullPath)) {
            return Path.HasExtension(fullPath)
                && fullPath.EndsWith(QuicksaveConstants.Extension, StringComparison.OrdinalIgnoreCase)
                && TryGetRelativeSubdirectory(Path.GetDirectoryName(fullPath)!, out subdirectory);
        }

        relativePath = NormalizeRelativePath(relativePath);
        if (relativePath.Length == 0 || relativePath is ".") {
            return true;
        }

        subdirectory = relativePath;
        return true;
    }

    public static string ResolveQuicksaveFilePath(string path, bool mustExist) {
        string fullPath = Path.GetFullPath(path);
        if (!IsUnderQuicksavesRoot(fullPath)) {
            throw new ArgumentException("Path must stay within the Quicksaves folder.", nameof(path));
        }

        if (!fullPath.EndsWith(QuicksaveConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Quicksave path must point to a .qs file.", nameof(path));
        }

        if (mustExist && !File.Exists(fullPath)) {
            throw new FileNotFoundException($"Quicksave file not found: {path}");
        }

        return fullPath;
    }

    public static string ResolveSaveDirectory(string? subdirectory) {
        string root = QuicksavesRootFullPath;

        if (string.IsNullOrWhiteSpace(subdirectory)) {
            return root;
        }

        subdirectory = NormalizeRelativePath(subdirectory);

        if (subdirectory.Split(Path.DirectorySeparatorChar).Any(part => part is "." or "..")) {
            throw new ArgumentException("Invalid quicksave subdirectory.", nameof(subdirectory));
        }

        string resolved = Path.GetFullPath(Path.Combine(root, subdirectory));
        if (!IsUnderQuicksavesRoot(resolved)) {
            throw new ArgumentException(
                "Quicksave subdirectory must stay within the Quicksaves folder.",
                nameof(subdirectory)
            );
        }

        return resolved;
    }

    public static bool IsUnderQuicksavesRoot(string fullPath) {
        string root = QuicksavesRootFullPath;
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTempPlaybackPath(string path) {
        string fullPath = Path.GetFullPath(path);
        string tempRoot = TempDirectoryFullPath;

        if (!fullPath.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(tempRoot, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string fileName = Path.GetFileName(fullPath);
        return fileName.StartsWith(QuicksaveConstants.TempTasPrefix, StringComparison.Ordinal)
            && fileName.EndsWith(".tas", StringComparison.OrdinalIgnoreCase);
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

    public static string EnsureQuicksaveExtension(string fileName) {
        if (!fileName.EndsWith(QuicksaveConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            return fileName + QuicksaveConstants.Extension;
        }

        return fileName;
    }

    public static string ResolveDeletableDirectoryPath(string fullPath) {
        fullPath = Path.GetFullPath(fullPath);
        string root = QuicksavesRootFullPath;

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("The Quicksaves root folder cannot be deleted.");
        }

        if (!Directory.Exists(fullPath)) {
            throw new FileNotFoundException($"Quicksave folder not found: {fullPath}");
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
        string root = QuicksavesRootFullPath;

        if (!IsUnderQuicksavesRoot(fullPath)) {
            relativePath = "";
            return false;
        }

        relativePath = Path.GetRelativePath(root, fullPath);
        return true;
    }
}
