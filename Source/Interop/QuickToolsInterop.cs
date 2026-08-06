using MonoMod.ModInterop;

namespace Celeste.Mod.QuickTools.Interop;

// Lets other mods register player states that consume analog locomotion (Input.Aim)
// instead of cardinal MoveX/MoveY, so quicksave recording emits F lines correctly.
[ModExportName("QuickTools")]
public static class QuickToolsInterop {
    public static Action<Func<Player, bool>>? RegisterAnalogCheck;
    public static Action? ClearAnalogChecks;

    private static readonly List<Func<Player, bool>> AnalogChecks = [];

    internal static void InitExports() {
        RegisterAnalogCheck = Register;
        ClearAnalogChecks = Clear;
    }

    internal static bool UsesAnalogLocomotion(Player? player) {
        if (player is not { Dead: false }) {
            return false;
        }

        foreach (Func<Player, bool> check in AnalogChecks) {
            if (check(player)) {
                return true;
            }
        }

        return false;
    }

    private static void Register(Func<Player, bool> check) {
        AnalogChecks.Add(check);
    }

    private static void Clear() {
        AnalogChecks.Clear();
    }
}
