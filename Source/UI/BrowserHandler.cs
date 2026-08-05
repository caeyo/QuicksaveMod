using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Recording;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserHandler : ImGuiHandler {
    internal static BrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private const string WindowId = "Quicksave Browser";
    private const float BaseWindowWidth = 520f;
    private const float BaseWindowHeight = 420f;
    private const float DesignDisplayHeight = 1080f;

    private readonly BrowserState state = new();
    private readonly BrowserView view;
    private readonly BrowserModals modals;

    private bool savedMouseVisible;
    private bool appliedFreeze;
    private float savedFontGlobalScale = 1f;
    private float appliedUiScale = 1f;
    private bool forceWindowSize;

    private ModBrowserCoordinator? coordinator;

    public BrowserHandler() {
        Instance = this;
        Visible = false;

        BrowserCommands commands = new(state, ModBrowserCoordinator.CloseAll);
        view = new BrowserView(state, commands);
        modals = new BrowserModals(state, commands);
    }

    public override void Update(GameTime gameTime) {
        coordinator?.Update(gameTime);
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

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

        if (!ImGui.Begin(WindowId, ImGuiWindowFlags.NoDocking)) {
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

    internal void SetCoordinator(ModBrowserCoordinator value) => coordinator = value;

    public void Open(bool focusWindow = true) {
        BrowserNavigation.EnsureRootExists();

        state.NavigateTo(state.DirectoryRecall.ResolveOpenDirectory(BrowserNavigation.RootPath));
        state.ResetTransient();
        state.FocusWindow = focusWindow;

        savedMouseVisible = Engine.Instance.IsMouseVisible;
        Engine.Instance.IsMouseVisible = true;

        appliedFreeze = false;
        if (Engine.Scene is Level level) {
            if (level is { Paused: false, Frozen: false }) {
                level.Frozen = true;
                appliedFreeze = true;
            }
        }

        savedFontGlobalScale = ImGui.GetIO().FontGlobalScale;
        appliedUiScale = 0f;
        forceWindowSize = true;

        RecordingSessionControls.SuspendAll();
        bool openingFirst = !ModBrowserCoordinator.AnyVisible;
        Visible = true;
        if (openingFirst) {
            BrowserInputHooks.OnBrowserOpened();
        }
    }

    public void Close() {
        if (!Visible) {
            return;
        }

        state.DirectoryRecall.Remember(state.CurrentDirectory);
        Visible = false;
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

        if (!ModBrowserCoordinator.AnyVisible) {
            BrowserInputHooks.OnBrowserClosed();
        }
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

    internal void SetNextWindowPos(System.Numerics.Vector2 pos, System.Numerics.Vector2 pivot) {
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
    }

    internal bool TryCancelOnEscape() {
        if (!Visible) {
            return false;
        }

        if (modals.TryCancelOnEscape()) {
            return true;
        }

        if (state.EditMode != InlineEditMode.None) {
            state.CancelInlineEdit();
            return true;
        }

        return false;
    }
}
