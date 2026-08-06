using Celeste.Mod.ImGuiHelper;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuickTools.UI;

internal sealed class QuicksaveBrowserHandler : ImGuiHandler {
    internal static QuicksaveBrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private readonly BrowserProfile profile = BrowserProfile.Quicksave;
    private readonly BrowserWindowChrome chrome;

    private ModBrowserCoordinator? coordinator;

    public QuicksaveBrowserHandler() {
        Instance = this;
        Visible = false;

        BrowserState state1 = new(profile);
        BrowserView view = null!;
        QuicksaveBrowserCommands commands = new(
            profile,
            state1,
            ModBrowserCoordinator.CloseAll,
            entry => view.QueueActivate(entry)
        );
        view = new BrowserView(profile, state1, commands);
        BrowserModals modals = new(profile, state1, commands);
        chrome = new BrowserWindowChrome(profile, state1, view, modals);
    }

    public override void Update(GameTime gameTime) {
        coordinator?.Update(gameTime);
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        chrome.Render();
    }

    internal void SetCoordinator(ModBrowserCoordinator value) => coordinator = value;

    public void Open(bool focusWindow = true) {
        BrowserNavigation.EnsureRootExists(profile);

        bool openingFirst = !ModBrowserCoordinator.AnyVisible;
        chrome.Open(focusWindow);
        Visible = true;
        chrome.NotifyOpened(openingFirst);
    }

    public void Close() {
        if (!Visible) {
            return;
        }

        Visible = false;
        chrome.Close();
        chrome.NotifyClosed(ModBrowserCoordinator.AnyVisible);
    }

    internal void SetNextWindowPos(System.Numerics.Vector2 pos, System.Numerics.Vector2 pivot) {
        chrome.SetNextWindowPos(pos, pivot);
    }

    internal bool TryCancelOnEscape() {
        if (!Visible) {
            return false;
        }

        return chrome.TryCancelOnEscape();
    }
}
