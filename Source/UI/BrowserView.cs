using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserView(
    BrowserProfile profile,
    BrowserState state,
    IBrowserViewHost host
) {
    private string? dragSourcePath;
    private BrowserEntry? pendingActivate;

    private static float InlineEditAreaHeight =>
        ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;

    public void ResetTransient() {
        dragSourcePath = null;
        pendingActivate = null;
    }

    public void RenderBreadcrumbs() {
        state.EnsureBreadcrumbs();

        for (int i = 0; i < state.Breadcrumbs.Count; i++) {
            if (i > 0) {
                ImGui.SameLine();
                ImGui.TextUnformatted(" / ");
                ImGui.SameLine();
            }

            BrowserBreadcrumb crumb = state.Breadcrumbs[i];
            if (ImGui.SmallButton(state.BreadcrumbButtonIds[i])) {
                state.NavigateTo(crumb.AbsolutePath);
            }

            RenderDirectoryDropTarget(crumb.AbsolutePath);
        }
    }

    public void RenderEntryList() {
        float listHeight = -(InlineEditAreaHeight * profile.ListFooterRowCount + ImGui.GetStyle().ItemSpacing.Y);

        if (!ImGui.BeginChild(profile.ListChildId, new System.Numerics.Vector2(0, listHeight))) {
            return;
        }

        if (state.Entries.Count == 0) {
            ImGui.TextUnformatted("(empty)");
        }

        for (int i = 0; i < state.Entries.Count; i++) {
            BrowserEntry entry = state.Entries[i];
            bool selected = i == state.SelectedIndex;

            if (ImGui.Selectable(state.EntrySelectableIds[i], selected, ImGuiSelectableFlags.AllowDoubleClick)) {
                state.SelectedIndex = i;

                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    // Defer: activation may NavigateTo/Close and invalidate cached ids mid-loop.
                    pendingActivate = entry;
                }
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                state.SelectedIndex = i;
            }

            string popupId = state.EntryPopupIds[i];
            ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonRight);
            RenderEntryContextMenu(entry, popupId);

            if (entry.Kind == BrowserEntryKind.File) {
                RenderFileDragSource(entry);
            }

            if (entry.Kind == BrowserEntryKind.Folder) {
                RenderDirectoryDropTarget(entry.FullPath);
            }

            if (profile.ScrollSelectionIntoView
                && selected
                && state.SelectedIndex == i
                && ImGui.IsWindowFocused()) {
                ImGui.SetScrollHereY(0.5f);
            }
        }

        RenderEmptySpaceContextMenu();
        ImGui.EndChild();
    }

    public void RenderInlineEditArea() {
        if (profile.InlineEditUsesChildPanel) {
            ImGui.BeginChild(
                profile.InlineEditChildId,
                new System.Numerics.Vector2(0, InlineEditAreaHeight),
                ImGuiChildFlags.None,
                ImGuiWindowFlags.NoScrollbar
            );

            if (state.EditMode != InlineEditMode.None) {
                RenderInlineEditFields();
            }

            ImGui.EndChild();
            return;
        }

        if (state.EditMode == InlineEditMode.None) {
            return;
        }

        RenderInlineEditFields();
    }

    private void RenderInlineEditFields() {
        string prompt = state.EditMode switch {
            InlineEditMode.Saving => "Save as:",
            InlineEditMode.SavingTo => "Save to folder as:",
            InlineEditMode.RenamingFile => "Rename file:",
            InlineEditMode.RenamingFolder => "Rename folder:",
            InlineEditMode.CreatingFolder => "New folder:",
            _ => "Name:",
        };

        if (profile.InlineEditUsesChildPanel) {
            ImGui.AlignTextToFramePadding();
        }

        ImGui.TextUnformatted(prompt);
        ImGui.SameLine();

        if (state.FocusEditField) {
            ImGui.SetKeyboardFocusHere();
            state.FocusEditField = false;
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText(profile.InlineEditFieldId, ref state.EditBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
            host.ConfirmInlineEdit();
        }
    }

    public void ClearCancelledDragSource() {
        if (dragSourcePath == null || ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
            return;
        }

        dragSourcePath = null;
    }

    public void FlushPendingActivate() {
        if (pendingActivate is not { } entry) {
            return;
        }

        pendingActivate = null;
        host.ActivateEntry(entry);
    }

    public void QueueActivate(BrowserEntry entry) => pendingActivate = entry;

    private void RenderEmptySpaceContextMenu() {
        if (!ImGui.BeginPopupContextWindow(
            profile.EmptyContextMenuId,
            ImGuiPopupFlags.NoOpenOverItems | ImGuiPopupFlags.MouseButtonRight
        )) {
            return;
        }

        host.RenderEmptySpaceContextMenu();
        ImGui.EndPopup();
    }

    private void RenderEntryContextMenu(BrowserEntry entry, string popupId) {
        if (!ImGui.BeginPopup(popupId)) {
            return;
        }

        if (entry.Kind == BrowserEntryKind.File) {
            host.RenderFileContextMenu(entry);
        } else {
            host.RenderFolderContextMenu(entry);
        }

        ImGui.EndPopup();
    }

    private void RenderFileDragSource(BrowserEntry entry) {
        if (!ImGui.BeginDragDropSource()) {
            return;
        }

        dragSourcePath = entry.FullPath;
        ImGui.SetDragDropPayload(profile.DragPayloadType, IntPtr.Zero, 0);
        ImGui.TextUnformatted($"Move {BrowserNavigation.GetDisplayName(entry)}");
        ImGui.EndDragDropSource();
    }

    private void RenderDirectoryDropTarget(string targetDirectory) {
        if (!ImGui.BeginDragDropTarget()) {
            return;
        }

        ImGui.AcceptDragDropPayload(profile.DragPayloadType);

        if (dragSourcePath != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
            host.TryMoveFile(dragSourcePath, targetDirectory);
            dragSourcePath = null;
        }

        ImGui.EndDragDropTarget();
    }
}
