using Celeste.Mod.QuicksaveMod.Ghost.Storage;
using Celeste.Mod.QuicksaveMod.Playback;
using Monocle;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostSpectateController {
    private static bool watching;
    private static bool playbackStarted;
    private static bool hintShown;
    private static readonly TasPlaybackFileState FileState = new();

    public static bool IsActive => watching;

    public static void Start(GhostData ghost) {
        Reset();
        PlaybackCoordinator.Begin(ActivePlayback.GhostSpectate);
        watching = true;
        playbackStarted = false;
        hintShown = false;
    }

    public static void StartPlayback(string tasFilePath) {
        TasFilePlayback.ScheduleStart(
            FileState,
            tasFilePath,
            orphanedTempPath: null,
            autoStart: true,
            GhostPath.IsTempPlaybackPath
        );
    }

    public static void Reset() {
        RestorePreviousFilePath();
        DeleteTempTasFile();
        watching = false;
        playbackStarted = false;
        hintShown = false;
        PlaybackCoordinator.Clear(ActivePlayback.GhostSpectate);
    }

    public static void OnEngineUpdate() {
        if (!watching) {
            return;
        }

        TryShowHint();

        if (Manager.Running) {
            playbackStarted = true;
            return;
        }

        if (!playbackStarted) {
            return;
        }

        Finish();
    }

    private static void TryShowHint() {
        if (hintShown || Engine.Scene is not Level level) {
            return;
        }

        hintShown = true;
        level.Add(new SpectateHintHud());
    }

    private static void Finish() {
        watching = false;
        playbackStarted = false;
        hintShown = false;

        if (Manager.Running) {
            Manager.DisableRun();
        }

        RestorePreviousFilePath();
        DeleteTempTasFile();
        Logger.Info(GhostConstants.LogTag, "Ghost spectate finished.");
        PlaybackCoordinator.Clear(ActivePlayback.GhostSpectate);
    }

    private static void RestorePreviousFilePath() => TasFilePlayback.RestoreFilePath(FileState);

    private static void DeleteTempTasFile() {
        string? path = FileState.TempTasPath;
        FileState.TempTasPath = null;
        TasFilePlayback.TryDeleteTempFile(path, GhostPath.IsTempPlaybackPath, GhostConstants.LogTag);
    }
}
