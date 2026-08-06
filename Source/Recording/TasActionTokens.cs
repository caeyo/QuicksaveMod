namespace Celeste.Mod.QuickTools.Recording;

internal static class TasActionTokens {
    internal static readonly string Pause = "S";
    internal static readonly string QuickRestart = "Q";
    internal static readonly string MenuConfirm = "O";

    internal static readonly string JumpA = "J";
    internal static readonly string JumpB = "K";
    internal static readonly string DashA = "X";
    internal static readonly string DashB = "C";
    internal static readonly string CrouchDashA = "Z";
    internal static readonly string CrouchDashB = "V";
    internal static readonly string GrabA = "G";
    internal static readonly string GrabB = "H";

    private static readonly string[] PlainByIndex = ["L", "R", "U", "D"];
    private static readonly string[] MoveByIndex = ["ML", "MR", "MU", "MD"];
    private static readonly string[] AimByIndex = ["AL", "AR", "AU", "AD"];

    internal static string Plain(char direction) => PlainByIndex[IndexForDirection(direction)];

    internal static string Move(char direction) => MoveByIndex[IndexForDirection(direction)];

    internal static string Aim(char direction) => AimByIndex[IndexForDirection(direction)];

    internal static string ForSlot(TasPressSlot slot, char slotAChar, char slotBChar) =>
        slot switch {
            TasPressSlot.A => ForChar(slotAChar),
            TasPressSlot.B => ForChar(slotBChar),
            _ => throw new InvalidOperationException("Unexpected press slot."),
        };

    private static string ForChar(char value) =>
        value switch {
            'J' => JumpA,
            'K' => JumpB,
            'X' => DashA,
            'C' => DashB,
            'Z' => CrouchDashA,
            'V' => CrouchDashB,
            'G' => GrabA,
            'H' => GrabB,
            _ => value.ToString(),
        };

    private static int IndexForDirection(char direction) =>
        direction switch {
            'L' => 0,
            'R' => 1,
            'U' => 2,
            'D' => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction."),
        };
}
