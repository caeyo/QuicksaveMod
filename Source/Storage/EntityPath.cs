namespace Celeste.Mod.QuickTools.Storage;

internal sealed class EntityPath {
    private readonly EntityStoreProfile profile;
    private string? cachedRootFullPath;

    public EntityPath(EntityStoreProfile profile) {
        this.profile = profile;
    }

    public string Root =>
        Path.Combine(Everest.PathGame, profile.RootFolderName);

    public string RootFullPath =>
        cachedRootFullPath ??= Path.GetFullPath(Root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public string TempDirectory =>
        Path.Combine(Root, profile.TempFolderName);

    private string TempDirectoryFullPath =>
        Path.GetFullPath(TempDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) {
        subdirectory = null;

        if (!TryGetPathUnderRoot(absolutePath, out string fullPath, out string relativePath)) {
            return false;
        }

        if (File.Exists(fullPath)) {
            relativePath = Path.GetDirectoryName(relativePath) ?? "";
        } else if (!Directory.Exists(fullPath)) {
            return Path.HasExtension(fullPath)
                && fullPath.EndsWith(profile.Extension, StringComparison.OrdinalIgnoreCase)
                && TryGetRelativeSubdirectory(Path.GetDirectoryName(fullPath)!, out subdirectory);
        }

        relativePath = NormalizeRelativePath(relativePath);
        if (relativePath.Length == 0 || relativePath is ".") {
            return true;
        }

        subdirectory = relativePath;
        return true;
    }

    public string ResolveEntityFilePath(string path, bool mustExist) {
        string fullPath = Path.GetFullPath(path);
        if (!IsUnderRoot(fullPath)) {
            throw new ArgumentException(profile.OutsideRootMessage, nameof(path));
        }

        if (!fullPath.EndsWith(profile.Extension, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException(profile.InvalidFilePathMessage, nameof(path));
        }

        if (mustExist && !File.Exists(fullPath)) {
            throw new FileNotFoundException(string.Format(profile.FileNotFoundMessage, path));
        }

        return fullPath;
    }

    public string ResolveSaveDirectory(string? subdirectory) {
        string root = RootFullPath;

        if (string.IsNullOrWhiteSpace(subdirectory)) {
            return root;
        }

        subdirectory = NormalizeRelativePath(subdirectory);

        if (subdirectory.Split(Path.DirectorySeparatorChar).Any(part => part is "." or "..")) {
            throw new ArgumentException(profile.InvalidSubdirectoryMessage, nameof(subdirectory));
        }

        string resolved = Path.GetFullPath(Path.Combine(root, subdirectory));
        if (!IsUnderRoot(resolved)) {
            throw new ArgumentException(profile.SubdirectoryOutsideRootMessage, nameof(subdirectory));
        }

        return resolved;
    }

    public bool IsUnderRoot(string fullPath) {
        string root = RootFullPath;
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsTempPlaybackPath(string path) {
        string fullPath = Path.GetFullPath(path);
        string tempRoot = TempDirectoryFullPath;

        if (!fullPath.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(tempRoot, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string fileName = Path.GetFileName(fullPath);
        return fileName.StartsWith(profile.TempTasPrefix, StringComparison.Ordinal)
            && fileName.EndsWith(".tas", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeRelativePath(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

    public void ValidateSingleName(string name, string paramName) {
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

    public void ValidateCreatableFolderName(string folderName) {
        if (folderName.StartsWith('.')) {
            throw new ArgumentException("Folder name must not start with '.'.", nameof(folderName));
        }
    }

    public string EnsureExtension(string fileName) {
        if (!fileName.EndsWith(profile.Extension, StringComparison.OrdinalIgnoreCase)) {
            return fileName + profile.Extension;
        }

        return fileName;
    }

    public string ResolveDeletableDirectoryPath(string fullPath) {
        fullPath = Path.GetFullPath(fullPath);
        string root = RootFullPath;

        if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(profile.RootCannotDeleteMessage);
        }

        if (!Directory.Exists(fullPath)) {
            throw new FileNotFoundException(string.Format(profile.FolderNotFoundMessage, fullPath));
        }

        string folderName = Path.GetFileName(
            fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        if (folderName.StartsWith('.')) {
            throw new InvalidOperationException($"The folder '{folderName}' cannot be deleted.");
        }

        return fullPath;
    }

    private bool TryGetPathUnderRoot(string path, out string fullPath, out string relativePath) {
        fullPath = Path.GetFullPath(path);
        string root = RootFullPath;

        if (!IsUnderRoot(fullPath)) {
            relativePath = "";
            return false;
        }

        relativePath = Path.GetRelativePath(root, fullPath);
        return true;
    }
}
