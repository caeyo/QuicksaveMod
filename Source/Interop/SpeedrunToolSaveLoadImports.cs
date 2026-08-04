using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Recording;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Interop;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class SpeedrunToolSaveLoadImports {
    private const string TimelineKey = "InputTimeline";

    private static object? registeredAction;

    public delegate object RegisterSaveLoadActionDelegate(
        Action<Dictionary<Type, Dictionary<string, object>>, Level> saveState,
        Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState,
        Action clearState,
        Action<Level> beforeSaveState,
        Action<Level> beforeLoadState,
        Action preCloneEntities
    );

    public static RegisterSaveLoadActionDelegate RegisterSaveLoadAction = null!;
    public static Action<object> Unregister = null!;

    public static void Apply() {
        if (!SpeedrunToolBridge.IsLoaded || registeredAction != null) {
            return;
        }

        registeredAction = RegisterSaveLoadAction(
            OnSaveState,
            OnLoadState,
            null!,
            null!,
            null!,
            null!
        );

        Logger.Info(QuicksaveConstants.LogTag, "Registered SpeedrunTool input timeline SaveLoadAction.");
    }

    public static void Unapply() {
        if (registeredAction != null) {
            Unregister(registeredAction);
        }

        registeredAction = null;
    }

    private static void OnSaveState(Dictionary<Type, Dictionary<string, object>> savedValues, Level level) {
        if (!QuicksaveTracker.IsTracking || QuicksaveTracker.Current is not { } timeline) {
            return;
        }

        if (!savedValues.TryGetValue(typeof(QuicksaveModModule), out Dictionary<string, object>? modValues)) {
            savedValues[typeof(QuicksaveModModule)] = modValues = [];
        }

        modValues[TimelineKey] = timeline.Clone();
        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Embedded input timeline on SRT save ({timeline.Inputs.Count} lines)."
        );
    }

    private static void OnLoadState(Dictionary<Type, Dictionary<string, object>> savedValues, Level level) {
        if (!savedValues.TryGetValue(typeof(QuicksaveModModule), out Dictionary<string, object>? modValues)
            || !modValues.TryGetValue(TimelineKey, out object? value)
            || value is not QuicksaveData timeline) {
            Logger.Warn(
                QuicksaveConstants.LogTag,
                "SRT load did not contain a QuicksaveMod input timeline; buffer was not rolled back."
            );
            return;
        }

        InputTimelineRestorer.Restore(
            timeline,
            InputTimelineRestorer.GhostRestoreMode.MatchRecordingStartOrRaceAnchor
        );

        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Restored input timeline on SRT load ({timeline.Inputs.Count} lines)."
        );
    }
}
