using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Recording;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class GhostBrowserHandler : ImGuiHandler {
    internal static GhostBrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private const string WindowId = "Ghost Browser";
    private const float BaseWindowWidth = 520f;
    private const float BaseWindowHeight = 420f;
    private const float DesignDisplayHeight = 1080f;

    private readonly GhostBrowserState state = new();
    private readonly GhostBrowserCommands commands;
    private readonly GhostBrowserModals modals;
    private readonly GhostBrowserView view;

    private bool savedMouseVisible;
    private bool appliedFreeze;
    private float savedFontGlobalScale = 1f;
    private float appliedUiScale = 0f;
    private bool forceWindowSize;

    public GhostBrowserHandler() {
        Instance = this;
        Visible = false;
        commands = new GhostBrowserCommands(state, ModBrowserCoordinator.CloseAll);
        view = new GhostBrowserView(state, commands);
        modals = new GhostBrowserModals(state, commands);
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        float uiScale = ComputeUiScale();
        ApplyUiScale(uiScale);

        if (state.FocusWindow) {
            ImGui.SetNextWindowFocus();
            state.FocusWindow = false;
        }

        ImGuiCond sizeCond = forceWindowSize ? ImGuiCond.Always : ImGuiCond.FirstUseEver;
        ImGui.SetNextWindowSize(
            new System.Numerics.Vector2(BaseWindowWidth * uiScale, BaseWindowHeight * uiScale),
            sizeCond
        );
        forceWindowSize = false;

        if (!ImGui.Begin(WindowId, ImGuiWindowFlags.NoDocking)) {
            ImGui.End();
            modals.Render();
            view.FlushPendingActivate();
            return;
        }

        state.ApplyPendingInlineEdit();
        view.HandleWindowKeyboardShortcuts();
        view.RenderBreadcrumbs();
        ImGui.Separator();
        view.RenderEntryList();
        view.RenderInlineEditArea();
        view.ClearCancelledDragSource();
        ImGui.End();
        modals.Render();
        view.FlushPendingActivate();
    }

    public void Open(bool focusWindow) {
        GhostBrowserNavigation.EnsureRootExists();
        state.NavigateTo(GhostBrowserNavigation.RootPath);
        state.ResetTransient();
        state.FocusWindow = focusWindow;

        savedMouseVisible = Engine.Instance.IsMouseVisible;
        Engine.Instance.IsMouseVisible = true;

        appliedFreeze = false;
        if (Engine.Scene is Level level && level is { Paused: false, Frozen: false }) {
            level.Frozen = true;
            appliedFreeze = true;
        }

        savedFontGlobalScale = ImGui.GetIO().FontGlobalScale;
        appliedUiScale = 0f;
        forceWindowSize = true;

        RecordingSessionControls.SuspendAll();
        bool openingFirst = !ModBrowserCoordinator.AnyVisible;
        Visible = true;
        if (openingFirst) {
            BrowserInputHooks.OnBrowserOpened();
        }
    }

    public void Close() {
        if (!Visible) {
            return;
        }

        Visible = false;
        state.ResetTransient();
        view.ResetTransient();
        modals.ResetTransient();

        ImGui.GetIO().FontGlobalScale = savedFontGlobalScale;
        appliedUiScale = 1f;

        if (appliedFreeze && Engine.Scene is Level level) {
            level.Frozen = false;
        }

        appliedFreeze = false;
        RecordingSessionControls.ResumeAll();
        Engine.Instance.IsMouseVisible = savedMouseVisible;
        MInput.Disabled = false;
        MInput.Active = true;

        if (!ModBrowserCoordinator.AnyVisible) {
            BrowserInputHooks.OnBrowserClosed();
        }
    }

    internal void OnAfterInputUpdate() {
        if (!Visible) {
            return;
        }

        if (Input.ESC.Pressed) {
            if (!TryCancelSubOperationOnEscape()) {
                ModBrowserCoordinator.CloseAll();
            }
        }

        ConsumeUnderlyingInput();
    }

    internal void SetNextWindowPos(System.Numerics.Vector2 pos, System.Numerics.Vector2 pivot) {
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
    }

    private bool TryCancelSubOperationOnEscape() {
        if (modals.TryCancelOnEscape()) {
            return true;
        }

        if (state.EditMode != InlineEditMode.None) {
            state.CancelInlineEdit();
            return true;
        }

        return false;
    }

    private void ApplyUiScale(float uiScale) {
        if (Math.Abs(uiScale - appliedUiScale) < 0.001f) {
            return;
        }

        ImGui.GetIO().FontGlobalScale = savedFontGlobalScale * uiScale;
        if (appliedUiScale > 0f) {
            forceWindowSize = true;
        }

        appliedUiScale = uiScale;
    }

    private static float ComputeUiScale() {
        float displayHeight = ImGui.GetIO().DisplaySize.Y;
        if (displayHeight <= 1f) {
            displayHeight = DesignDisplayHeight;
        }

        float auto = Math.Clamp(displayHeight / DesignDisplayHeight, 1f, 2f);
        float user = Math.Clamp(QuicksaveModModule.Settings.BrowserUiScalePercent / 100f, 1f, 2f);
        return Math.Clamp(auto * user, 1f, 2.5f);
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
}

internal sealed class GhostBrowserView(GhostBrowserState state, GhostBrowserCommands commands) {
    private const string DragPayloadType = "GHOST_FILE";
    private string? dragSourcePath;
    private BrowserEntry? pendingActivate;

    public void ResetTransient() {
        dragSourcePath = null;
        pendingActivate = null;
    }

    public void HandleWindowKeyboardShortcuts() {
        if (ImGui.IsAnyItemActive()
            || state.EditMode != InlineEditMode.None
            || state.ShowDeleteModal
            || state.ShowConflictModal) {
            return;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Backspace)
            && !GhostBrowserNavigation.IsRootDirectory(state.CurrentDirectory)) {
            state.NavigateUp();
        } else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
            state.MoveSelection(-1);
        } else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
            state.MoveSelection(1);
        } else if (ImGui.IsKeyPressed(ImGuiKey.Enter) && state.SelectedEntry is { } entry) {
            pendingActivate = entry;
        }
    }

    public void RenderBreadcrumbs() {
        state.EnsureBreadcrumbs();
        for (int i = 0; i < state.Breadcrumbs.Count; i++) {
            if (i > 0) {
                ImGui.SameLine();
                ImGui.TextUnformatted(" / ");
                ImGui.SameLine();
            }

            if (ImGui.SmallButton(state.BreadcrumbButtonIds[i])) {
                state.NavigateTo(state.Breadcrumbs[i].AbsolutePath);
            }

            RenderDirectoryDropTarget(state.Breadcrumbs[i].AbsolutePath);
        }
    }

    public void RenderEntryList() {
        float listHeight = -(ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetStyle().ItemSpacing.Y);
        if (!ImGui.BeginChild("GhostEntryList", new System.Numerics.Vector2(0, listHeight))) {
            return;
        }

        if (state.Entries.Count == 0) {
            ImGui.TextUnformatted("(empty)");
        }

        for (int i = 0; i < state.Entries.Count; i++) {
            BrowserEntry entry = state.Entries[i];
            if (ImGui.Selectable(
                state.EntrySelectableIds[i],
                i == state.SelectedIndex,
                ImGuiSelectableFlags.AllowDoubleClick
            )) {
                state.SelectedIndex = i;
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    pendingActivate = entry;
                }
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                state.SelectedIndex = i;
            }

            ImGui.OpenPopupOnItemClick(state.EntryPopupIds[i], ImGuiPopupFlags.MouseButtonRight);
            RenderEntryContextMenu(entry, state.EntryPopupIds[i]);

            if (entry.Kind == BrowserEntryKind.File) {
                RenderFileDragSource(entry);
            } else {
                RenderDirectoryDropTarget(entry.FullPath);
            }
        }

        RenderEmptySpaceContextMenu();
        ImGui.EndChild();
    }

    public void RenderInlineEditArea() {
        if (state.EditMode == InlineEditMode.None) {
            return;
        }

        string prompt = state.EditMode switch {
            InlineEditMode.Saving => "Save as:",
            InlineEditMode.RenamingFile => "Rename file:",
            InlineEditMode.RenamingFolder => "Rename folder:",
            InlineEditMode.CreatingFolder => "New folder:",
            _ => "Name:",
        };

        ImGui.TextUnformatted(prompt);
        ImGui.SameLine();
        if (state.FocusEditField) {
            ImGui.SetKeyboardFocusHere();
            state.FocusEditField = false;
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##ghost_inline_edit", ref state.EditBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
            commands.ConfirmInlineEdit();
        }
    }

    public void ClearCancelledDragSource() {
        if (dragSourcePath != null && !ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
            dragSourcePath = null;
        }
    }

    public void FlushPendingActivate() {
        if (pendingActivate is not { } entry) {
            return;
        }

        pendingActivate = null;
        if (entry.Kind == BrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
        }
    }

    private void RenderEmptySpaceContextMenu() {
        if (!ImGui.BeginPopupContextWindow("ghost_empty_ctx", ImGuiPopupFlags.NoOpenOverItems | ImGuiPopupFlags.MouseButtonRight)) {
            return;
        }

        if (GhostRecordingSession.IsAnchored && ImGui.MenuItem("Save from last Load")) {
            state.RequestInlineEdit(InlineEditMode.Saving, GhostBrowserNavigation.DefaultSaveName());
        }

        if (ImGui.MenuItem("New Folder")) {
            state.RequestInlineEdit(InlineEditMode.CreatingFolder, "New Folder");
        }

        ImGui.EndPopup();
    }

    private void RenderEntryContextMenu(BrowserEntry entry, string popupId) {
        if (!ImGui.BeginPopup(popupId)) {
            return;
        }

        if (entry.Kind == BrowserEntryKind.File) {
            if (ImGui.MenuItem("Race")) {
                commands.RaceEntry(entry);
            }

            if (ImGui.MenuItem("Spectate")) {
                commands.SpectateEntry(entry);
            }

            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFile,
                    GhostBrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, GhostBrowserNavigation.GetDisplayName(entry));
            }
        } else {
            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFolder,
                    GhostBrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, GhostBrowserNavigation.GetDisplayName(entry));
            }
        }

        ImGui.EndPopup();
    }

    private void RenderFileDragSource(BrowserEntry entry) {
        if (!ImGui.BeginDragDropSource()) {
            return;
        }

        dragSourcePath = entry.FullPath;
        ImGui.SetDragDropPayload(DragPayloadType, IntPtr.Zero, 0);
        ImGui.TextUnformatted($"Move {GhostBrowserNavigation.GetDisplayName(entry)}");
        ImGui.EndDragDropSource();
    }

    private void RenderDirectoryDropTarget(string targetDirectory) {
        if (!ImGui.BeginDragDropTarget()) {
            return;
        }

        ImGui.AcceptDragDropPayload(DragPayloadType);
        if (dragSourcePath != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
            commands.TryMoveFile(dragSourcePath, targetDirectory);
            dragSourcePath = null;
        }

        ImGui.EndDragDropTarget();
    }
}

