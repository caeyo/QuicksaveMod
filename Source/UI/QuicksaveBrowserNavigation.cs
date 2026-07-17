using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.UI;

public enum QuicksaveBrowserEntryKind {
    Folder,
    File,
}

public readonly record struct QuicksaveBrowserEntry(
    string Name,
    string FullPath,
    QuicksaveBrowserEntryKind Kind
);

public readonly record struct QuicksaveBrowserBreadcrumb(
    string Label,
    string AbsolutePath
);

public static class QuicksaveBrowserNavigation {
    public static string RootPath => QuicksaveService.QuicksavesRootFullPath;

    public static void EnsureRootExists() {
        Directory.CreateDirectory(RootPath);
    }

    public static List<QuicksaveBrowserEntry> ListDirectory(string absolutePath) {
        absolutePath = Path.GetFullPath(absolutePath);

        var entries = new List<QuicksaveBrowserEntry>();

        foreach (string directory in Directory.GetDirectories(absolutePath)) {
            string name = Path.GetFileName(directory);
            if (ShouldHideName(name)) {
                continue;
            }

            entries.Add(new QuicksaveBrowserEntry(name, directory, QuicksaveBrowserEntryKind.Folder));
        }

        foreach (string file in Directory.GetFiles(absolutePath, "*.qs")) {
            string name = Path.GetFileName(file);
            entries.Add(new QuicksaveBrowserEntry(name, file, QuicksaveBrowserEntryKind.File));
        }

        entries.Sort(static (left, right) => {
            int kindCompare = left.Kind.CompareTo(right.Kind);
            return kindCompare != 0
                ? kindCompare
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });

        return entries;
    }

    public static List<QuicksaveBrowserBreadcrumb> GetBreadcrumbs(string currentPath) {
        currentPath = Path.GetFullPath(currentPath);
        string root = RootPath;

        var breadcrumbs = new List<QuicksaveBrowserBreadcrumb> {
            new("Quicksaves", root),
        };

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
            breadcrumbs.Add(new QuicksaveBrowserBreadcrumb(segment, accumulated));
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
        return QuicksaveService.TryGetRelativeSubdirectory(absolutePath, out string? subdirectory)
            ? subdirectory
            : null;
    }

    public static string DefaultSaveName() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

    private static bool ShouldHideName(string name) => name.StartsWith('.');

    public static string GetDisplayName(QuicksaveBrowserEntry entry) {
        if (entry.Kind == QuicksaveBrowserEntryKind.Folder) {
            return entry.Name;
        }

        return Path.GetFileNameWithoutExtension(entry.Name);
    }

    public static string RenameDefaultText(QuicksaveBrowserEntry entry) => GetDisplayName(entry);
}
