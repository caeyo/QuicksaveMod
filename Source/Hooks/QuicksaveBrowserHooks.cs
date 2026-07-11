using Celeste.Mod.QuicksaveMod.UI;

namespace Celeste.Mod.QuicksaveMod.Hooks;

public static class QuicksaveBrowserHooks {
    public static void Apply() {
        On.Monocle.MInput.Update += OnMInputUpdate;
    }

    public static void Unapply() {
        On.Monocle.MInput.Update -= OnMInputUpdate;
    }

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();
        QuicksaveBrowserHandler.Instance?.OnAfterInputUpdate();
    }
}
