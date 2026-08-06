using Celeste.Mod.QuicksaveMod.Interop;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class MovementInputSampler {
    private const float AimComponentEpsilon = 1e-4f;

    // Cardinal snap tolerances from Celeste.Input.GetAimVector
    private const float AimSnapWide = MathF.PI / 8f; // 22.5°
    private const float AimSnapSkew = MathF.PI / 36f; // 5° — subtracted when angle < 0

    internal static bool UsesAnalogLocomotion(Player? player) {
        if (player is not { Dead: false }) {
            return false;
        }

        int state = player.StateMachine.State;
        if (state is Player.StSwim or Player.StStarFly) {
            return true;
        }

        return QuicksaveModInterop.UsesAnalogLocomotion(player);
    }

    internal static void AppendCardinalDirections(Level level, List<string> actions) {
        if (level.Paused) {
            if (Input.MenuLeft.Check) {
                actions.Add(TasActionTokens.Plain('L'));
            }

            if (Input.MenuRight.Check) {
                actions.Add(TasActionTokens.Plain('R'));
            }

            if (Input.MenuUp.Check) {
                actions.Add(TasActionTokens.Plain('U'));
            }

            if (Input.MenuDown.Check) {
                actions.Add(TasActionTokens.Plain('D'));
            }

            return;
        }

        ResolveAimCardinals(SnapHeldAim(Input.Aim.Value), out bool aimLeft, out bool aimRight, out bool aimUp, out bool aimDown);

        int moveX = Input.MoveX.Value;
        int moveY = Input.MoveY.Value;

        AppendDirection(actions, moveX < 0, aimLeft, 'L');
        AppendDirection(actions, moveX > 0, aimRight, 'R');
        AppendDirection(actions, moveY < 0, aimUp, 'U');
        AppendDirection(actions, moveY > 0, aimDown, 'D');
    }

    // Lifted from Celeste.Input.GetAimVector
    private static Vector2 SnapHeldAim(Vector2 value) {
        if (value == Vector2.Zero) {
            return Vector2.Zero;
        }

        float angle = value.Angle();
        float snap = angle < 0f ? AimSnapWide - AimSnapSkew : AimSnapWide;

        if (Calc.AbsAngleDiff(angle, 0f) < snap) {
            return new Vector2(1f, 0f);
        }

        if (Calc.AbsAngleDiff(angle, MathF.PI) < snap) {
            return new Vector2(-1f, 0f);
        }

        if (Calc.AbsAngleDiff(angle, -MathF.PI / 2f) < snap) {
            return new Vector2(0f, -1f);
        }

        if (Calc.AbsAngleDiff(angle, MathF.PI / 2f) < snap) {
            return new Vector2(0f, 1f);
        }

        return new Vector2(Math.Sign(value.X), Math.Sign(value.Y)).SafeNormalize();
    }

    private static void ResolveAimCardinals(
        Vector2 aim,
        out bool left,
        out bool right,
        out bool up,
        out bool down
    ) {
        left = aim.X < -AimComponentEpsilon;
        right = aim.X > AimComponentEpsilon;
        up = aim.Y < -AimComponentEpsilon;
        down = aim.Y > AimComponentEpsilon;
    }

    private static void AppendDirection(List<string> actions, bool move, bool aim, char plain) {
        if (move && aim) {
            actions.Add(TasActionTokens.Plain(plain));
        } else if (move) {
            actions.Add(TasActionTokens.Move(plain));
        } else if (aim) {
            actions.Add(TasActionTokens.Aim(plain));
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

        // MoveY is screen-space: negative is up, positive is down
        return new Vector2(moveX, -moveY);
    }
}
