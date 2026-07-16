using System.Reflection;

namespace Celeste.Mod.QuicksaveMod.Interop;

/// <summary>
/// Optional SpeedrunTool bridge via reflection.
/// Normal (non-TAS) SaveState is not in SpeedrunTool's ModInterop exports —
/// only <c>SpeedrunTool.TasAction</c> exists for TAS slots — so we call
/// <c>SaveSlotsManager.SaveState</c> directly when the mod is loaded.
/// </summary>
public static class SpeedrunToolBridge {
    private static readonly EverestModuleMetadata Meta = new() {
        Name = "SpeedrunTool",
        Version = new Version(3, 27, 16),
    };

    private static bool? _loaded;
    private static MethodInfo? _saveState;
    private static bool _resolveAttempted;

    public static bool IsLoaded => _loaded ??= Everest.Loader.DependencyLoaded(Meta);

    /// <summary>
    /// Attempts a SpeedrunTool savestate on the current slot.
    /// On success, SpeedrunTool applies its own post-save freeze.
    /// </summary>
    public static bool TrySaveState() {
        if (!IsLoaded) {
            return false;
        }

        EnsureResolved();
        if (_saveState == null) {
            return false;
        }

        try {
            object?[] args = [null];
            return (bool)_saveState.Invoke(null, args)!;
        } catch (Exception e) {
            Logger.Warn(
                nameof(SpeedrunToolBridge),
                $"SpeedrunTool SaveState failed: {e.InnerException?.Message ?? e.Message}"
            );
            return false;
        }
    }

    private static void EnsureResolved() {
        if (_resolveAttempted) {
            return;
        }

        _resolveAttempted = true;

        foreach (EverestModule module in Everest.Modules) {
            if (module.Metadata.Name != Meta.Name) {
                continue;
            }

            Type? type = module.GetType().Assembly
                .GetType("Celeste.Mod.SpeedrunTool.SaveLoad.SaveSlotsManager");
            if (type == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find SaveSlotsManager type.");
                return;
            }

            _saveState = type.GetMethod(
                "SaveState",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string).MakeByRefType()],
                modifiers: null
            );

            if (_saveState == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find SaveSlotsManager.SaveState.");
            }

            return;
        }
    }
}
