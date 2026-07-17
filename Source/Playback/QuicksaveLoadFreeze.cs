using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Playback;

internal static class QuicksaveLoadFreeze {
    private static readonly VirtualInput[] UnfreezeInputs = [
        Input.Dash,
        Input.Jump,
        Input.Grab,
        Input.MoveX,
        Input.MoveY,
        Input.Aim,
        Input.Pause,
        Input.CrouchDash,
        Input.MenuLeft,
        Input.MenuRight,
        Input.MenuUp,
        Input.MenuDown,
        Input.MenuConfirm,
        Input.MenuCancel,
    ];

    public static bool IsWaiting { get; private set; }

    public static void Begin() {
        IsWaiting = true;
        Logger.Info(QuicksaveConstants.LogTag, "Waiting for input after quicksave playback.");
    }

    public static void Cancel() {
        IsWaiting = false;
    }

    public static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level level) {
        if (!IsWaiting) {
            orig(level);
            return;
        }

        level.Wipe?.Update(level);
        level.HiresSnow?.Update(level);
        level.Foreground.Update(level);
        level.Background.Update(level);
    }

    public static void OnAfterInputUpdate() {
        if (!IsWaiting || Engine.Scene is not Level) {
            return;
        }

        for (int i = 0; i < UnfreezeInputs.Length; i++) {
            if (IsActive(UnfreezeInputs[i])) {
                Cancel();
                Logger.Info(QuicksaveConstants.LogTag, "Resumed after input.");
                return;
            }
        }
    }

    private static bool IsActive(VirtualInput input) {
        return input switch {
            VirtualButton button => button.Pressed || button.Check,
            VirtualIntegerAxis axis => axis.turned || axis.Value != 0,
            VirtualJoystick stick => stick.hTurned || stick.vTurned
                || stick.Value != Microsoft.Xna.Framework.Vector2.Zero,
            VirtualAxis axis => axis.Value != 0,
            _ => false,
        };
    }
}
