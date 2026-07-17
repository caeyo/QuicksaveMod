using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

public class TasActionsMapper {
    private TasSlot jumpSlot = TasSlot.None;
    private TasSlot dashSlot = TasSlot.None;
    private TasSlot crouchDashSlot = TasSlot.None;
    private TasSlot grabSlot = TasSlot.None;

    public void Reset() {
        jumpSlot = TasSlot.None;
        dashSlot = TasSlot.None;
        crouchDashSlot = TasSlot.None;
        grabSlot = TasSlot.None;
    }

    public string Sample(Level level) {
        var actions = new List<char>();
        float? featherAngle = null;
        float? featherMagnitude = null;
        Player? player = level.Tracker.GetEntity<Player>();
        bool menu = IsMenuContext(level);

        AppendMovement(level, player, actions, ref featherAngle, ref featherMagnitude);
        if (!menu) {
            AppendJump(actions);
            AppendDash(actions);
            AppendCrouchDash(actions);
            AppendGrab(actions);
        }
        AppendMenuInputs(level, menu, actions);

        return TasLineFormatter.Format(1, actions, featherAngle, featherMagnitude);
    }

    private static void AppendMovement(
        Level level,
        Player? player,
        List<char> actions,
        ref float? featherAngle,
        ref float? featherMagnitude
    ) {
        if (MovementInputSampler.UsesAnalogLocomotion(player)) {
            MovementInputSampler.AppendAnalogMovement(ref featherAngle, ref featherMagnitude);
            return;
        }

        MovementInputSampler.AppendCardinalDirections(level, actions);
    }

    private void AppendJump(List<char> actions) {
        jumpSlot = TwoSlotEncoder.Update(jumpSlot, RawPressed(Input.Jump), Input.Jump.Check);
        AppendSlot(actions, jumpSlot, 'J', 'K');
    }

    private void AppendDash(List<char> actions) {
        dashSlot = TwoSlotEncoder.Update(dashSlot, RawPressed(Input.Dash), Input.Dash.Check);
        AppendSlot(actions, dashSlot, 'X', 'C');
    }

    private void AppendCrouchDash(List<char> actions) {
        crouchDashSlot = TwoSlotEncoder.Update(
            crouchDashSlot,
            RawPressed(Input.CrouchDash),
            Input.CrouchDash.Check
        );
        AppendSlot(actions, crouchDashSlot, 'Z', 'V');
    }

    private void AppendGrab(List<char> actions) {
        grabSlot = TwoSlotEncoder.Update(grabSlot, RawPressed(Input.Grab), Input.GrabCheck);
        AppendSlot(actions, grabSlot, 'G', 'H');
    }

    private static void AppendMenuInputs(Level level, bool menu, List<char> actions) {
        if (Input.Pause.Pressed || Input.Pause.Check) {
            actions.Add('S');
        }

        if (Input.QuickRestart.Pressed) {
            actions.Add('Q');
        }

        if (menu && Input.MenuConfirm.Pressed && ShouldRecordConfirm(level)) {
            actions.Add('O');
        }
    }

    private static bool ShouldRecordConfirm(Level level) {
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

    private static bool IsResumeOrSkipCutsceneSelected(Level level) {
        if (level.Entities.FindFirst<TextMenu>() is not { } menu
            || menu.Current is not TextMenu.Button button) {
            return false;
        }

        return button.Label == Dialog.Clean("menu_pause_resume")
            || button.Label == Dialog.Clean("menu_pause_skip_cutscene");
    }

    private static bool IsMenuContext(Level level) {
        if (level.Paused) {
            return true;
        }

        return Engine.Scene.Tracker.GetEntity<Textbox>() != null;
    }

    private static bool RawPressed(VirtualButton button) =>
        button.Binding.Pressed(button.GamepadIndex, button.Threshold);

    private static void AppendSlot(List<char> actions, TasSlot slot, char slotAChar, char slotBChar) {
        switch (slot) {
            case TasSlot.A:
            case TasSlot.B:
                actions.Add(TwoSlotEncoder.CharFor(slot, slotAChar, slotBChar));
                break;
            case TasSlot.None:
                break;
        }
    }
}
