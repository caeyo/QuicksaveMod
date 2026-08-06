using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;
using ImGuiNET;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class QuicksaveBrowserCommands(
    BrowserProfile profile,
    BrowserState state,
    Action closeBrowser,
    Action<BrowserEntry> queueActivate
) : IBrowserViewHost {
    public void TryMoveFile(string sourcePath, string targetDirectory) {
        try {
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            string target = Path.GetFullPath(targetDirectory);

            if (sourceDirectory.Equals(target, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            QuicksaveRepository.MoveQuicksave(
                sourcePath,
                BrowserNavigation.GetRelativeSubdirectory(profile, target) ?? ""
            );
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(QuicksaveConstants.LogTag, $"Failed to move quicksave: {ex.Message}");
        }
    }

    public void ActivateEntry(BrowserEntry entry, BrowserFileActivation activation = BrowserFileActivation.Primary) {
        if (entry.Kind == BrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
            return;
        }

        closeBrowser();
        QuicksaveService.LoadQuicksave(entry.FullPath);
    }

    public void ConfirmInlineEdit() {
        string name = state.EditBuffer.Trim();
        if (name.Length == 0) {
            return;
        }

        try {
            switch (state.EditMode) {
                case InlineEditMode.Saving:
                    QuicksaveService.SaveQuicksave(
                        name,
                        BrowserNavigation.GetRelativeSubdirectory(profile, state.CurrentDirectory)
                    );
                    break;

                case InlineEditMode.SavingTo when state.EditTargetPath != null:
                    QuicksaveService.SaveQuicksave(
                        name,
                        BrowserNavigation.GetRelativeSubdirectory(profile, state.EditTargetPath)
                    );
                    break;

                case InlineEditMode.RenamingFile or InlineEditMode.RenamingFolder
                    when state.EditTargetPath is { } renamePath:
                    QuicksaveRepository.RenameQuicksave(renamePath, name);
                    break;

                case InlineEditMode.CreatingFolder:
                    QuicksaveRepository.CreateQuicksaveFolder(
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
            Logger.Warn(QuicksaveConstants.LogTag, $"Inline edit failed: {ex.Message}");
        }
    }

    public void ConfirmDelete() {
        if (state.PendingDeletePath == null) {
            state.CancelDelete();
            return;
        }

        try {
            QuicksaveRepository.DeleteQuicksave(state.PendingDeletePath);
            state.CancelDelete();
            state.RefreshEntries();
        } catch (Exception ex) {
            state.CancelDelete();
            Logger.Warn(QuicksaveConstants.LogTag, $"Failed to delete quicksave: {ex.Message}");
        }
    }

    public void RenderEmptySpaceContextMenu() {
        if (QuicksaveService.IsTracking && ImGui.MenuItem("Save")) {
            state.RequestInlineEdit(InlineEditMode.Saving, BrowserNavigation.DefaultSaveName());
        }

        if (ImGui.MenuItem("New Folder")) {
            state.RequestInlineEdit(InlineEditMode.CreatingFolder, "New Folder");
        }
    }

    public void RenderFileContextMenu(BrowserEntry entry) {
        if (ImGui.MenuItem("Load")) {
            queueActivate(entry);
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
}
