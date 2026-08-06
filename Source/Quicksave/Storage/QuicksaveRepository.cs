using Celeste.Mod.QuicksaveMod.Storage;

namespace Celeste.Mod.QuicksaveMod.Quicksave.Storage;

internal static class QuicksaveRepository {
    private static readonly EntityRepository Repository = new(
        EntityStoreProfile.Quicksave,
        new EntityPath(EntityStoreProfile.Quicksave)
    );

    public static void MoveQuicksave(string sourcePath, string targetDirectory) =>
        Repository.Move(sourcePath, targetDirectory);

    public static void RenameQuicksave(string path, string newName) =>
        Repository.Rename(path, newName);

    public static void DeleteQuicksave(string path) =>
        Repository.Delete(path);

    public static void CreateQuicksaveFolder(string folderName, string? parentSubdirectory = null) =>
        Repository.CreateFolder(folderName, parentSubdirectory);
}
