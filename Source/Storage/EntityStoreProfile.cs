using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.Storage;

internal sealed record EntityStoreProfile(
    string RootFolderName,
    string Extension,
    string TempFolderName,
    string TempTasPrefix,
    string EntityLabel,
    string RootFolderLabel,
    string InvalidSubdirectoryMessage,
    string OutsideRootMessage,
    string InvalidFilePathMessage,
    string FileNotFoundMessage,
    string FolderNotFoundMessage,
    string RootCannotDeleteMessage
) {
    public static EntityStoreProfile Quicksave { get; } = new(
        RootFolderName: "Quicksaves",
        Extension: QuicksaveConstants.Extension,
        TempFolderName: QuicksaveConstants.TempFolderName,
        TempTasPrefix: QuicksaveConstants.TempTasPrefix,
        EntityLabel: "quicksave",
        RootFolderLabel: "Quicksaves",
        InvalidSubdirectoryMessage: "Invalid quicksave subdirectory.",
        OutsideRootMessage: "Path must stay within the Quicksaves folder.",
        InvalidFilePathMessage: "Quicksave path must point to a .qs file.",
        FileNotFoundMessage: "Quicksave file not found: {0}",
        FolderNotFoundMessage: "Quicksave folder not found: {0}",
        RootCannotDeleteMessage: "The Quicksaves root folder cannot be deleted."
    );

    public static EntityStoreProfile Ghost { get; } = new(
        RootFolderName: "Ghosts",
        Extension: GhostConstants.Extension,
        TempFolderName: GhostConstants.TempFolderName,
        TempTasPrefix: GhostConstants.TempTasPrefix,
        EntityLabel: "ghost",
        RootFolderLabel: "Ghosts",
        InvalidSubdirectoryMessage: "Invalid ghost subdirectory.",
        OutsideRootMessage: "Path must stay within the Ghosts folder.",
        InvalidFilePathMessage: "Ghost path must point to a .ghost file.",
        FileNotFoundMessage: "Ghost file not found: {0}",
        FolderNotFoundMessage: "Ghost folder not found: {0}",
        RootCannotDeleteMessage: "The Ghosts root folder cannot be deleted."
    );

    public string MoveConflictMessage(string destination) =>
        EntityLabel == "quicksave"
            ? $"A quicksave already exists at '{destination}'."
            : $"A ghost file already exists at '{destination}'.";

    public string RenameFileConflictMessage(string destination) =>
        EntityLabel == "quicksave"
            ? $"A quicksave already exists at '{destination}'."
            : $"A ghost file already exists at '{destination}'.";

    public string RenameFolderConflictMessage(string destination) =>
        EntityLabel == "quicksave"
            ? $"A quicksave folder already exists at '{destination}'."
            : $"A ghost folder already exists at '{destination}'.";

    public string CreateFolderConflictMessage(string destination) =>
        EntityLabel == "quicksave"
            ? $"A quicksave folder already exists at '{destination}'."
            : $"A ghost folder already exists at '{destination}'.";

    public string PathNotFoundMessage(string path) =>
        EntityLabel == "quicksave"
            ? $"Quicksave path not found: {path}"
            : $"Ghost path not found: {path}";

    public string SubdirectoryOutsideRootMessage =>
        EntityLabel == "quicksave"
            ? "Quicksave subdirectory must stay within the Quicksaves folder."
            : "Ghost subdirectory must stay within the Ghosts folder.";
}
