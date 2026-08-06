using TAS;

namespace Celeste.Mod.QuickTools.Playback;

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
        Func<string, bool> isTempPlaybackPath,
        string logTag
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
                TryDeleteTempFile(orphanedTempPath, isTempPlaybackPath, logTag);
            }
        });
    }

    public static void Cleanup(
        TasPlaybackFileState state,
        Func<string, bool> isTempPlaybackPath,
        string logTag
    ) {
        RestoreFilePath(state);
        DeleteTempFile(state, isTempPlaybackPath, logTag);
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

    public static void DeleteTempFile(
        TasPlaybackFileState state,
        Func<string, bool> isTempPlaybackPath,
        string logTag
    ) {
        string? path = state.TempTasPath;
        state.TempTasPath = null;
        TryDeleteTempFile(path, isTempPlaybackPath, logTag);
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
