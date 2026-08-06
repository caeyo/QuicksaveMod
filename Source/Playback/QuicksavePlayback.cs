using Celeste.Mod.QuickTools.Ghost.Playback;
using Celeste.Mod.QuickTools.Interop;
using Celeste.Mod.QuickTools.Module;
using Celeste.Mod.QuickTools.Quicksave;
using Celeste.Mod.QuickTools.Quicksave.Storage;
using Celeste.Mod.QuickTools.Recording;
using TAS;

namespace Celeste.Mod.QuickTools.Playback;

internal static class QuicksavePlayback {
    private static bool watching;
    private static bool playbackStarted;
    private static readonly TasPlaybackFileState FileState = new();
    private static QuicksaveData? loadedQuicksave;
    private static InputTimelineRestorer.GhostRestoreMode seedGhostMode =
        InputTimelineRestorer.GhostRestoreMode.AlwaysAnchor;

    public static bool IsWatching => watching;

    public static void Reset() {
        TasFilePlayback.Cleanup(FileState, QuicksavePath.IsTempPlaybackPath, QuicksaveConstants.LogTag);
        watching = false;
        playbackStarted = false;
        loadedQuicksave = null;
        PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
    }

    public static void Start(
        string tasFilePath,
        QuicksaveData loaded,
        InputTimelineRestorer.GhostRestoreMode ghostSeedMode = InputTimelineRestorer.GhostRestoreMode.AlwaysAnchor
    ) {
        PlaybackCoordinator.Begin(ActivePlayback.QuicksaveAnchor);

        string? orphanedTemp = FileState.TempTasPath;
        loadedQuicksave = loaded.Clone();
        seedGhostMode = ghostSeedMode;
        watching = true;
        playbackStarted = false;

        QuicksaveLoadFreeze.Cancel();

        TasFilePlayback.ScheduleStart(
            FileState,
            tasFilePath,
            orphanedTemp,
            autoStart: true,
            QuicksavePath.IsTempPlaybackPath,
            QuicksaveConstants.LogTag
        );
    }

    public static void OnEngineUpdate() {
        if (!watching) {
            return;
        }

        if (Manager.Running) {
            playbackStarted = true;

            if (Manager.CurrState == Manager.State.Paused && Manager.Controller.Break) {
                Finish();
            }

            return;
        }

        if (!playbackStarted) {
            return;
        }

        Finish();
    }

    private static void Finish() {
        watching = false;
        playbackStarted = false;

        if (Manager.Running) {
            Manager.DisableRun();
        }

        TasFilePlayback.Cleanup(FileState, QuicksavePath.IsTempPlaybackPath, QuicksaveConstants.LogTag);
        RaiseSeedNeeded();

        if (GhostRaceController.IsArmed) {
            GhostRaceController.OnAnchorPlaybackComplete();

            if (ShouldSavestateOnLoad() && SpeedrunToolBridge.TrySaveState()) {
                Logger.Info(
                    QuicksaveConstants.LogTag,
                    "Quicksave playback finished; created SpeedrunTool savestate for ghost race anchor."
                );
                PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
                return;
            }

            QuicksaveLoadFreeze.Begin();
            Logger.Info(QuicksaveConstants.LogTag, "Ghost race anchor finished; waiting for input.");
            PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
            return;
        }

        if (ShouldSavestateOnLoad() && SpeedrunToolBridge.TrySaveState()) {
            Logger.Info(QuicksaveConstants.LogTag, "Quicksave playback finished; created SpeedrunTool savestate.");
            PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
            return;
        }

        QuicksaveLoadFreeze.Begin();
        Logger.Info(QuicksaveConstants.LogTag, "Quicksave playback finished; CelesteTAS stopped.");
        PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
    }

    private static void RaiseSeedNeeded() {
        QuicksaveData? data = loadedQuicksave;
        loadedQuicksave = null;
        if (data == null) {
            return;
        }

        InputTimelineRestorer.Restore(data, seedGhostMode);
        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Seeded input tracker with {data.Inputs.Count} lines from loaded quicksave."
        );
    }

    private static bool ShouldSavestateOnLoad() {
        return QuickToolsModule.Settings.SavestateOnQuicksaveLoad
            && SpeedrunToolBridge.IsLoaded
            && SpeedrunToolBridge.IsEnabled;
    }

}
