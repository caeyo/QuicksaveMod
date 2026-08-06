using Celeste.Mod.QuickTools.Module;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuickTools.UI;

internal sealed class ModBrowserCoordinator(
    QuicksaveBrowserHandler quicksaveBrowser,
    GhostBrowserHandler ghostBrowser
) {
    public static bool AnyVisible =>
        QuicksaveBrowserHandler.Instance?.Visible == true || GhostBrowserHandler.Instance?.Visible == true;

    public void Update(GameTime gameTime) {
        if (AnyVisible) {
            return;
        }

        if (!QuickToolsModule.Settings.OpenBrowser.Pressed) {
            return;
        }

        QuickToolsModule.Settings.OpenBrowser.ConsumePress();
        QuickToolsModule.Settings.OpenBrowser.ConsumeBuffer();
        OpenBoth();
    }

    private void OpenBoth() {
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
