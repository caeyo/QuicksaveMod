using MonoMod.ModInterop;

namespace Celeste.Mod.QuickTools.Interop;

[ModImportName("CelesteTAS")]
public static class CelesteTasImports {
    public static Func<bool>? IsTasActive;
}
