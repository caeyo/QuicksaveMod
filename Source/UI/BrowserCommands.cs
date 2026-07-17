using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserCommands {
    private readonly BrowserState state;
    private readonly Action closeBrowser;

    public BrowserCommands(BrowserState state, Action closeBrowser) {
        this.state = state;
        this.closeBrowser = closeBrowser;
    }

    public void TryMoveFile(string sourcePath, string targetDirectory) {
        try {
            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
            string target = Path.GetFullPath(targetDirectory);

            if (sourceDirectory.Equals(target, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            QuicksaveRepository.MoveQuicksave(
                sourcePath,
                BrowserNavigation.GetRelativeSubdirectory(target) ?? ""
            );
            state.RefreshEntries();
        } catch (IOException ex) {
            state.ShowConflict(ex.Message);
        } catch (Exception ex) {
            Logger.Warn(QuicksaveConstants.LogTag, $"Failed to move quicksave: {ex.Message}");
        }
    }

    public void ActivateEntry(BrowserEntry entry) {
        if (entry.Kind == BrowserEntryKind.Folder) {
            state.NavigateTo(entry.FullPath);
            return;
        }

        LoadEntry(entry);
    }

    public void LoadEntry(BrowserEntry entry) {
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
                        BrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
                    );
                    break;

                case InlineEditMode.SavingTo when state.EditTargetPath != null:
                    QuicksaveService.SaveQuicksave(
                        name,
                        BrowserNavigation.GetRelativeSubdirectory(state.EditTargetPath)
                    );
                    break;

                case InlineEditMode.RenamingFile or InlineEditMode.RenamingFolder
                    when state.EditTargetPath is { } renamePath:
                    QuicksaveRepository.RenameQuicksave(renamePath, name);
                    break;

                case InlineEditMode.CreatingFolder:
                    QuicksaveRepository.CreateQuicksaveFolder(
                        name,
                        BrowserNavigation.GetRelativeSubdirectory(state.CurrentDirectory)
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
}
