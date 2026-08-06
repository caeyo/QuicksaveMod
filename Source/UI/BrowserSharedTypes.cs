namespace Celeste.Mod.QuicksaveMod.UI;

internal enum BrowserEntryKind {
    Folder,
    File,
}

internal readonly record struct BrowserEntry(
    string Name,
    string FullPath,
    BrowserEntryKind Kind
);

internal readonly record struct BrowserBreadcrumb(
    string Label,
    string AbsolutePath
);

internal enum InlineEditMode {
    None,
    Saving,
    SavingTo,
    RenamingFile,
    RenamingFolder,
    CreatingFolder,
}

internal readonly record struct PendingInlineEditRequest(
    InlineEditMode Mode,
    string DefaultText,
    string? TargetPath
);
