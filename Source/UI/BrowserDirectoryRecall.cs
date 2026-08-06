namespace Celeste.Mod.QuickTools.UI;

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
