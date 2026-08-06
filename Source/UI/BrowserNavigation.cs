using Celeste.Mod.QuicksaveMod.Storage;

namespace Celeste.Mod.QuicksaveMod.UI;

internal static class BrowserNavigation {
    public static string RootPath(BrowserProfile profile) => profile.Path.RootFullPath;

    public static void EnsureRootExists(BrowserProfile profile) {
        Directory.CreateDirectory(profile.Path.Root);
    }

    public static List<BrowserEntry> ListDirectory(BrowserProfile profile, string absolutePath) {
        absolutePath = Path.GetFullPath(absolutePath);

        List<BrowserEntry> entries = [];

        foreach (string directory in Directory.GetDirectories(absolutePath)) {
            string name = Path.GetFileName(directory);
            if (ShouldHideName(name)) {
                continue;
            }

            entries.Add(new BrowserEntry(name, directory, BrowserEntryKind.Folder));
        }

        foreach (string file in Directory.GetFiles(absolutePath, $"*{profile.Store.Extension}")) {
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

    public static List<BrowserBreadcrumb> GetBreadcrumbs(BrowserProfile profile, string currentPath) {
        currentPath = Path.GetFullPath(currentPath);
        string root = RootPath(profile);

        List<BrowserBreadcrumb> breadcrumbs = [new(profile.RootLabel, root)];

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

    public static bool TryGetParentDirectory(BrowserProfile profile, string currentPath, out string parentPath) {
        currentPath = Path.GetFullPath(currentPath);
        string root = RootPath(profile);

        if (currentPath.Equals(root, StringComparison.OrdinalIgnoreCase)) {
            parentPath = root;
            return false;
        }

        parentPath = Path.GetFullPath(Path.Combine(currentPath, ".."));
        return true;
    }

    public static bool IsRootDirectory(BrowserProfile profile, string currentPath) =>
        Path.GetFullPath(currentPath).Equals(RootPath(profile), StringComparison.OrdinalIgnoreCase);

    public static string? GetRelativeSubdirectory(BrowserProfile profile, string absolutePath) {
        return profile.Path.TryGetRelativeSubdirectory(absolutePath, out string? subdirectory)
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
