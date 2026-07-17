using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Quicksave;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.UI;

internal sealed class BrowserHandler : ImGuiHandler {
    internal static BrowserHandler? Instance { get; private set; }

    internal static void ClearInstance() => Instance = null;

    private const string WindowId = "Quicksave Browser";
    private const float WindowWidth = 520f;
    private const float WindowHeight = 420f;

    private readonly BrowserState state = new();
    private readonly BrowserCommands commands;
    private readonly BrowserView view;
    private readonly BrowserModals modals;

    private bool savedMouseVisible;
    private bool appliedFreeze;

    public BrowserHandler() {
        Instance = this;
        Visible = false;

        commands = new BrowserCommands(state, Close);
        view = new BrowserView(state, commands);
        modals = new BrowserModals(state, commands);
    }

    public override void Update(GameTime gameTime) {
        if (Visible) {
            return;
        }

        if (!QuicksaveModModule.Settings.OpenBrowser.Pressed) {
            return;
        }

        QuicksaveModModule.Settings.OpenBrowser.ConsumePress();
        QuicksaveModModule.Settings.OpenBrowser.ConsumeBuffer();
        Open();
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        if (state.FocusWindow) {
            ImGui.SetNextWindowFocus();
            state.FocusWindow = false;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(WindowWidth, WindowHeight), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin(WindowId, ImGuiWindowFlags.NoDocking)) {
            ImGui.End();
            modals.Render();
            view.FlushPendingActivate();
            return;
        }

        state.ApplyPendingInlineEdit();

        view.HandleWindowKeyboardShortcuts();

        view.RenderBreadcrumbs();
        ImGui.Separator();
        view.RenderEntryList();
        view.RenderInlineEditArea();
        view.ClearCancelledDragSource();

        ImGui.End();
        modals.Render();
        view.FlushPendingActivate();
    }

    public void Open() {
        BrowserNavigation.EnsureRootExists();

        state.NavigateTo(BrowserNavigation.RootPath);
        state.ResetTransient();
        state.FocusWindow = true;

        savedMouseVisible = Engine.Instance.IsMouseVisible;
        Engine.Instance.IsMouseVisible = true;

        appliedFreeze = false;
        if (Engine.Scene is Level level) {
            if (!level.Paused && !level.Frozen) {
                level.Frozen = true;
                appliedFreeze = true;
            }
        }

        QuicksaveService.SuspendTracking();
        Visible = true;
    }

    public void Close() {
        if (!Visible) {
            return;
        }

        Visible = false;
        state.ResetTransient();
        view.ResetTransient();
        modals.ResetTransient();

        if (appliedFreeze && Engine.Scene is Level level) {
            level.Frozen = false;
        }

        appliedFreeze = false;
        QuicksaveService.ResumeTracking();
        Engine.Instance.IsMouseVisible = savedMouseVisible;
    }

    internal void OnAfterInputUpdate() {
        if (!Visible) {
            return;
        }

        if (Input.ESC.Pressed) {
            if (!TryCancelSubOperationOnEscape()) {
                Close();
            }
        }

        ConsumeUnderlyingInput();
    }

    private bool TryCancelSubOperationOnEscape() {
        if (modals.TryCancelOnEscape()) {
            return true;
        }

        if (state.EditMode != InlineEditMode.None) {
            state.CancelInlineEdit();
            return true;
        }

        return false;
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
}
