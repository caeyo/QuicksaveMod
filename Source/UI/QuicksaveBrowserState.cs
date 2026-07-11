namespace Celeste.Mod.QuicksaveMod.UI;

public enum InlineEditMode {
    None,
    Saving,
    SavingTo,
    RenamingFile,
    RenamingFolder,
    CreatingFolder,
}

public readonly record struct PendingInlineEditRequest(
    InlineEditMode Mode,
    string DefaultText,
    string? TargetPath
);

public sealed class QuicksaveBrowserState {
    public string CurrentDirectory { get; set; } = QuicksaveBrowserNavigation.RootPath;

    public List<QuicksaveBrowserEntry> Entries { get; } = [];

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
        Entries.AddRange(QuicksaveBrowserNavigation.ListDirectory(CurrentDirectory));
        ClampSelection();
    }

    public void NavigateTo(string absolutePath) {
        CurrentDirectory = Path.GetFullPath(absolutePath);
        RefreshEntries();
        SelectedIndex = Entries.Count > 0 ? 0 : -1;
        CancelInlineEdit();
    }

    public void NavigateUp() {
        if (!QuicksaveBrowserNavigation.TryGetParentDirectory(CurrentDirectory, out string parentPath)) {
            return;
        }

        NavigateTo(parentPath);
    }

    public QuicksaveBrowserEntry? SelectedEntry =>
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
}
