using Celeste.Mod.QuicksaveMod.UI;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Hooks;

internal static class BrowserInputHooks {
    private static readonly BrowserHotkeyBlocker HotkeyBlocker = new();

    public static void Apply() {
        On.Monocle.MInput.Update += OnMInputUpdate;
        On.Celeste.ScreenWipe.Update += OnScreenWipeUpdate;
    }

    public static void Unapply() {
        HotkeyBlocker.Detach();
        On.Celeste.ScreenWipe.Update -= OnScreenWipeUpdate;
        On.Monocle.MInput.Update -= OnMInputUpdate;
    }

    internal static void OnBrowserOpened() => HotkeyBlocker.Attach();

    internal static void OnBrowserClosed() => HotkeyBlocker.Detach();

    private static bool ShouldPauseScreenWipe => ModBrowserCoordinator.AnyVisible;

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();

        if (!ModBrowserCoordinator.AnyVisible) {
            return;
        }

        if (Input.ESC.Pressed) {
            if (!TryCancelBrowserSubOperation()) {
                ModBrowserCoordinator.CloseAll();
            }
        }

        ConsumeUnderlyingInput();
    }

    private static bool TryCancelBrowserSubOperation() {
        return QuicksaveBrowserHandler.Instance?.TryCancelOnEscape() == true
            || GhostBrowserHandler.Instance?.TryCancelOnEscape() == true;
    }

    private static void ConsumeUnderlyingInput() {
        foreach (VirtualInput input in MInput.VirtualInputs) {
            if (input is VirtualButton button) {
                button.ConsumePress();
                button.ConsumeBuffer();
            }
        }

        MInput.Disabled = true;
        MInput.Active = false;
    }

    private static void OnScreenWipeUpdate(On.Celeste.ScreenWipe.orig_Update orig, ScreenWipe self, Scene scene) {
        if (ShouldPauseScreenWipe) {
            return;
        }

        orig(self, scene);
    }

    // Speedrun Tool disables hotkey invocation when TextInput.OnInput has subscribers
    // outside its allowlist (ImGuiHelper / BingoChat). Subscribing while the browser is
    // open blocks Save/Load etc. without depending on MInput.
    private sealed class BrowserHotkeyBlocker {
        private bool attached;

        public void Attach() {
            if (attached) {
                return;
            }

            TextInput.OnInput += OnTextInput;
            attached = true;
        }

        public void Detach() {
            if (!attached) {
                return;
            }

            TextInput.OnInput -= OnTextInput;
            attached = false;
        }

        private static void OnTextInput(char _) { }
    }
}
