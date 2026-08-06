using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Ghost.Storage;
using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class GhostBrowserCommands(
    BrowserProfile profile,
    BrowserState state,
    Action closeBrowser
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

    public void ActivateEntry(BrowserEntry entry) {
        if (entry.Kind == BrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
            return;
        }

        RaceEntry(entry);
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
            RaceEntry(entry);
        }

        if (ImGui.MenuItem("Spectate")) {
            SpectateEntry(entry);
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
