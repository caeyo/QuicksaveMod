using Monocle;

namespace Celeste.Mod.QuicksaveMod.Playback;

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
    ];

    public static bool IsWaiting { get; private set; }

    public static void Apply() {
        On.Celeste.Level.Update += UpdateBackdropWhenWaiting;
        On.Monocle.Scene.BeforeUpdate += ThawOnInput;
    }

    public static void Unapply() {
        On.Celeste.Level.Update -= UpdateBackdropWhenWaiting;
        On.Monocle.Scene.BeforeUpdate -= ThawOnInput;
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

    private static void ThawOnInput(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        if (IsWaiting && self is Level && UnfreezeInputs.Any(IsActive)) {
            Cancel();
            Logger.Info(nameof(QuicksaveLoadFreeze), "Resumed after input.");
        }

        orig(self);
    }

    private static bool IsActive(VirtualInput input) => input switch {
        VirtualButton button => button.Pressed || button.Check,
        VirtualIntegerAxis axis => axis.turned || axis.Value != 0,
        VirtualJoystick stick => stick.hTurned || stick.vTurned || stick.Value != Microsoft.Xna.Framework.Vector2.Zero,
        VirtualAxis axis => axis.Value != 0,
        _ => false,
    };
}
