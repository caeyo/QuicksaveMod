using Celeste.Mod.QuicksaveMod.UI;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Hooks;

internal static class BrowserInputHooks {
    public static void Apply() {
        On.Monocle.MInput.Update += OnMInputUpdate;
        On.Celeste.ScreenWipe.Update += OnScreenWipeUpdate;
    }

    public static void Unapply() {
        On.Monocle.MInput.Update -= OnMInputUpdate;
        On.Celeste.ScreenWipe.Update -= OnScreenWipeUpdate;
    }

    private static bool ShouldPauseScreenWipe => BrowserHandler.Instance?.Visible == true;

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();
        BrowserHandler.Instance?.OnAfterInputUpdate();
    }

    private static void OnScreenWipeUpdate(On.Celeste.ScreenWipe.orig_Update orig, ScreenWipe self, Scene scene) {
        if (ShouldPauseScreenWipe) {
            return;
        }

        orig(self, scene);
    }
}
