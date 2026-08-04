using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

internal static class QuicksavePlayback {
    private static bool watching;
    private static bool playbackStarted;
    private static readonly TasPlaybackFileState FileState = new();
    private static QuicksaveData? loadedQuicksave;

    public static Action<QuicksaveData>? OnSeedNeeded { get; set; }

    public static void Reset() {
        RestorePreviousFilePath();
        DeleteTempTasFile();
        watching = false;
        playbackStarted = false;
        loadedQuicksave = null;
        PlaybackCoordinator.Clear(ActivePlayback.QuicksaveAnchor);
    }

    public static void Start(string tasFilePath, QuicksaveData loaded) {
        PlaybackCoordinator.Begin(ActivePlayback.QuicksaveAnchor);

        string? orphanedTemp = FileState.TempTasPath;
        loadedQuicksave = loaded.Clone();
        watching = true;
        playbackStarted = false;

        QuicksaveLoadFreeze.Cancel();

        TasFilePlayback.ScheduleStart(
            FileState,
            tasFilePath,
            orphanedTemp,
            autoStart: true,
            QuicksavePath.IsTempPlaybackPath
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

        RestorePreviousFilePath();
        DeleteTempTasFile();
        RaiseSeedNeeded();

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

        OnSeedNeeded?.Invoke(data);
        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Seeded input tracker with {data.Inputs.Count} lines from loaded quicksave."
        );
    }

    private static bool ShouldSavestateOnLoad() {
        return QuicksaveModModule.Settings.SavestateOnQuicksaveLoad
            && SpeedrunToolBridge.IsLoaded
            && SpeedrunToolBridge.IsEnabled;
    }

    private static void RestorePreviousFilePath() => TasFilePlayback.RestoreFilePath(FileState);

    private static void DeleteTempTasFile() {
        string? path = FileState.TempTasPath;
        FileState.TempTasPath = null;
        TasFilePlayback.TryDeleteTempFile(path, QuicksavePath.IsTempPlaybackPath, QuicksaveConstants.LogTag);
    }
}
