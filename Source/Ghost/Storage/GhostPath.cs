using Celeste.Mod.QuicksaveMod.Storage;

namespace Celeste.Mod.QuicksaveMod.Ghost.Storage;

internal static class GhostPath {
    private static readonly EntityPath Path = new(EntityStoreProfile.Ghost);

    public static string GhostsRoot => Path.Root;

    public static string GhostsRootFullPath => Path.RootFullPath;

    public static string TempDirectory => Path.TempDirectory;

    public static bool IsTempPlaybackPath(string path) =>
        Path.IsTempPlaybackPath(path);

    public static bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) =>
        Path.TryGetRelativeSubdirectory(absolutePath, out subdirectory);

    public static string ResolveGhostFilePath(string path, bool mustExist) =>
        Path.ResolveEntityFilePath(path, mustExist);

    public static string ResolveSaveDirectory(string? subdirectory) =>
        Path.ResolveSaveDirectory(subdirectory);

    public static bool IsUnderGhostsRoot(string fullPath) =>
        Path.IsUnderRoot(fullPath);

    public static void ValidateSingleName(string name, string paramName) =>
        Path.ValidateSingleName(name, paramName);

    public static void ValidateCreatableFolderName(string folderName) =>
        Path.ValidateCreatableFolderName(folderName);

    public static string EnsureGhostExtension(string fileName) =>
        Path.EnsureExtension(fileName);

    public static string ResolveDeletableDirectoryPath(string fullPath) =>
        Path.ResolveDeletableDirectoryPath(fullPath);
}
