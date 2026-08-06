using Monocle;

namespace Celeste.Mod.QuickTools.Recording;

internal class TasActionsMapper {
    private readonly List<string> currActions = new(16);

    private TasPressSlot jumpSlot = TasPressSlot.None;
    private TasPressSlot dashSlot = TasPressSlot.None;
    private TasPressSlot crouchDashSlot = TasPressSlot.None;
    private TasPressSlot grabSlot = TasPressSlot.None;

    private string? cachedResumeLabel;
    private string? cachedSkipCutsceneLabel;
    private string? cachedDialogLanguage;

    public void Reset() {
        jumpSlot = TasPressSlot.None;
        dashSlot = TasPressSlot.None;
        crouchDashSlot = TasPressSlot.None;
        grabSlot = TasPressSlot.None;
    }

    public void Sample(
        Level level,
        Player? player,
        InputLineBuffer primary,
        InputLineBuffer? secondary = null
    ) {
        currActions.Clear();
        float? featherAngle = null;
        float? featherMagnitude = null;
        bool menu = IsMenuContext(level);

        AppendMovement(level, player, currActions, ref featherAngle, ref featherMagnitude);
        if (!menu) {
            AppendJump(currActions);
            AppendDash(currActions);
            AppendCrouchDash(currActions);
            AppendGrab(currActions);
        }
        AppendMenuInputs(level, menu, currActions);

        primary.PushFrame(currActions, featherAngle, featherMagnitude);
        secondary?.PushFrame(currActions, featherAngle, featherMagnitude);
    }

    private static void AppendMovement(
        Level level,
        Player? player,
        List<string> actions,
        ref float? featherAngle,
        ref float? featherMagnitude
    ) {
        if (MovementInputSampler.UsesAnalogLocomotion(player)) {
            MovementInputSampler.AppendAnalogMovement(ref featherAngle, ref featherMagnitude);
            return;
        }

        MovementInputSampler.AppendCardinalDirections(level, actions);
    }

    private void AppendJump(List<string> actions) {
        jumpSlot = TwoSlotEncoder.Update(jumpSlot, RawPressed(Input.Jump), Input.Jump.Check);
        AppendSlot(actions, jumpSlot, 'J', 'K');
    }

    private void AppendDash(List<string> actions) {
        dashSlot = TwoSlotEncoder.Update(dashSlot, RawPressed(Input.Dash), Input.Dash.Check);
        AppendSlot(actions, dashSlot, 'X', 'C');
    }

    private void AppendCrouchDash(List<string> actions) {
        crouchDashSlot = TwoSlotEncoder.Update(
            crouchDashSlot,
            RawPressed(Input.CrouchDash),
            Input.CrouchDash.Check
        );
        AppendSlot(actions, crouchDashSlot, 'Z', 'V');
    }

    private void AppendGrab(List<string> actions) {
        grabSlot = TwoSlotEncoder.Update(grabSlot, RawPressed(Input.Grab), Input.GrabCheck);
        AppendSlot(actions, grabSlot, 'G', 'H');
    }

    private void AppendMenuInputs(Level level, bool menu, List<string> actions) {
        if (Input.Pause.Pressed || Input.Pause.Check) {
            actions.Add(TasActionTokens.Pause);
        }

        if (Input.QuickRestart.Pressed) {
            actions.Add(TasActionTokens.QuickRestart);
        }

        if (menu && Input.MenuConfirm.Pressed && ShouldRecordConfirm(level)) {
            actions.Add(TasActionTokens.MenuConfirm);
        }
    }

    private bool ShouldRecordConfirm(Level level) {
        // Dialogue / other non-pause menus: always record confirm.
        if (!level.Paused) {
            return true;
        }

        // Main pause only: Resume / Skip Cutscene
        if (!level.PauseMainMenuOpen) {
            return false;
        }

        return IsResumeOrSkipCutsceneSelected(level);
    }

    private bool IsResumeOrSkipCutsceneSelected(Level level) {
        if (level.Entities.FindFirst<TextMenu>() is not { } menu
            || menu.Current is not TextMenu.Button button) {
            return false;
        }

        EnsureDialogLabelsCached();
        return button.Label == cachedResumeLabel
            || button.Label == cachedSkipCutsceneLabel;
    }

    private void EnsureDialogLabelsCached() {
        string? language = Dialog.Language?.Id;
        if (cachedDialogLanguage == language
            && cachedResumeLabel != null
            && cachedSkipCutsceneLabel != null) {
            return;
        }

        cachedDialogLanguage = language;
        cachedResumeLabel = Dialog.Clean("menu_pause_resume");
        cachedSkipCutsceneLabel = Dialog.Clean("menu_pause_skip_cutscene");
    }

    private static bool IsMenuContext(Level level) {
        if (level.Paused) {
            return true;
        }

        return Engine.Scene.Tracker.GetEntity<Textbox>() != null;
    }

    private static bool RawPressed(VirtualButton button) =>
        button.Binding.Pressed(button.GamepadIndex, button.Threshold);

    private static void AppendSlot(List<string> actions, TasPressSlot slot, char slotAChar, char slotBChar) {
        switch (slot) {
            case TasPressSlot.A:
            case TasPressSlot.B:
                actions.Add(TasActionTokens.ForSlot(slot, slotAChar, slotBChar));
                break;
            case TasPressSlot.None:
                break;
        }
    }
}
