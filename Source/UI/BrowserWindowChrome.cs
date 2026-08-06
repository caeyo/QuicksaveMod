using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Recording;
using ImGuiNET;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserWindowChrome {
    private const float BaseWindowWidth = 520f;
    private const float BaseWindowHeight = 420f;
    private const float DesignDisplayHeight = 1080f;

    private readonly BrowserProfile profile;
    private readonly BrowserState state;
    private readonly BrowserView view;
    private readonly BrowserModals modals;

    private bool savedMouseVisible;
    private bool appliedFreeze;
    private float savedFontGlobalScale = 1f;
    private float appliedUiScale = 1f;
    private bool forceWindowSize;

    public BrowserWindowChrome(
        BrowserProfile profile,
        BrowserState state,
        BrowserView view,
        BrowserModals modals
    ) {
        this.profile = profile;
        this.state = state;
        this.view = view;
        this.modals = modals;
    }

    public void Render() {
        float uiScale = ComputeUiScale();
        ApplyUiScale(uiScale);

        if (state.FocusWindow) {
            ImGui.SetNextWindowFocus();
            state.FocusWindow = false;
        }

        ImGuiCond sizeCond = forceWindowSize ? ImGuiCond.Always : ImGuiCond.FirstUseEver;
        ImGui.SetNextWindowSize(
            new System.Numerics.Vector2(BaseWindowWidth * uiScale, BaseWindowHeight * uiScale),
            sizeCond
        );
        forceWindowSize = false;

        if (!ImGui.Begin(profile.WindowId, ImGuiWindowFlags.NoDocking)) {
            ImGui.End();
            modals.Render();
            view.FlushPendingActivate();
            return;
        }

        state.ApplyPendingInlineEdit();

        view.RenderBreadcrumbs();
        ImGui.Separator();
        view.RenderEntryList();
        view.RenderInlineEditArea();
        view.ClearCancelledDragSource();

        ImGui.End();
        modals.Render();
        view.FlushPendingActivate();
    }

    public void Open(bool focusWindow) {
        BrowserNavigation.EnsureRootExists(profile);

        state.NavigateTo(state.DirectoryRecall.ResolveOpenDirectory(BrowserNavigation.RootPath(profile)));
        state.ResetTransient();
        view.ResetTransient();
        modals.ResetTransient();
        state.FocusWindow = focusWindow;

        savedMouseVisible = Engine.Instance.IsMouseVisible;
        Engine.Instance.IsMouseVisible = true;

        appliedFreeze = false;
        if (Engine.Scene is Level level && level is { Paused: false, Frozen: false }) {
            level.Frozen = true;
            appliedFreeze = true;
        }

        savedFontGlobalScale = ImGui.GetIO().FontGlobalScale;
        appliedUiScale = 0f;
        forceWindowSize = true;

        RecordingSessionControls.SuspendAll();
    }

    public void NotifyOpened(bool openingFirst) {
        if (openingFirst) {
            BrowserInputHooks.OnBrowserOpened();
        }
    }

    public void Close() {
        state.DirectoryRecall.Remember(state.CurrentDirectory);

        state.ResetTransient();
        view.ResetTransient();
        modals.ResetTransient();

        ImGui.GetIO().FontGlobalScale = savedFontGlobalScale;
        appliedUiScale = 1f;

        if (appliedFreeze && Engine.Scene is Level level) {
            level.Frozen = false;
        }

        appliedFreeze = false;
        RecordingSessionControls.ResumeAll();
        Engine.Instance.IsMouseVisible = savedMouseVisible;

        MInput.Disabled = false;
        MInput.Active = true;
    }

    public void NotifyClosed(bool anyStillVisible) {
        if (!anyStillVisible) {
            BrowserInputHooks.OnBrowserClosed();
        }
    }

    public void SetNextWindowPos(System.Numerics.Vector2 pos, System.Numerics.Vector2 pivot) {
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
    }

    public bool TryCancelOnEscape() {
        if (modals.TryCancelOnEscape()) {
            return true;
        }

        if (state.EditMode != InlineEditMode.None) {
            state.CancelInlineEdit();
            return true;
        }

        return false;
    }

    private void ApplyUiScale(float uiScale) {
        if (Math.Abs(uiScale - appliedUiScale) < 0.001f) {
            return;
        }

        ImGui.GetIO().FontGlobalScale = savedFontGlobalScale * uiScale;
        if (appliedUiScale > 0f) {
            forceWindowSize = true;
        }

        appliedUiScale = uiScale;
    }

    private static float ComputeUiScale() {
        float displayHeight = ImGui.GetIO().DisplaySize.Y;
        if (displayHeight <= 1f) {
            displayHeight = DesignDisplayHeight;
        }

        float auto = Math.Clamp(displayHeight / DesignDisplayHeight, 1f, 2f);
        float user = Math.Clamp(QuicksaveModModule.Settings.BrowserUiScalePercent / 100f, 1f, 2f);
        return Math.Clamp(auto * user, 1f, 2.5f);
    }
}
