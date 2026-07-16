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
    private static PropertyInfo? _stateManagerInstance;
    private static PropertyInfo? _stateManagerState;
    private static bool _resolveAttempted;

    public static bool IsLoaded => _loaded ??= Everest.Loader.DependencyLoaded(Meta);

    /// <summary>
    /// True while SpeedrunTool is saving, loading, or frozen waiting for input.
    /// </summary>
    public static bool IsGameFrozen {
        get {
            if (!IsLoaded) {
                return false;
            }

            EnsureResolved();
            if (_stateManagerInstance == null || _stateManagerState == null) {
                return false;
            }

            try {
                object? manager = _stateManagerInstance.GetValue(null);
                if (manager == null) {
                    return false;
                }

                object? state = _stateManagerState.GetValue(manager);
                // State.None == 0; Saving / Loading / Waiting are non-zero.
                return state != null && Convert.ToInt32(state) != 0;
            } catch {
                return false;
            }
        }
    }

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

            Assembly assembly = module.GetType().Assembly;

            Type? saveSlotsType = assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.SaveSlotsManager");
            if (saveSlotsType == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find SaveSlotsManager type.");
                return;
            }

            _saveState = saveSlotsType.GetMethod(
                "SaveState",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string).MakeByRefType()],
                modifiers: null
            );

            if (_saveState == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find SaveSlotsManager.SaveState.");
            }

            Type? stateManagerType = assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager");
            if (stateManagerType == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find StateManager type.");
                return;
            }

            _stateManagerInstance = stateManagerType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static
            );
            _stateManagerState = stateManagerType.GetProperty(
                "State",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (_stateManagerInstance == null || _stateManagerState == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find StateManager.Instance/State.");
            }

            return;
        }
    }
}
