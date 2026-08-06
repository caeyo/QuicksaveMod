using Celeste.Mod.QuicksaveMod.Ghost.Storage;
using Celeste.Mod.QuicksaveMod.Playback;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostSpectateController {
    private static bool watching;
    private static bool playbackStarted;
    private static bool hintShown;
    private static readonly TasPlaybackFileState FileState = new();

    public static bool IsActive => watching;

    public static void Start() {
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
        SpectateHintHud.Hide();
        PlaybackCoordinator.Clear(ActivePlayback.GhostSpectate);
    }

    public static void OnEngineUpdate() {
        if (!watching) {
            return;
        }

        if (Manager.Running) {
            playbackStarted = true;
            TryShowHintAtBreakpoint();
            return;
        }

        if (!playbackStarted) {
            return;
        }

        Finish();
    }

    private static void TryShowHintAtBreakpoint() {
        if (hintShown || !Manager.Controller.Break) {
            return;
        }

        hintShown = true;
        SpectateHintHud.Show();
    }

    private static void Finish() {
        watching = false;
        playbackStarted = false;
        hintShown = false;
        SpectateHintHud.Hide();

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
