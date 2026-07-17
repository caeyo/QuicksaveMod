using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Recording;

namespace Celeste.Mod.QuicksaveMod.Hooks;

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
