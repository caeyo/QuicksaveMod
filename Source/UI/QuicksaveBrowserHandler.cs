using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

public sealed class QuicksaveBrowserHandler : ImGuiHandler {
    internal static QuicksaveBrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private const string WindowId = "Quicksave Browser";
    private const string DragPayloadType = "QS_FILE";
    private const string DeleteModalId = "Quicksave Confirm Delete";
    private const string ConflictModalId = "Quicksave Conflict";

    private readonly QuicksaveBrowserState state = new();
    private bool savedMouseVisible;
    private bool appliedFreeze;
    private bool deletePopupOpened;
    private bool conflictPopupOpened;
    private string? dragSourcePath;

    public QuicksaveBrowserHandler() {
        Instance = this;
        Visible = false;
    }

    public override void Update(GameTime gameTime) {
        if (Visible) {
            return;
        }

        if (!QuicksaveModModule.Settings.OpenBrowser.Pressed) {
            return;
        }

        QuicksaveModModule.Settings.OpenBrowser.ConsumePress();
        QuicksaveModModule.Settings.OpenBrowser.ConsumeBuffer();
        Open();
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        if (state.FocusWindow) {
            ImGui.SetNextWindowFocus();
            state.FocusWindow = false;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(520, 420), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin(WindowId, ImGuiWindowFlags.NoDocking)) {
            ImGui.End();
            RenderModals();
            return;
        }

        state.ApplyPendingInlineEdit();

        HandleWindowKeyboardShortcuts();

        RenderBreadcrumbs();
        ImGui.Separator();
        RenderEntryList();
        RenderInlineEditArea();
        ClearCancelledDragSource();

        ImGui.End();
        RenderModals();
    }

    private void RenderModals() {
        RenderDeleteModal();
        RenderConflictModal();
    }

