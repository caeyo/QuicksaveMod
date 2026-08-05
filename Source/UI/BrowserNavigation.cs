using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;

namespace Celeste.Mod.QuicksaveMod.UI;

internal enum BrowserEntryKind {
    Folder,
    File,
}

internal readonly record struct BrowserEntry(
    string Name,
    string FullPath,
    BrowserEntryKind Kind
);

internal readonly record struct BrowserBreadcrumb(
    string Label,
    string AbsolutePath
);

internal static class BrowserNavigation {
    public static string RootPath => QuicksavePath.QuicksavesRootFullPath;

    public static void EnsureRootExists() {
        Directory.CreateDirectory(QuicksavePath.QuicksavesRoot);
    }

    public static List<BrowserEntry> ListDirectory(string absolutePath) {
        absolutePath = Path.GetFullPath(absolutePath);

        List<BrowserEntry> entries = new();

        foreach (string directory in Directory.GetDirectories(absolutePath)) {
            string name = Path.GetFileName(directory);
            if (ShouldHideName(name)) {
                continue;
            }

            entries.Add(new BrowserEntry(name, directory, BrowserEntryKind.Folder));
        }

        foreach (string file in Directory.GetFiles(absolutePath, $"*{QuicksaveConstants.Extension}")) {
            string name = Path.GetFileName(file);
            entries.Add(new BrowserEntry(name, file, BrowserEntryKind.File));
        }

        entries.Sort(static (left, right) => {
            int kindCompare = left.Kind.CompareTo(right.Kind);
            return kindCompare != 0
                ? kindCompare
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });

        return entries;
    }

    public static List<BrowserBreadcrumb> GetBreadcrumbs(string currentPath) {
        currentPath = Path.GetFullPath(currentPath);
        string root = RootPath;

        List<BrowserBreadcrumb> breadcrumbs = [
            new("Quicksaves", root),
        ];

        if (currentPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return breadcrumbs;
        }

        string relative = Path.GetRelativePath(root, currentPath);
        string accumulated = root;

        foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) {
            if (string.IsNullOrEmpty(segment)) {
                continue;
            }

            accumulated = Path.Combine(accumulated, segment);
            breadcrumbs.Add(new BrowserBreadcrumb(segment, accumulated));
        }

        return breadcrumbs;
    }

    public static bool TryGetParentDirectory(string currentPath, out string parentPath) {
        currentPath = Path.GetFullPath(currentPath);
        string root = RootPath;

        if (currentPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            parentPath = root;
            return false;
        }

        parentPath = Path.GetFullPath(Path.Combine(currentPath, ".."));
        return true;
    }

    public static bool IsRootDirectory(string currentPath) =>
        Path.GetFullPath(currentPath).Equals(RootPath, StringComparison.OrdinalIgnoreCase);

    public static string? GetRelativeSubdirectory(string absolutePath) {
        return QuicksavePath.TryGetRelativeSubdirectory(absolutePath, out string? subdirectory)
            ? subdirectory
            : null;
    }

    public static string DefaultSaveName() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

    private static bool ShouldHideName(string name) => name.StartsWith('.');

    public static string GetDisplayName(BrowserEntry entry) {
        if (entry.Kind == BrowserEntryKind.Folder) {
            return entry.Name;
        }

        return Path.GetFileNameWithoutExtension(entry.Name);
    }

    public static string RenameDefaultText(BrowserEntry entry) => GetDisplayName(entry);
}

internal sealed class BrowserDirectoryRecall {
    private string? remembered;

    public void Remember(string currentDirectory) {
        remembered = currentDirectory;
    }

    public string ResolveOpenDirectory(string rootPath) {
        string root = Path.GetFullPath(rootPath);

        if (string.IsNullOrEmpty(remembered) || !Directory.Exists(remembered)) {
            return root;
        }

        string full = Path.GetFullPath(remembered);
        if (full.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            return root;
        }

        string relative = Path.GetRelativePath(root, full);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) {
            return root;
        }

        return full;
    }
}
