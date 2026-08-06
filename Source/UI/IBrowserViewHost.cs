namespace Celeste.Mod.QuickTools.UI;

internal interface IBrowserCommands {
    void ConfirmDelete();
}

internal interface IBrowserViewHost : IBrowserCommands {
    void TryMoveFile(string sourcePath, string targetDirectory);
    void ConfirmInlineEdit();
    void ActivateEntry(BrowserEntry entry, BrowserFileActivation activation = BrowserFileActivation.Primary);
    void RenderEmptySpaceContextMenu();
    void RenderFileContextMenu(BrowserEntry entry);
    void RenderFolderContextMenu(BrowserEntry entry);
}
