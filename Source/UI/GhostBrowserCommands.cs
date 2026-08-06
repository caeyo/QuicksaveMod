using Celeste.Mod.QuickTools.Ghost;
using Celeste.Mod.QuickTools.Ghost.Storage;
using ImGuiNET;

namespace Celeste.Mod.QuickTools.UI;

internal sealed class GhostBrowserCommands(
    BrowserProfile profile,
    BrowserState state,
    Action closeBrowser,
    Action<BrowserEntry, BrowserFileActivation> queueActivate
) : IBrowserViewHost {
    public void TryMoveFile(string sourcePath, string targetDirectory) {
        try {
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            if (sourceDirectory.Equals(Path.GetFullPath(targetDirectory), StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            GhostRepository.MoveGhost(
                sourcePath,
                BrowserNavigation.GetRelativeSubdirectory(profile, targetDirectory) ?? ""
            );
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to move ghost: {ex.Message}");
        }
    }

    public void ActivateEntry(BrowserEntry entry, BrowserFileActivation activation = BrowserFileActivation.Primary) {
        if (entry.Kind == BrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
            return;
        }

        closeBrowser();
        switch (activation) {
            case BrowserFileActivation.Spectate:
                GhostService.LoadGhostForSpectate(entry.FullPath);
                break;
            default:
                GhostService.LoadGhostForRace(entry.FullPath);
                break;
        }
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
                        BrowserNavigation.GetRelativeSubdirectory(profile, state.CurrentDirectory)
                    );
                    break;
                case InlineEditMode.RenamingFile or InlineEditMode.RenamingFolder
                    when state.EditTargetPath is { } renamePath:
                    GhostRepository.RenameGhost(renamePath, name);
                    break;
                case InlineEditMode.CreatingFolder:
                    GhostRepository.CreateGhostFolder(
                        name,
                        BrowserNavigation.GetRelativeSubdirectory(profile, state.CurrentDirectory)
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

    public void RenderEmptySpaceContextMenu() {
        if (GhostRecordingSession.IsAnchored && ImGui.MenuItem("Save from last Load")) {
            state.RequestInlineEdit(InlineEditMode.Saving, BrowserNavigation.DefaultSaveName());
        }

        if (ImGui.MenuItem("New Folder")) {
            state.RequestInlineEdit(InlineEditMode.CreatingFolder, "New Folder");
        }
    }

    public void RenderFileContextMenu(BrowserEntry entry) {
        if (ImGui.MenuItem("Race")) {
            queueActivate(entry, BrowserFileActivation.Primary);
        }

        if (ImGui.MenuItem("Spectate")) {
            queueActivate(entry, BrowserFileActivation.Spectate);
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
    }

    public void RenderFolderContextMenu(BrowserEntry entry) {
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
}
