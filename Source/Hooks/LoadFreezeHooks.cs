using Celeste.Mod.QuicksaveMod.Playback;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Hooks;

internal static class LoadFreezeHooks {
    public static void Apply() {
        On.Celeste.Level.Update += QuicksaveLoadFreeze.OnLevelUpdate;
        On.Monocle.MInput.Update += OnMInputUpdate;
    }

    public static void Unapply() {
        On.Celeste.Level.Update -= QuicksaveLoadFreeze.OnLevelUpdate;
        On.Monocle.MInput.Update -= OnMInputUpdate;
        QuicksaveLoadFreeze.Cancel();
    }

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();
        QuicksaveLoadFreeze.OnAfterInputUpdate();
    }
}
