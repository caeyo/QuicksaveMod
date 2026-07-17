using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserModals(
    BrowserState state,
    BrowserCommands commands
) {
    private const string DeleteModalId = "Quicksave Confirm Delete";
    private const string ConflictModalId = "Quicksave Conflict";
    private const float ModalButtonWidth = 120f;

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

    private void RenderDeleteModal() {
        if (!TryBeginModal(DeleteModalId, state.ShowDeleteModal, ref deletePopupOpened, state.CancelDelete)) {
            return;
        }

        ImGui.TextUnformatted($"Delete \"{state.PendingDeleteLabel}\"?");
        ImGui.Separator();

        if (ImGui.Button("Yes", new System.Numerics.Vector2(ModalButtonWidth, 0))) {
            commands.ConfirmDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("No", new System.Numerics.Vector2(ModalButtonWidth, 0))) {
            state.CancelDelete();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void RenderConflictModal() {
        if (!TryBeginModal(ConflictModalId, state.ShowConflictModal, ref conflictPopupOpened, state.ClearConflict)) {
            return;
        }

        ImGui.TextUnformatted(state.ConflictMessage ?? "A file or folder with that name already exists.");
        ImGui.Separator();

        if (ImGui.Button("OK", new System.Numerics.Vector2(ModalButtonWidth, 0))) {
            state.ClearConflict();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}
