using System.Linq.Expressions;
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

    private static readonly object?[] SaveStateArgs = [null];

    private static bool? _loaded;
    private static MethodInfo? _saveState;
    private static Func<object?>? _getStateManagerInstance;
    private static Func<object, int>? _getStateAsInt;
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
            if (_getStateManagerInstance == null || _getStateAsInt == null) {
                return false;
            }

            try {
                object? manager = _getStateManagerInstance();
                if (manager == null) {
                    return false;
                }

                // State.None == 0; Saving / Loading / Waiting are non-zero.
                return _getStateAsInt(manager) != 0;
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
            SaveStateArgs[0] = null;
            return (bool)_saveState.Invoke(null, SaveStateArgs)!;
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

            PropertyInfo? instanceProperty = stateManagerType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static
            );
            PropertyInfo? stateProperty = stateManagerType.GetProperty(
                "State",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (instanceProperty == null || stateProperty == null) {
                Logger.Warn(nameof(SpeedrunToolBridge), "Could not find StateManager.Instance/State.");
                return;
            }

            try {
                _getStateManagerInstance = CompileStaticObjectGetter(instanceProperty);
                _getStateAsInt = CompileInstanceEnumAsIntGetter(stateProperty, stateManagerType);
            } catch (Exception e) {
                Logger.Warn(
                    nameof(SpeedrunToolBridge),
                    $"Failed to compile StateManager accessors: {e.Message}"
                );
            }

            return;
        }
    }

    private static Func<object?> CompileStaticObjectGetter(PropertyInfo property) {
        // () => (object)Property
        Expression body = Expression.Convert(Expression.Property(null, property), typeof(object));
        return Expression.Lambda<Func<object?>>(body).Compile();
    }

    private static Func<object, int> CompileInstanceEnumAsIntGetter(PropertyInfo property, Type instanceType) {
        // (object instance) => (int)((InstanceType)instance).Property
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        UnaryExpression castInstance = Expression.Convert(instanceParam, instanceType);
        MemberExpression propertyAccess = Expression.Property(castInstance, property);
        UnaryExpression asInt = Expression.Convert(propertyAccess, typeof(int));
        return Expression.Lambda<Func<object, int>>(asInt, instanceParam).Compile();
    }
}
