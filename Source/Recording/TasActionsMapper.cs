using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

public class TasActionsMapper
{
    private TasSlot jumpSlot = TasSlot.None;
    private TasSlot dashSlot = TasSlot.None;
    private TasSlot crouchDashSlot = TasSlot.None;
    private TasSlot grabSlot = TasSlot.None;

    public void Reset()
    {
        jumpSlot = TasSlot.None;
        dashSlot = TasSlot.None;
        crouchDashSlot = TasSlot.None;
        grabSlot = TasSlot.None;
    }

    public string Sample(Level level)
    {
        var actions = new List<char>();
        float? featherAngle = null;
        float? featherMagnitude = null;

        AppendDirections(level, actions);
        AppendJump(actions);
        AppendDash(level, actions);
        AppendCrouchDash(actions);
        AppendGrab(actions);
        AppendFeather(ref featherAngle, ref featherMagnitude);
        AppendMenuInputs(level, actions);

        return TasLineFormatter.Format(1, actions, featherAngle, featherMagnitude);
    }

    private void AppendDirections(Level level, List<char> actions)
    {
        int moveX = GetHorizontalInput(level);
        int moveY = GetVerticalInput(level);

        switch (moveX)
        {
            case < 0:
                actions.Add('L');
                break;
            case > 0:

                actions.Add('R');
                break;
        }

        switch (moveY)
        {
            case < 0:
                actions.Add('D');
                break;
            case > 0:
                actions.Add('U');
                break;
        }
    }

    private static int GetHorizontalInput(Level level)
    {
        if (level.Paused)
        {
            if (Input.MenuLeft.Check)
            {
                return -1;
            }

            if (Input.MenuRight.Check)
            {
                return 1;
            }
        }

        return Input.MoveX.Value;
    }

    private static int GetVerticalInput(Level level)
    {
        if (level.Paused)
        {
            if (Input.MenuUp.Check)
            {
                return 1;
            }

            if (Input.MenuDown.Check)
            {
                return -1;
            }
        }

        return Input.MoveY.Value;
    }

    private void AppendJump(List<char> actions)
    {
        jumpSlot = TwoSlotEncoder.Update(jumpSlot, Input.Jump.Pressed, Input.Jump.Check);
        AppendSlot(actions, jumpSlot, 'J', 'K');
    }

    private void AppendDash(Level level, List<char> actions)
    {
        if (IsMenuContext(level) && Input.MenuCancel.Pressed)
        {
            actions.Add('C');
            return;
        }

        if (IsMenuContext(level))
        {
            return;
        }

        dashSlot = TwoSlotEncoder.Update(dashSlot, Input.Dash.Pressed, Input.Dash.Check);
        AppendSlot(actions, dashSlot, 'X', 'C');
    }

    private void AppendCrouchDash(List<char> actions)
    {
        crouchDashSlot = TwoSlotEncoder.Update(
            crouchDashSlot,
            Input.CrouchDash.Pressed,
            Input.CrouchDash.Check
        );
        AppendSlot(actions, crouchDashSlot, 'Z', 'V');
    }

    private void AppendGrab(List<char> actions)
    {
        grabSlot = TwoSlotEncoder.Update(grabSlot, Input.Grab.Pressed, Input.GrabCheck);
        AppendSlot(actions, grabSlot, 'G', 'H');
    }

    private static void AppendFeather(
        ref float? featherAngle,
        ref float? featherMagnitude
    )
    {
        Vector2 aim = Input.Feather.Value;
        if (aim == Vector2.Zero)
        {
            return;
        }

        float angle = MathHelper.ToDegrees(MathF.Atan2(-aim.Y, aim.X));
        if (angle < 0f)
        {
            angle += 360f;
        }

        featherAngle = angle;
        featherMagnitude = MathHelper.Clamp(aim.Length(), 0f, 1f);
    }

    private static void AppendMenuInputs(Level level, List<char> actions)
    {
        if (Input.Pause.Pressed || Input.Pause.Check)
        {
            actions.Add('S');
        }

        if (Input.QuickRestart.Pressed)
        {
            actions.Add('Q');
        }

        if (Input.MenuConfirm.Pressed)
        {
            actions.Add('O');
        }

        if (Input.MenuJournal.Pressed || !IsMenuContext(level) && Input.Talk.Pressed)
        {
            actions.Add('N');
        }
    }

    private static bool IsMenuContext(Level level)
    {
        if (level.Paused)
        {
            return true;
        }

        return Engine.Scene.Tracker.GetEntity<Textbox>() != null;
    }

    private static void AppendSlot(List<char> actions, TasSlot slot, char slotAChar, char slotBChar)
    {
        switch (slot)
        {
            case TasSlot.A:
            case TasSlot.B:
                actions.Add(TwoSlotEncoder.CharFor(slot, slotAChar, slotBChar));
                break;
            case TasSlot.None:
                break;
        }
    }
}
