using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Interop;

[ModImportName("CelesteTAS")]
public static class CelesteTasImports {
    public static Func<bool>? IsTasActive;
}