internal sealed class GhostBrowserModals(GhostBrowserState state, GhostBrowserCommands commands) {
    private const string DeleteModalId = "Ghost Confirm Delete";
    private const string ConflictModalId = "Ghost Conflict";
    private bool deletePopupOpened;
    private bool conflictPopupOpened;

    public void Render() {
        RenderDeleteModal();
        RenderConflictModal();
    }

    public void ResetTransient() {
        deletePopupOpened = false;
        conflictPopupOpened = false;
    }

    public bool TryCancelOnEscape() {
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

        return false;
    }

    private void RenderDeleteModal() {
        if (!BeginModal(DeleteModalId, state.ShowDeleteModal, ref deletePopupOpened, state.CancelDelete)) {
            return;
        }

        ImGui.TextUnformatted($"Delete \"{state.PendingDeleteLabel}\"?");
        ImGui.Separator();
        if (ImGui.Button("Yes", new System.Numerics.Vector2(120, 0))) {
            commands.ConfirmDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("No", new System.Numerics.Vector2(120, 0))) {
            state.CancelDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void RenderConflictModal() {
        if (!BeginModal(ConflictModalId, state.ShowConflictModal, ref conflictPopupOpened, state.ClearConflict)) {
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

    private static bool BeginModal(string popupId, bool shouldShow, ref bool popupOpened, Action onDismiss) {
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
}
