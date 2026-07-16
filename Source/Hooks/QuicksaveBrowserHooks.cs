using Celeste.Mod.QuicksaveMod.UI;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Hooks;

public static class QuicksaveBrowserHooks {
    public static void Apply() {
        On.Monocle.MInput.Update += OnMInputUpdate;
        On.Celeste.ScreenWipe.Update += OnScreenWipeUpdate;
    }

    public static void Unapply() {
        On.Monocle.MInput.Update -= OnMInputUpdate;
        On.Celeste.ScreenWipe.Update -= OnScreenWipeUpdate;
    }

    private static bool ShouldPauseScreenWipe => QuicksaveBrowserHandler.Instance?.Visible == true;

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();
        QuicksaveBrowserHandler.Instance?.OnAfterInputUpdate();
    }

    private static void OnScreenWipeUpdate(On.Celeste.ScreenWipe.orig_Update orig, ScreenWipe self, Scene scene) {
        if (ShouldPauseScreenWipe) {
            return;
        }

        orig(self, scene);
    }
}
