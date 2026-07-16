using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Module;
using Monocle;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

public static class QuicksavePlayback {
    private static bool _watching;
    private static bool _playbackStarted;
    private static string? _previousFilePath;
    private static bool _filePathOverridden;
    private static string? _tempTasPath;

    public static void Apply() {
        On.Monocle.Engine.Update += WatchForEnd;
    }

    public static void Unapply() {
        On.Monocle.Engine.Update -= WatchForEnd;
        RestorePreviousFilePath();
        DeleteTempTasFile();
        _watching = false;
        _playbackStarted = false;
    }

    public static void Start(string tasFilePath) {
        string fullPath = Path.GetFullPath(tasFilePath);
        string? orphanedTemp = _tempTasPath;
        _tempTasPath = fullPath;
        _watching = true;
        _playbackStarted = false;

        // A prior post-playback freeze must not block Level.Update / player intro on the new load.
        QuicksaveLoadFreeze.Cancel();

        Manager.AddMainThreadAction(() => {
            if (Manager.Running) {
                Manager.DisableRun();
            }

            if (!_filePathOverridden) {
                _previousFilePath = Manager.Controller.FilePath;
                _filePathOverridden = true;
            }

            // RefreshInputs clears parsed inputs when NextState is Disabled.
            Manager.NextState = Manager.State.Running;
            Manager.Controller.FilePath = fullPath;

            if (orphanedTemp != null
                && !string.Equals(orphanedTemp, fullPath, StringComparison.OrdinalIgnoreCase)) {
                TryDeleteTempFile(orphanedTemp);
            }
        });
    }

    private static void WatchForEnd(On.Monocle.Engine.orig_Update orig, Engine engine, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(engine, gameTime);

        if (!_watching) {
            return;
        }

        if (Manager.Running) {
            _playbackStarted = true;

            // *** breakpoint: CelesteTAS pauses with Break set.
            if (Manager.CurrState == Manager.State.Paused && Manager.Controller.Break) {
                FinishPlayback();
            }

            return;
        }

        // EnableRun happens on a later Manager.Update after we set NextState.
        if (!_playbackStarted) {
            return;
        }

        // Unexpected stop (EOF without pause, abort, etc.).
        FinishPlayback();
    }

    private static void FinishPlayback() {
        _watching = false;
        _playbackStarted = false;

        if (Manager.Running) {
            Manager.DisableRun();
        }

        RestorePreviousFilePath();
        DeleteTempTasFile();

        if (ShouldSavestateOnLoad() && SpeedrunToolBridge.TrySaveState()) {
            Logger.Info(nameof(QuicksavePlayback), "Quicksave playback finished; created SpeedrunTool savestate.");
            return;
        }

        QuicksaveLoadFreeze.Begin();
        Logger.Info(nameof(QuicksavePlayback), "Quicksave playback finished; CelesteTAS stopped.");
    }

    private static bool ShouldSavestateOnLoad() {
        return QuicksaveModModule.Settings.SavestateOnQuicksaveLoad
            && SpeedrunToolBridge.IsLoaded;
    }

    private static void RestorePreviousFilePath() {
        if (!_filePathOverridden) {
            return;
        }

        string? previous = _previousFilePath;
        _previousFilePath = null;
        _filePathOverridden = false;

        if (previous == null) {
            return;
        }

        // Avoid re-triggering DisableRunLater; we already stopped playback.
        if (Manager.Running) {
            Manager.DisableRun();
        }

        Manager.Controller.FilePath = previous;
    }

    private static void DeleteTempTasFile() {
        string? path = _tempTasPath;
        _tempTasPath = null;
        TryDeleteTempFile(path);
    }

    private static void TryDeleteTempFile(string? path) {
        if (path == null || !IsTempPlaybackPath(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch (Exception e) {
            Logger.Warn(nameof(QuicksavePlayback), $"Failed to delete temp TAS file '{path}': {e.Message}");
        }
    }

    private static bool IsTempPlaybackPath(string path) {
        string fullPath = Path.GetFullPath(path);
        string tempRoot = Path.GetFullPath(Path.Combine(Everest.PathGame, "Quicksaves", ".temp"));

        if (!fullPath.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(tempRoot, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        string fileName = Path.GetFileName(fullPath);
        return fileName.StartsWith("playback_", StringComparison.Ordinal)
            && fileName.EndsWith(".tas", StringComparison.OrdinalIgnoreCase);
    }
}
