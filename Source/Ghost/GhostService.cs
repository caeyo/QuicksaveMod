using Celeste.Mod.QuicksaveMod.Ghost.Playback;
using Celeste.Mod.QuicksaveMod.Ghost.Storage;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostService {
    public static void SaveGhost(string? fileName = null, string? subdirectory = null) {
        GhostData data = GhostRecordingSession.BuildGhostData()
            ?? throw new InvalidOperationException("No ghost recording session is anchored.");

        if (Engine.Scene is not Level) {
            throw new InvalidOperationException("Ghosts can only be saved while in a level.");
        }

        if (SaveData.Instance == null) {
            throw new InvalidOperationException("No SaveData is loaded.");
        }

        data.Anchor.SaveUid = SaveSlotResolver.EnsureCurrentSaveUid();

        string directory = GhostPath.ResolveSaveDirectory(subdirectory);
        Directory.CreateDirectory(directory);

        fileName ??= $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{GhostConstants.Extension}";
        if (!fileName.EndsWith(GhostConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            fileName += GhostConstants.Extension;
        }

        string path = Path.Combine(directory, fileName);
        GhostSerializer.Write(path, data);
        Logger.Info(GhostConstants.LogTag, $"Saved ghost to {path}");
    }

    public static void LoadGhostForRace(string filePath) {
        string fullPath = GhostPath.ResolveGhostFilePath(filePath, mustExist: true);
        GhostData ghost = GhostSerializer.Read(fullPath);
        GhostRaceController.Prepare(ghost);
        LoadAnchorPlayback(ghost.Anchor);
        Logger.Info(GhostConstants.LogTag, $"Loading ghost race from {fullPath}");
    }

    public static void LoadGhostForSpectate(string filePath) {
        string fullPath = GhostPath.ResolveGhostFilePath(filePath, mustExist: true);
        GhostData ghost = GhostSerializer.Read(fullPath);
        QuicksaveLoadFreeze.Cancel();
        GhostSpectateController.Start(ghost);

        Session session = SessionLoadHelper.PrepareSession(ghost.Anchor);
        string tempTasPath = SessionLoadHelper.CreateTempTasPath(
            GhostPath.TempDirectory,
            GhostConstants.TempTasPrefix
        );
        SessionLoadHelper.WriteAnchorTasFile(tempTasPath, ghost.Anchor.Inputs, ghost.Inputs);

        Engine.Scene = new LevelLoader(session);
        GhostSpectateController.StartPlayback(tempTasPath);
        Logger.Info(GhostConstants.LogTag, $"Loading ghost spectate from {fullPath}");
    }

    private static void LoadAnchorPlayback(QuicksaveData anchor) {
        Session session = SessionLoadHelper.PrepareSession(anchor);
        string tempTasPath = SessionLoadHelper.CreateTempTasPath(
            GhostPath.TempDirectory,
            GhostConstants.TempTasPrefix
        );
        SessionLoadHelper.WriteAnchorTasFile(tempTasPath, anchor.Inputs, appendLoadFreezeFrame: true);

        Engine.Scene = new LevelLoader(session);
        QuicksavePlayback.Start(tempTasPath, anchor);
    }
}
