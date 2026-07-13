using Celeste.Mod.QuicksaveMod.Recording;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Interop;

/// <summary>
/// Lets other mods register player states that consume analog locomotion (Input.Aim)
/// instead of cardinal MoveX/MoveY, so quicksave recording emits F lines correctly.
/// </summary>
[ModExportName("QuicksaveMod")]
public static class QuicksaveModInterop {
    public static Action<Func<Player, bool>>? RegisterAnalogLocomotionCheck;
    public static Action? ClearAnalogLocomotionChecks;

    internal static void InitExports() {
        RegisterAnalogLocomotionCheck = MovementInputSampler.RegisterAnalogLocomotionCheck;
        ClearAnalogLocomotionChecks = MovementInputSampler.ClearAnalogLocomotionChecks;
    }
}
