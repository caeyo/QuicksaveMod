namespace Celeste.Mod.QuickTools.Recording;

internal static class TasActionTokens {
    internal const string Pause = "S";
    internal const string QuickRestart = "Q";
    internal const string MenuConfirm = "O";

    private const string JumpA = "J";
    private const string JumpB = "K";
    private const string DashA = "X";
    private const string DashB = "C";
    private const string CrouchDashA = "Z";
    private const string CrouchDashB = "V";
    private const string GrabA = "G";
    private const string GrabB = "H";

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
