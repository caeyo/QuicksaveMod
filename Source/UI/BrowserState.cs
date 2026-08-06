namespace Celeste.Mod.QuickTools.UI;

internal sealed class BrowserState(
    BrowserProfile profile
) {
    public string CurrentDirectory { get; private set; } = BrowserNavigation.RootPath(profile);

    internal readonly BrowserDirectoryRecall DirectoryRecall = new();

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
        Entries.AddRange(BrowserNavigation.ListDirectory(profile, CurrentDirectory));
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
        if (!BrowserNavigation.TryGetParentDirectory(profile, CurrentDirectory, out string parentPath)) {
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

        if (SelectedIndex < 0) {
            SelectedIndex = 0;
            return;
        }

        SelectedIndex = NormalizeSelection(SelectedIndex + delta);
    }

    public void RequestInlineEdit(InlineEditMode mode, string defaultText, string? targetPath = null) {
        PendingInlineEdit = new PendingInlineEditRequest(mode, defaultText, targetPath);
    }

    public void ApplyPendingInlineEdit() {
        if (PendingInlineEdit is not { } pending) {
            return;
        }

        PendingInlineEdit = null;
        BeginInlineEdit(pending.Mode, pending.DefaultText, pending.TargetPath);
    }

    private void BeginInlineEdit(InlineEditMode mode, string defaultText, string? targetPath = null) {
        EditMode = mode;
        EditBuffer = defaultText;
        EditTargetPath = targetPath;
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
        Breadcrumbs.AddRange(BrowserNavigation.GetBreadcrumbs(profile, CurrentDirectory));
        string crumbPrefix = profile.IdPrefix.Length == 0 ? "crumb" : $"{profile.IdPrefix}crumb";
        for (int i = 0; i < Breadcrumbs.Count; i++) {
            BreadcrumbButtonIds.Add($"{Breadcrumbs[i].Label}##{crumbPrefix}{i}");
        }
    }

    private void RebuildEntryIds() {
        EntrySelectableIds.Clear();
        EntryPopupIds.Clear();
        string entryPrefix = profile.IdPrefix.Length == 0 ? "entry" : $"{profile.IdPrefix}entry";
        string folderCtxPrefix = profile.IdPrefix.Length == 0 ? "folder_ctx" : $"{profile.IdPrefix}folder_ctx";
        string fileCtxPrefix = profile.IdPrefix.Length == 0 ? "file_ctx" : $"{profile.IdPrefix}file_ctx";

        for (int i = 0; i < Entries.Count; i++) {
            BrowserEntry entry = Entries[i];
            string label = entry.Kind == BrowserEntryKind.Folder
                ? $"{entry.Name}/"
                : BrowserNavigation.GetDisplayName(entry);
            EntrySelectableIds.Add($"{label}##{entryPrefix}{i}");
            EntryPopupIds.Add(
                entry.Kind == BrowserEntryKind.Folder
                    ? $"{folderCtxPrefix}_{i}"
                    : $"{fileCtxPrefix}_{i}"
            );
        }
    }

    private void ClampSelection() {
        SelectedIndex = NormalizeSelection(SelectedIndex);
    }

    private int NormalizeSelection(int index) =>
        Entries.Count == 0 ? -1 : Math.Clamp(index, 0, Entries.Count - 1);
}
