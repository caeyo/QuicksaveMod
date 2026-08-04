using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Ghost.Storage;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal static class GhostBrowserNavigation {
    public static string RootPath => GhostPath.GhostsRootFullPath;

    public static void EnsureRootExists() {
        Directory.CreateDirectory(GhostPath.GhostsRoot);
    }

    public static List<BrowserEntry> ListDirectory(string absolutePath) {
        absolutePath = Path.GetFullPath(absolutePath);

        List<BrowserEntry> entries = [];

        foreach (string directory in Directory.GetDirectories(absolutePath)) {
            string name = Path.GetFileName(directory);
            if (name.StartsWith('.')) {
                continue;
            }

            entries.Add(new BrowserEntry(name, directory, BrowserEntryKind.Folder));
        }

        foreach (string file in Directory.GetFiles(absolutePath, $"*{GhostConstants.Extension}")) {
            entries.Add(new BrowserEntry(Path.GetFileName(file), file, BrowserEntryKind.File));
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

        List<BrowserBreadcrumb> breadcrumbs = [new("Ghosts", root)];

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
        return GhostPath.TryGetRelativeSubdirectory(absolutePath, out string? subdirectory)
            ? subdirectory
            : null;
    }

    public static string DefaultSaveName() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

    public static string GetDisplayName(BrowserEntry entry) =>
        entry.Kind == BrowserEntryKind.Folder
            ? entry.Name
            : Path.GetFileNameWithoutExtension(entry.Name);

    public static string RenameDefaultText(BrowserEntry entry) => GetDisplayName(entry);
}

internal sealed class GhostBrowserState {
    public string CurrentDirectory { get; private set; } = GhostBrowserNavigation.RootPath;
    public List<BrowserEntry> Entries { get; } = [];
    public List<string> EntrySelectableIds { get; } = [];
    public List<string> EntryPopupIds { get; } = [];
    public List<BrowserBreadcrumb> Breadcrumbs { get; } = [];
    public List<string> BreadcrumbButtonIds { get; } = [];
    public int SelectedIndex { get; set; } = -1;
    public InlineEditMode EditMode { get; private set; }
    public string EditBuffer = "";
    public string? EditTargetPath { get; private set; }
    public string? PendingDeletePath { get; private set; }
    public string? PendingDeleteLabel { get; private set; }
    public bool ShowDeleteModal { get; private set; }
    public string? ConflictMessage { get; private set; }
    public bool ShowConflictModal { get; private set; }
    public bool FocusWindow { get; set; }
    public bool FocusEditField { get; set; }
    private PendingInlineEditRequest? PendingInlineEdit { get; set; }

    public void RefreshEntries() {
        Entries.Clear();
        Entries.AddRange(GhostBrowserNavigation.ListDirectory(CurrentDirectory));
        RebuildEntryIds();
        ClampSelection();
    }

    public void NavigateTo(string absolutePath) {
        CurrentDirectory = Path.GetFullPath(absolutePath);
        RebuildBreadcrumbs();
        RefreshEntries();
        SelectedIndex = Entries.Count > 0 ? 0 : -1;
        CancelInlineEdit();
    }

    public void NavigateUp() {
        if (!GhostBrowserNavigation.TryGetParentDirectory(CurrentDirectory, out string parentPath)) {
            return;
        }

        NavigateTo(parentPath);
    }

    public void EnsureBreadcrumbs() {
        if (Breadcrumbs.Count == 0) {
            RebuildBreadcrumbs();
        }
    }

    public BrowserEntry? SelectedEntry =>
        SelectedIndex >= 0 && SelectedIndex < Entries.Count ? Entries[SelectedIndex] : null;

    public void MoveSelection(int delta) {
        if (Entries.Count == 0) {
            SelectedIndex = -1;
            return;
        }

        SelectedIndex = Math.Clamp(
            SelectedIndex < 0 ? 0 : SelectedIndex + delta,
            0,
            Entries.Count - 1
        );
    }

    public void RequestInlineEdit(InlineEditMode mode, string defaultText, string? targetPath = null) {
        PendingInlineEdit = new PendingInlineEditRequest(mode, defaultText, targetPath);
    }

    public void ApplyPendingInlineEdit() {
        if (PendingInlineEdit is not { } pending) {
            return;
        }

        PendingInlineEdit = null;
        EditMode = pending.Mode;
        EditBuffer = pending.DefaultText;
        EditTargetPath = pending.TargetPath;
        FocusEditField = true;
    }

    public void CancelInlineEdit() {
        EditMode = InlineEditMode.None;
        EditBuffer = "";
        EditTargetPath = null;
        FocusEditField = false;
        PendingInlineEdit = null;
    }

    public void BeginDelete(string path, string label) {
        PendingDeletePath = path;
        PendingDeleteLabel = label;
        ShowDeleteModal = true;
    }

    public void CancelDelete() {
        PendingDeletePath = null;
        PendingDeleteLabel = null;
        ShowDeleteModal = false;
    }

    public void ShowConflict(string message) {
        ConflictMessage = message;
        ShowConflictModal = true;
    }

    public void ClearConflict() {
        ConflictMessage = null;
        ShowConflictModal = false;
    }

    public void ResetTransient() {
        CancelInlineEdit();
        CancelDelete();
        ClearConflict();
        PendingInlineEdit = null;
        FocusWindow = false;
        FocusEditField = false;
    }

    private void RebuildBreadcrumbs() {
        Breadcrumbs.Clear();
        BreadcrumbButtonIds.Clear();
        Breadcrumbs.AddRange(GhostBrowserNavigation.GetBreadcrumbs(CurrentDirectory));
        for (int i = 0; i < Breadcrumbs.Count; i++) {
            BreadcrumbButtonIds.Add($"{Breadcrumbs[i].Label}##ghost_crumb{i}");
        }
    }

    private void RebuildEntryIds() {
        EntrySelectableIds.Clear();
        EntryPopupIds.Clear();
        for (int i = 0; i < Entries.Count; i++) {
            BrowserEntry entry = Entries[i];
            string label = entry.Kind == BrowserEntryKind.Folder
                ? $"{entry.Name}/"
                : GhostBrowserNavigation.GetDisplayName(entry);
            EntrySelectableIds.Add($"{label}##ghost_entry{i}");
            EntryPopupIds.Add(entry.Kind == BrowserEntryKind.Folder ? $"ghost_folder_ctx_{i}" : $"ghost_file_ctx_{i}");
        }
    }

    private void ClampSelection() {
        if (Entries.Count == 0) {
            SelectedIndex = -1;
            return;
        }

        SelectedIndex = Math.Clamp(SelectedIndex, 0, Entries.Count - 1);
    }
}

internal sealed class GhostBrowserCommands(GhostBrowserState state, Action closeBrowser) {
    public void TryMoveFile(string sourcePath, string targetDirectory) {
        try {
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            if (sourceDirectory.Equals(Path.GetFullPath(targetDirectory), StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            GhostRepository.MoveGhost(
                sourcePath,
                GhostBrowserNavigation.GetRelativeSubdirectory(targetDirectory) ?? ""
            );
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to move ghost: {ex.Message}");
        }
    }

    public void RaceEntry(BrowserEntry entry) {
        closeBrowser();
        GhostService.LoadGhostForRace(entry.FullPath);
    }

    public void SpectateEntry(BrowserEntry entry) {
        closeBrowser();
        GhostService.LoadGhostForSpectate(entry.FullPath);
    }

    public void ConfirmInlineEdit() {
        string name = state.EditBuffer.Trim();
        if (name.Length == 0) {
            return;
        }

        try {
            switch (state.EditMode) {
                case InlineEditMode.Saving:
                    GhostService.SaveGhost(
                        name,
                        GhostBrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
                    );
                    break;
                case InlineEditMode.RenamingFile or InlineEditMode.RenamingFolder
                    when state.EditTargetPath is { } renamePath:
                    GhostRepository.RenameGhost(renamePath, name);
                    break;
                case InlineEditMode.CreatingFolder:
                    GhostRepository.CreateGhostFolder(
                        name,
                        GhostBrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
                    );
                    break;
                default:
                    return;
            }

            state.CancelInlineEdit();
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(GhostConstants.LogTag, $"Inline edit failed: {ex.Message}");
        }
    }

    public void ConfirmDelete() {
        if (state.PendingDeletePath == null) {
            state.CancelDelete();
            return;
        }

        try {
            GhostRepository.DeleteGhost(state.PendingDeletePath);
            state.CancelDelete();
            state.RefreshEntries();
        } catch (Exception ex) {
            state.CancelDelete();
            Logger.Warn(GhostConstants.LogTag, $"Failed to delete ghost: {ex.Message}");
        }
    }
}
