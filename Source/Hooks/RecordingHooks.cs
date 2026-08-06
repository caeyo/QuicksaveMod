using Celeste.Mod.QuickTools.Playback;
using Celeste.Mod.QuickTools.Recording;

namespace Celeste.Mod.QuickTools.Hooks;

internal static class RecordingHooks {
    public static void Apply() {
        GameplayInputRecorder.EnsureMapper();
        On.Monocle.MInput.Update += OnMInputUpdate;
    }

    public static void Unapply() {
        On.Monocle.MInput.Update -= OnMInputUpdate;
        GameplayInputRecorder.ClearMapper();
    }

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();

        if (QuicksaveLoadFreeze.IsWaiting) {
            return;
        }

        GameplayInputRecorder.OnAfterInputUpdate();
    }
}
