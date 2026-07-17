namespace Celeste.Mod.QuicksaveMod.UI;

internal enum InlineEditMode {
    None,
    Saving,
    SavingTo,
    RenamingFile,
    RenamingFolder,
    CreatingFolder,
}

internal readonly record struct PendingInlineEditRequest(
    InlineEditMode Mode,
    string DefaultText,
    string? TargetPath
);

internal sealed class BrowserState {
    public string CurrentDirectory { get; set; } = BrowserNavigation.RootPath;

    public List<BrowserEntry> Entries { get; } = [];

    // Rebuilt alongside Entries by RefreshEntries.
    public List<string> EntrySelectableIds { get; } = [];
    public List<string> EntryPopupIds { get; } = [];

    public List<BrowserBreadcrumb> Breadcrumbs { get; } = [];

    public List<string> BreadcrumbButtonIds { get; } = [];

    public int SelectedIndex { get; set; } = -1;

    public InlineEditMode EditMode { get; set; }

    public string EditBuffer = "";

    public string? EditTargetPath { get; set; }

    public string? PendingDeletePath { get; set; }

    public string? PendingDeleteLabel { get; set; }

    public bool ShowDeleteModal { get; set; }

    public string? ConflictMessage { get; set; }

    public bool ShowConflictModal { get; set; }

    public bool FocusWindow { get; set; }

    public bool FocusEditField { get; set; }

    public PendingInlineEditRequest? PendingInlineEdit { get; set; }

    public void RefreshEntries() {
        Entries.Clear();
        Entries.AddRange(BrowserNavigation.ListDirectory(CurrentDirectory));
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
        if (!BrowserNavigation.TryGetParentDirectory(CurrentDirectory, out string parentPath)) {
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

    public void ClampSelection() {
        SelectedIndex = NormalizeSelection(SelectedIndex);
    }

    private int NormalizeSelection(int index) =>
        Entries.Count == 0 ? -1 : Math.Clamp(index, 0, Entries.Count - 1);

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

    public void BeginInlineEdit(InlineEditMode mode, string defaultText, string? targetPath = null) {
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
        Breadcrumbs.AddRange(BrowserNavigation.GetBreadcrumbs(CurrentDirectory));
        for (int i = 0; i < Breadcrumbs.Count; i++) {
            BreadcrumbButtonIds.Add($"{Breadcrumbs[i].Label}##crumb{i}");
        }
    }

    private void RebuildEntryIds() {
        EntrySelectableIds.Clear();
        EntryPopupIds.Clear();
        for (int i = 0; i < Entries.Count; i++) {
            BrowserEntry entry = Entries[i];
            string label = entry.Kind == BrowserEntryKind.Folder
                ? $"{entry.Name}/"
                : BrowserNavigation.GetDisplayName(entry);
            EntrySelectableIds.Add($"{label}##entry{i}");
            EntryPopupIds.Add(
                entry.Kind == BrowserEntryKind.Folder
                    ? $"folder_ctx_{i}"
                    : $"file_ctx_{i}"
            );
        }
    }
}
