using Celeste.Mod.QuicksaveMod.Quicksave;
using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserView(
    BrowserState state,
    BrowserCommands commands
) {
    private const string DragPayloadType = "QS_FILE";

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
        float listHeight = -(InlineEditAreaHeight + ImGui.GetStyle().ItemSpacing.Y);

        if (ImGui.BeginChild("QuicksaveEntryList", new System.Numerics.Vector2(0, listHeight))) {
            if (state.Entries.Count == 0) {
                ImGui.TextUnformatted("(empty)");
            }

            for (int i = 0; i < state.Entries.Count; i++) {
                BrowserEntry entry = state.Entries[i];
                bool selected = i == state.SelectedIndex;

                if (ImGui.Selectable(state.EntrySelectableIds[i], selected, ImGuiSelectableFlags.AllowDoubleClick)) {
                    state.SelectedIndex = i;

                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                        // Defer: ActivateEntry may NavigateTo/Close and invalidate cached ids mid-loop.
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

                if (selected && state.SelectedIndex == i && ImGui.IsWindowFocused()) {
                    ImGui.SetScrollHereY(0.5f);
                }
            }

            RenderEmptySpaceContextMenu();
            ImGui.EndChild();
        }
    }

    public void RenderInlineEditArea() {
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
                commands.ConfirmInlineEdit();
            }
        }

        ImGui.EndChild();
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
        commands.ActivateEntry(entry);
    }

    private void RenderEmptySpaceContextMenu() {
        if (!ImGui.BeginPopupContextWindow("empty_ctx", ImGuiPopupFlags.NoOpenOverItems | ImGuiPopupFlags.MouseButtonRight)) {
            return;
        }

        if (QuicksaveService.IsTracking && ImGui.MenuItem("Save")) {
            state.RequestInlineEdit(InlineEditMode.Saving, BrowserNavigation.DefaultSaveName());
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
            if (ImGui.MenuItem("Load")) {
                pendingActivate = entry;
            }

            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFile,
                    BrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, BrowserNavigation.GetDisplayName(entry));
            }
        } else {
            if (QuicksaveService.IsTracking && ImGui.MenuItem("Save To")) {
                state.RequestInlineEdit(
                    InlineEditMode.SavingTo,
                    BrowserNavigation.DefaultSaveName(),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Rename")) {
                state.RequestInlineEdit(
                    InlineEditMode.RenamingFolder,
                    BrowserNavigation.RenameDefaultText(entry),
                    entry.FullPath
                );
            }

            if (ImGui.MenuItem("Delete")) {
                state.BeginDelete(entry.FullPath, BrowserNavigation.GetDisplayName(entry));
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
        ImGui.TextUnformatted($"Move {BrowserNavigation.GetDisplayName(entry)}");
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
