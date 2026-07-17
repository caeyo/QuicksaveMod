using Celeste.Mod.QuicksaveMod.Interop;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class MovementInputSampler {
    internal static bool UsesAnalogLocomotion(Player? player) {
        if (player is not { Dead: false }) {
            return false;
        }

        int state = player.StateMachine.State;
        if (state == Player.StSwim || state == Player.StStarFly) {
            return true;
        }

        return QuicksaveModInterop.UsesAnalogLocomotion(player);
    }

    internal static void AppendCardinalDirections(Level level, List<char> actions) {
        if (level.Paused) {
            if (Input.MenuLeft.Check) {
                actions.Add('L');
            }

            if (Input.MenuRight.Check) {
                actions.Add('R');
            }

            if (Input.MenuUp.Check) {
                actions.Add('U');
            }

            if (Input.MenuDown.Check) {
                actions.Add('D');
            }

            return;
        }

        switch (Input.MoveX.Value) {
            case < 0:
                actions.Add('L');
                break;
            case > 0:
                actions.Add('R');
                break;
        }

        switch (Input.MoveY.Value) {
            case < 0:
                actions.Add('U');
                break;
            case > 0:
                actions.Add('D');
                break;
        }
    }

    internal static void AppendAnalogMovement(ref float? featherAngle, ref float? featherMagnitude) {
        Vector2 aim = Input.Aim.Value;
        if (aim == Vector2.Zero) {
            aim = CardinalFallbackAim();
        }

        if (aim == Vector2.Zero) {
            return;
        }

        float angle = MathHelper.ToDegrees(MathF.Atan2(aim.X, -aim.Y));
        if (angle < 0f) {
            angle += 360f;
        }

        featherAngle = MathF.Round(angle);
        featherMagnitude = MathHelper.Clamp(aim.Length(), 0f, 1f);
    }

    private static Vector2 CardinalFallbackAim() {
        int moveX = Input.MoveX.Value;
        int moveY = Input.MoveY.Value;
        if (moveX == 0 && moveY == 0) {
            return Vector2.Zero;
        }

        // MoveY is screen-space: negative is up, positive is down.
        return new Vector2(moveX, -moveY);
    }
}
