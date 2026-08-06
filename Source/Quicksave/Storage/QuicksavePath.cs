using Celeste.Mod.QuickTools.Storage;

namespace Celeste.Mod.QuickTools.Quicksave.Storage;

internal static class QuicksavePath {
    private static readonly EntityPath Path = new(EntityStoreProfile.Quicksave);

    public static string QuicksavesRoot => Path.Root;

    public static string QuicksavesRootFullPath => Path.RootFullPath;

    public static string TempDirectory => Path.TempDirectory;

    public static bool TryGetRelativeSubdirectory(string absolutePath, out string? subdirectory) =>
        Path.TryGetRelativeSubdirectory(absolutePath, out subdirectory);

    public static string ResolveQuicksaveFilePath(string path, bool mustExist) =>
        Path.ResolveEntityFilePath(path, mustExist);

    public static string ResolveSaveDirectory(string? subdirectory) =>
        Path.ResolveSaveDirectory(subdirectory);

    public static bool IsUnderQuicksavesRoot(string fullPath) =>
        Path.IsUnderRoot(fullPath);

    public static bool IsTempPlaybackPath(string path) =>
        Path.IsTempPlaybackPath(path);

    public static void ValidateSingleName(string name, string paramName) =>
        Path.ValidateSingleName(name, paramName);

    public static void ValidateCreatableFolderName(string folderName) =>
        Path.ValidateCreatableFolderName(folderName);

    public static string EnsureQuicksaveExtension(string fileName) =>
        Path.EnsureExtension(fileName);

    public static string ResolveDeletableDirectoryPath(string fullPath) =>
        Path.ResolveDeletableDirectoryPath(fullPath);
}
