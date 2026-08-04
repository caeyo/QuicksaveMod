using Celeste.Mod.QuicksaveMod.Quicksave;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

internal sealed class TasPlaybackFileState {
    public string? PreviousFilePath;
    public bool FilePathOverridden;
    public string? TempTasPath;
}

internal static class TasFilePlayback {
    public static void ScheduleStart(
        TasPlaybackFileState state,
        string tasFilePath,
        string? orphanedTempPath,
        bool autoStart,
        Func<string, bool>? isTempPlaybackPath = null
    ) {
        string fullPath = Path.GetFullPath(tasFilePath);
        state.TempTasPath = fullPath;

        Manager.AddMainThreadAction(() => {
            if (Manager.Running) {
                Manager.DisableRun();
            }

            if (!state.FilePathOverridden) {
                state.PreviousFilePath = Manager.Controller.FilePath;
                state.FilePathOverridden = true;
            }

            if (autoStart) {
                Manager.NextState = Manager.State.Running;
            }

            Manager.Controller.FilePath = fullPath;

            if (orphanedTempPath != null
                && !string.Equals(orphanedTempPath, fullPath, StringComparison.OrdinalIgnoreCase)) {
                TryDeleteTempFile(orphanedTempPath, isTempPlaybackPath ?? (_ => true), QuicksaveConstants.LogTag);
            }
        });
    }

    public static void RestoreFilePath(TasPlaybackFileState state) {
        if (!state.FilePathOverridden) {
            return;
        }

        string? previous = state.PreviousFilePath;
        state.PreviousFilePath = null;
        state.FilePathOverridden = false;

        if (previous == null) {
            return;
        }

        if (Manager.Running) {
            Manager.DisableRun();
        }

        Manager.Controller.FilePath = previous;
    }

    public static void TryDeleteTempFile(
        string? path,
        Func<string, bool> isTempPlaybackPath,
        string logTag
    ) {
        if (path == null || !isTempPlaybackPath(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch (Exception e) {
            Logger.Warn(logTag, $"Failed to delete temp TAS file '{path}': {e.Message}");
        }
    }
}