    private bool TryBeginModal(string popupId, bool shouldShow, ref bool popupOpened, Action onDismiss) {
        if (!shouldShow) {
            popupOpened = false;
            return false;
        }

        if (!popupOpened) {
            ImGui.OpenPopup(popupId);
            popupOpened = true;
        }

        ImGui.SetNextWindowPos(
            ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Always,
            new System.Numerics.Vector2(0.5f, 0.5f)
        );

        bool open = true;
        if (!ImGui.BeginPopupModal(popupId, ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (!open) {
                popupOpened = false;
                onDismiss();
            }

            return false;
        }

        return true;
    }

    public void Open() {
        QuicksaveBrowserNavigation.EnsureRootExists();

        state.CurrentDirectory = QuicksaveBrowserNavigation.RootPath;
        state.RefreshEntries();
        state.SelectedIndex = state.Entries.Count > 0 ? 0 : -1;
        state.ResetTransient();
        state.FocusWindow = true;

        savedMouseVisible = Engine.Instance.IsMouseVisible;
        Engine.Instance.IsMouseVisible = true;

        appliedFreeze = false;
        if (Engine.Scene is Level level) {
            if (!level.Paused && !level.Frozen) {
                level.Frozen = true;
                appliedFreeze = true;
            }
        }

        QuicksaveService.SuspendTracking();
        Visible = true;
    }

    public void Close() {
        if (!Visible) {
            return;
        }

        Visible = false;
        state.ResetTransient();
        deletePopupOpened = false;
        conflictPopupOpened = false;
        dragSourcePath = null;

        if (appliedFreeze && Engine.Scene is Level level) {
            level.Frozen = false;
        }

        appliedFreeze = false;
        QuicksaveService.ResumeTracking();
        Engine.Instance.IsMouseVisible = savedMouseVisible;
    }

    internal void OnAfterInputUpdate() {
        if (!Visible) {
            return;
        }

        if (Input.ESC.Pressed) {
            if (!TryCancelSubOperationOnEscape()) {
                Close();
            }
        }

        ConsumeUnderlyingInput();
    }

    private bool TryCancelSubOperationOnEscape() {
        if (state.ShowDeleteModal) {
            state.CancelDelete();
            deletePopupOpened = false;
            return true;
        }

        if (state.ShowConflictModal) {
            state.ClearConflict();
            conflictPopupOpened = false;
            return true;
        }

        if (state.EditMode != InlineEditMode.None) {
            state.CancelInlineEdit();
            return true;
        }

        return false;
    }

    private static void ConsumeUnderlyingInput() {
        foreach (VirtualInput input in MInput.VirtualInputs) {
            if (input is VirtualButton button) {
                button.ConsumePress();
                button.ConsumeBuffer();
            }
        }

        MInput.Disabled = true;
        MInput.Active = false;
    }

    private void HandleWindowKeyboardShortcuts() {
        if (ImGui.IsAnyItemActive()) {
            return;
        }

        if (state.EditMode != InlineEditMode.None || state.ShowDeleteModal || state.ShowConflictModal) {
            return;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Backspace)
            && !QuicksaveBrowserNavigation.IsRootDirectory(state.CurrentDirectory)) {
            state.NavigateUp();
            return;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
            state.MoveSelection(-1);
        } else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
            state.MoveSelection(1);
        } else if (ImGui.IsKeyPressed(ImGuiKey.Enter)) {
            ActivateSelectedEntry();
        }
    }

    private static float InlineEditAreaHeight =>
        ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;

    private void RenderBreadcrumbs() {
        var breadcrumbs = QuicksaveBrowserNavigation.GetBreadcrumbs(state.CurrentDirectory);

        for (int i = 0; i < breadcrumbs.Count; i++) {
            if (i > 0) {
                ImGui.SameLine();
                ImGui.TextUnformatted(" / ");
                ImGui.SameLine();
            }

            var crumb = breadcrumbs[i];
            if (ImGui.SmallButton($"{crumb.Label}##crumb{i}")) {
                state.NavigateTo(crumb.AbsolutePath);
            }

            RenderDirectoryDropTarget(crumb.AbsolutePath);
        }
    }

    private void RenderEntryList() {
        float listHeight = -(InlineEditAreaHeight + ImGui.GetStyle().ItemSpacing.Y);

        if (ImGui.BeginChild("QuicksaveEntryList", new System.Numerics.Vector2(0, listHeight))) {
            if (state.Entries.Count == 0) {
                ImGui.TextUnformatted("(empty)");
            }

            for (int i = 0; i < state.Entries.Count; i++) {
                var entry = state.Entries[i];
                bool selected = i == state.SelectedIndex;
                string label = entry.Kind == QuicksaveBrowserEntryKind.Folder
                    ? $"{entry.Name}/"
                    : QuicksaveBrowserNavigation.GetDisplayName(entry);

                if (ImGui.Selectable($"{label}##entry{i}", selected, ImGuiSelectableFlags.AllowDoubleClick)) {
                    state.SelectedIndex = i;

                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                        ActivateEntry(entry);
                    }
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                    state.SelectedIndex = i;
                }

                string popupId = entry.Kind == QuicksaveBrowserEntryKind.Folder
                    ? $"folder_ctx_{i}"
                    : $"file_ctx_{i}";
                ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonRight);
                RenderEntryContextMenu(entry, popupId);

                if (entry.Kind == QuicksaveBrowserEntryKind.File) {
                    RenderFileDragSource(entry);
                }

                if (entry.Kind == QuicksaveBrowserEntryKind.Folder) {
                    RenderDirectoryDropTarget(entry.FullPath);
                }

                if (selected && state.SelectedIndex == i && ImGui.IsWindowFocused()) {
                    ImGui.SetScrollHereY(0.5f);
                }
            }

            RenderEmptySpaceContextMenu();
            ImGui.EndChild();
        }
    }

    private void RenderInlineEditArea() {
        ImGui.BeginChild(
            "QuicksaveInlineEdit",
            new System.Numerics.Vector2(0, InlineEditAreaHeight),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.NoScrollbar
        );

        if (state.EditMode != InlineEditMode.None) {
            string prompt = state.EditMode switch {
                InlineEditMode.Saving => "Save as:",
                InlineEditMode.SavingTo => "Save to folder as:",
                InlineEditMode.RenamingFile => "Rename file:",
                InlineEditMode.RenamingFolder => "Rename folder:",
                InlineEditMode.CreatingFolder => "New folder:",
                _ => "Name:",
            };

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(prompt);
            ImGui.SameLine();

            if (state.FocusEditField) {
                ImGui.SetKeyboardFocusHere();
                state.FocusEditField = false;
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##inline_edit", ref state.EditBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
                ConfirmInlineEdit();
            }
        }

        ImGui.EndChild();
    }

    private void RenderEmptySpaceContextMenu() {
        if (!ImGui.BeginPopupContextWindow("empty_ctx", ImGuiPopupFlags.NoOpenOverItems | ImGuiPopupFlags.MouseButtonRight)) {
            return;
        }

        if (QuicksaveService.IsTracking && ImGui.MenuItem("Save")) {
            state.RequestInlineEdit(InlineEditMode.Saving, QuicksaveBrowserNavigation.DefaultSaveName());
        }

        if (ImGui.MenuItem("New Folder")) {
            state.RequestInlineEdit(InlineEditMode.CreatingFolder, "New Folder");
        }

        ImGui.EndPopup();
    }

    private void RenderEntryContextMenu(QuicksaveBrowserEntry entry, string popupId) {
        if (!ImGui.BeginPopup(popupId)) {
            return;
        }

        if (entry.Kind == QuicksaveBrowserEntryKind.File) {
            if (ImGui.MenuItem("Load")) {
                LoadEntry(entry);
            }

            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFile,
                    QuicksaveBrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, QuicksaveBrowserNavigation.GetDisplayName(entry));
            }
        } else {
            if (QuicksaveService.IsTracking && ImGui.MenuItem("Save To")) {
                state.RequestInlineEdit(
                    InlineEditMode.SavingTo,
                    QuicksaveBrowserNavigation.DefaultSaveName(),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFolder,
                    QuicksaveBrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, QuicksaveBrowserNavigation.GetDisplayName(entry));
            }
        }

        ImGui.EndPopup();
    }

    private void RenderFileDragSource(QuicksaveBrowserEntry entry) {
        if (!ImGui.BeginDragDropSource()) {
            return;
        }

        dragSourcePath = entry.FullPath;
        ImGui.SetDragDropPayload(DragPayloadType, IntPtr.Zero, 0);
        ImGui.TextUnformatted($"Move {QuicksaveBrowserNavigation.GetDisplayName(entry)}");
        ImGui.EndDragDropSource();
    }

    private void RenderDirectoryDropTarget(string targetDirectory) {
        if (!ImGui.BeginDragDropTarget()) {
            return;
        }

        ImGui.AcceptDragDropPayload(DragPayloadType);

        if (dragSourcePath != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
            TryMoveFile(dragSourcePath, targetDirectory);
            dragSourcePath = null;
        }

        ImGui.EndDragDropTarget();
    }

    private void ClearCancelledDragSource() {
        if (dragSourcePath == null || ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
            return;
        }

        dragSourcePath = null;
    }

    private void TryMoveFile(string sourcePath, string targetDirectory) {
        try {
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            string target = Path.GetFullPath(targetDirectory);

            if (sourceDirectory.Equals(target, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            QuicksaveService.MoveQuicksave(
                sourcePath,
                QuicksaveBrowserNavigation.GetRelativeSubdirectory(target) ?? ""
            );
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(nameof(QuicksaveBrowserHandler), $"Failed to move quicksave: {ex.Message}");
        }
    }

    private void ActivateSelectedEntry() {
        if (state.SelectedEntry is { } entry) {
            ActivateEntry(entry);
        }
    }

    private void ActivateEntry(QuicksaveBrowserEntry entry) {
        if (entry.Kind == QuicksaveBrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
            return;
        }

        LoadEntry(entry);
    }

    private void LoadEntry(QuicksaveBrowserEntry entry) {
        Close();
        QuicksaveService.LoadQuicksave(entry.FullPath);
    }

    private void ConfirmInlineEdit() {
        string name = state.EditBuffer.Trim();
        if (name.Length == 0) {
            return;
        }

        try {
            switch (state.EditMode) {
                case InlineEditMode.Saving:
                    QuicksaveService.SaveQuicksave(
                        name,
                        QuicksaveBrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
                    );
                    break;

                case InlineEditMode.SavingTo when state.EditTargetPath != null:
                    QuicksaveService.SaveQuicksave(
                        name,
                        QuicksaveBrowserNavigation.GetRelativeSubdirectory(state.EditTargetPath)
                    );
                    break;

                case InlineEditMode.RenamingFile or InlineEditMode.RenamingFolder
                    when state.EditTargetPath is { } renamePath:
                    QuicksaveService.RenameQuicksave(renamePath, name);
                    break;

                case InlineEditMode.CreatingFolder:
                    QuicksaveService.CreateQuicksaveFolder(
                        name,
                        QuicksaveBrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
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
            Logger.Warn(nameof(QuicksaveBrowserHandler), $"Inline edit failed: {ex.Message}");
        }
    }

    private void RenderDeleteModal() {
        if (!TryBeginModal(DeleteModalId, state.ShowDeleteModal, ref deletePopupOpened, state.CancelDelete)) {
            return;
        }

        ImGui.TextUnformatted($"Delete \"{state.PendingDeleteLabel}\"?");
        ImGui.Separator();

        if (ImGui.Button("Yes", new System.Numerics.Vector2(120, 0))) {
            ConfirmDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("No", new System.Numerics.Vector2(120, 0))) {
            state.CancelDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void ConfirmDelete() {
        if (state.PendingDeletePath == null) {
            state.CancelDelete();
            return;
        }

        try {
            QuicksaveService.DeleteQuicksave(state.PendingDeletePath);
            state.CancelDelete();
            state.RefreshEntries();
        } catch (Exception ex) {
            state.CancelDelete();
            Logger.Warn(nameof(QuicksaveBrowserHandler), $"Failed to delete quicksave: {ex.Message}");
        }
    }

    private void RenderConflictModal() {
        if (!TryBeginModal(ConflictModalId, state.ShowConflictModal, ref conflictPopupOpened, state.ClearConflict)) {
            return;
        }

        ImGui.TextUnformatted(state.ConflictMessage ?? "A file or folder with that name already exists.");
        ImGui.Separator();

        if (ImGui.Button("OK", new System.Numerics.Vector2(120, 0))) {
            state.ClearConflict();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}
