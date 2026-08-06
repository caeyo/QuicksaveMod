using Celeste.Mod.QuicksaveMod.Module;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class ModBrowserCoordinator {
    private readonly QuicksaveBrowserHandler quicksaveBrowser;
    private readonly GhostBrowserHandler ghostBrowser;

    public ModBrowserCoordinator(QuicksaveBrowserHandler quicksaveBrowser, GhostBrowserHandler ghostBrowser) {
        this.quicksaveBrowser = quicksaveBrowser;
        this.ghostBrowser = ghostBrowser;
    }

    public static bool AnyVisible =>
        QuicksaveBrowserHandler.Instance?.Visible == true || GhostBrowserHandler.Instance?.Visible == true;

    public void Update(GameTime gameTime) {
        if (AnyVisible) {
            return;
        }

        if (!QuicksaveModModule.Settings.OpenBrowser.Pressed) {
            return;
        }

        QuicksaveModModule.Settings.OpenBrowser.ConsumePress();
        QuicksaveModModule.Settings.OpenBrowser.ConsumeBuffer();
        OpenBoth();
    }

    public void OpenBoth() {
        float halfWidth = ImGui.GetIO().DisplaySize.X * 0.5f;
        quicksaveBrowser.SetNextWindowPos(new System.Numerics.Vector2(0, 0), System.Numerics.Vector2.Zero);
        ghostBrowser.SetNextWindowPos(new System.Numerics.Vector2(halfWidth, 0), System.Numerics.Vector2.Zero);
        quicksaveBrowser.Open(focusWindow: true);
        ghostBrowser.Open(focusWindow: false);
    }

    public static void CloseAll() {
        QuicksaveBrowserHandler.Instance?.Close();
        GhostBrowserHandler.Instance?.Close();
    }
}
