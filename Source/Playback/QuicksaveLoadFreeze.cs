using Monocle;

namespace Celeste.Mod.QuicksaveMod.Playback;

/// <summary>
/// SpeedrunTool-style post-load freeze: skip Level.Update until a gameplay input is pressed.
/// </summary>
public static class QuicksaveLoadFreeze {
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

    public static void Apply() {
        On.Celeste.Level.Update += UpdateBackdropWhenWaiting;
        On.Monocle.MInput.Update += ThawAfterInputUpdate;
    }

    public static void Unapply() {
        On.Celeste.Level.Update -= UpdateBackdropWhenWaiting;
        On.Monocle.MInput.Update -= ThawAfterInputUpdate;
        Cancel();
    }

    public static void Begin() {
        IsWaiting = true;
        Logger.Info(nameof(QuicksaveLoadFreeze), "Waiting for input after quicksave playback.");
    }

    public static void Cancel() {
        IsWaiting = false;
    }

    private static void UpdateBackdropWhenWaiting(On.Celeste.Level.orig_Update orig, Level level) {
        if (!IsWaiting) {
            orig(level);
            return;
        }

        level.Wipe?.Update(level);
        level.HiresSnow?.Update(level);
        level.Foreground.Update(level);
        level.Background.Update(level);
    }

    private static void ThawAfterInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();

        if (!IsWaiting || Engine.Scene is not Level) {
            return;
        }

        for (int i = 0; i < UnfreezeInputs.Length; i++) {
            if (IsActive(UnfreezeInputs[i])) {
                Cancel();
                Logger.Info(nameof(QuicksaveLoadFreeze), "Resumed after input.");
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
