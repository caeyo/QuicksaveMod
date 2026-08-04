using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

internal static class QuicksavePlayback {
    private static bool watching;
    private static bool playbackStarted;
    private static string? previousFilePath;
    private static bool filePathOverridden;
    private static string? tempTasPath;
    private static QuicksaveData? loadedQuicksave;

    // Wired by Module so Playback does not call into Tracker/Recorder directly
    public static Action<QuicksaveData>? OnSeedNeeded { get; set; }

    public static void Reset() {
        RestorePreviousFilePath();
        DeleteTempTasFile();
        watching = false;
        playbackStarted = false;
        loadedQuicksave = null;
    }

    public static void Start(string tasFilePath, QuicksaveData loaded) {
        string fullPath = Path.GetFullPath(tasFilePath);
        string? orphanedTemp = tempTasPath;
        tempTasPath = fullPath;
        loadedQuicksave = loaded.Clone();
        watching = true;
        playbackStarted = false;

        // A prior post-playback freeze must not block Level.Update / player intro on the new load
        QuicksaveLoadFreeze.Cancel();

        Manager.AddMainThreadAction(() => {
            if (Manager.Running) {
                Manager.DisableRun();
            }

            if (!filePathOverridden) {
                previousFilePath = Manager.Controller.FilePath;
                filePathOverridden = true;
            }

            // RefreshInputs clears parsed inputs when NextState is Disabled
            Manager.NextState = Manager.State.Running;
            Manager.Controller.FilePath = fullPath;

            if (orphanedTemp != null
                && !string.Equals(orphanedTemp, fullPath, StringComparison.OrdinalIgnoreCase)) {
                TryDeleteTempFile(orphanedTemp);
            }
        });
    }

    public static void OnEngineUpdate() {
        if (!watching) {
            return;
        }

        if (Manager.Running) {
            playbackStarted = true;

            // *** breakpoint: CelesteTAS pauses with Break set
            if (Manager.CurrState == Manager.State.Paused && Manager.Controller.Break) {
                Finish();
            }

            return;
        }

        // EnableRun happens on a later Manager.Update after we set NextState
        if (!playbackStarted) {
            return;
        }

        // Unexpected stop (EOF without pause, abort, etc.)
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
            return;
        }

        QuicksaveLoadFreeze.Begin();
        Logger.Info(QuicksaveConstants.LogTag, "Quicksave playback finished; CelesteTAS stopped.");
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

    private static void RestorePreviousFilePath() {
        if (!filePathOverridden) {
            return;
        }

        string? previous = previousFilePath;
        previousFilePath = null;
        filePathOverridden = false;

        if (previous == null) {
            return;
        }

        // Avoid re-triggering DisableRunLater; we already stopped playback
        if (Manager.Running) {
            Manager.DisableRun();
        }

        Manager.Controller.FilePath = previous;
    }

    private static void DeleteTempTasFile() {
        string? path = tempTasPath;
        tempTasPath = null;
        TryDeleteTempFile(path);
    }

    private static void TryDeleteTempFile(string? path) {
        if (path == null || !QuicksavePath.IsTempPlaybackPath(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch (Exception e) {
            Logger.Warn(QuicksaveConstants.LogTag, $"Failed to delete temp TAS file '{path}': {e.Message}");
        }
    }
}
