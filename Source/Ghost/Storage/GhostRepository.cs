using Celeste.Mod.QuicksaveMod.Storage;

namespace Celeste.Mod.QuicksaveMod.Ghost.Storage;

internal static class GhostRepository {
    private static readonly EntityRepository Repository = new(
        EntityStoreProfile.Ghost,
        new EntityPath(EntityStoreProfile.Ghost)
    );

    public static void MoveGhost(string sourcePath, string targetDirectory) =>
        Repository.Move(sourcePath, targetDirectory);

    public static void RenameGhost(string path, string newName) =>
        Repository.Rename(path, newName);

    public static void DeleteGhost(string path) =>
        Repository.Delete(path);

    public static void CreateGhostFolder(string folderName, string? parentSubdirectory = null) =>
        Repository.CreateFolder(folderName, parentSubdirectory);
}
