namespace Celeste.Mod.QuickTools.Storage;

internal sealed class EntityRepository {
    private readonly EntityStoreProfile profile;
    private readonly EntityPath path;

    public EntityRepository(EntityStoreProfile profile, EntityPath path) {
        this.profile = profile;
        this.path = path;
    }

    public void Move(string sourcePath, string targetDirectory) {
        string source = path.ResolveEntityFilePath(sourcePath, mustExist: true);
        string targetDir = path.ResolveSaveDirectory(targetDirectory);
        Directory.CreateDirectory(targetDir);

        string destination = Path.Combine(targetDir, Path.GetFileName(source));
        if (File.Exists(destination)) {
            throw new IOException(profile.MoveConflictMessage(destination));
        }

        File.Move(source, destination);
    }

    public void Rename(string pathValue, string newName) {
        if (string.IsNullOrWhiteSpace(newName)) {
            throw new ArgumentException("New name must not be empty.", nameof(newName));
        }

        path.ValidateSingleName(newName, nameof(newName));

        string fullPath = Path.GetFullPath(pathValue);
        if (!path.IsUnderRoot(fullPath)) {
            throw new ArgumentException(profile.OutsideRootMessage, nameof(pathValue));
        }

        if (File.Exists(fullPath)) {
            RenameFile(fullPath, newName);
            return;
        }

        if (Directory.Exists(fullPath)) {
            RenameDirectory(fullPath, newName);
            return;
        }

        throw new FileNotFoundException(profile.PathNotFoundMessage(pathValue));
    }

    public void Delete(string pathValue) {
        string fullPath = Path.GetFullPath(pathValue);
        if (!path.IsUnderRoot(fullPath)) {
            throw new ArgumentException(profile.OutsideRootMessage, nameof(pathValue));
        }

        if (File.Exists(fullPath)) {
            if (!fullPath.EndsWith(profile.Extension, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException(profile.InvalidFilePathMessage, nameof(pathValue));
            }

            File.Delete(fullPath);
            return;
        }

        if (Directory.Exists(fullPath)) {
            Directory.Delete(path.ResolveDeletableDirectoryPath(fullPath), recursive: true);
            return;
        }

        throw new FileNotFoundException(profile.PathNotFoundMessage(pathValue));
    }

    public void CreateFolder(string folderName, string? parentSubdirectory = null) {
        if (string.IsNullOrWhiteSpace(folderName)) {
            throw new ArgumentException("Folder name must not be empty.", nameof(folderName));
        }

        path.ValidateSingleName(folderName, nameof(folderName));
        path.ValidateCreatableFolderName(folderName);

        string parentDirectory = path.ResolveSaveDirectory(parentSubdirectory);
        string fullPath = Path.Combine(parentDirectory, folderName);

        if (Directory.Exists(fullPath)) {
            throw new IOException(profile.CreateFolderConflictMessage(fullPath));
        }

        Directory.CreateDirectory(fullPath);
    }

    private void RenameFile(string fullPath, string newName) {
        string renamed = path.EnsureExtension(newName);
        path.ValidateSingleName(renamed, nameof(newName));

        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, renamed);
        if (File.Exists(destination)) {
            throw new IOException(profile.RenameFileConflictMessage(destination));
        }

        File.Move(fullPath, destination);
    }

    private void RenameDirectory(string fullPath, string newName) {
        string destination = Path.Combine(Path.GetDirectoryName(fullPath)!, newName);
        if (Directory.Exists(destination)) {
            throw new IOException(profile.RenameFolderConflictMessage(destination));
        }

        Directory.Move(fullPath, destination);
    }
}
