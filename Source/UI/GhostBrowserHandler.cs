using Celeste.Mod.ImGuiHelper;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class GhostBrowserHandler : ImGuiHandler {
    internal static GhostBrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private readonly BrowserProfile profile = BrowserProfile.Ghost;
    private readonly BrowserState state;
    private readonly BrowserWindowChrome chrome;

    public GhostBrowserHandler() {
        Instance = this;
        Visible = false;

        state = new BrowserState(profile);
        BrowserView view = null!;
        GhostBrowserCommands commands = new(
            profile,
            state,
            ModBrowserCoordinator.CloseAll,
            (entry, activation) => view.QueueActivate(entry, activation)
        );
        view = new BrowserView(profile, state, commands);
        BrowserModals modals = new(profile, state, commands);
        chrome = new BrowserWindowChrome(profile, state, view, modals);
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        chrome.Render();
    }

    public void Open(bool focusWindow) {
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
