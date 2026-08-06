using Celeste.Mod.QuickTools.Quicksave;
using System.Linq.Expressions;
using System.Reflection;

namespace Celeste.Mod.QuickTools.Interop;

public static class SpeedrunToolBridge {
    private static readonly EverestModuleMetadata Meta = new() {
        Name = "SpeedrunTool",
        Version = new Version(3, 27, 16),
    };

    private static readonly object?[] SaveStateArgs = [null];

    private static bool? loaded;
    private static MethodInfo? saveState;
    private static Func<object?>? getStateManagerInstance;
    private static Func<object, int>? getStateAsInt;
    private static Func<bool>? getEnabled;
    private static bool resolveAttempted;

    public static bool IsLoaded => loaded ??= Everest.Loader.DependencyLoaded(Meta);

    public static bool IsEnabled {
        get {
            if (!IsLoaded) {
                return false;
            }

            EnsureResolved();
            return getEnabled?.Invoke() ?? false;
        }
    }

    // True while SpeedrunTool is saving, loading, or frozen waiting for input
    public static bool IsGameFrozen {
        get {
            if (!IsLoaded) {
                return false;
            }

            EnsureResolved();
            if (getStateManagerInstance == null || getStateAsInt == null) {
                return false;
            }

            try {
                object? manager = getStateManagerInstance();
                if (manager == null) {
                    return false;
                }

                // State.None == 0; Saving / Loading / Waiting are non-zero
                return getStateAsInt(manager) != 0;
            } catch {
                return false;
            }
        }
    }

    // On success, SpeedrunTool applies its own post-save freeze
    public static bool TrySaveState() {
        if (!IsLoaded || !IsEnabled) {
            return false;
        }

        EnsureResolved();
        if (saveState == null) {
            return false;
        }

        try {
            SaveStateArgs[0] = null;
            return (bool) saveState.Invoke(null, SaveStateArgs)!;
        } catch (Exception e) {
            Logger.Warn(
                QuicksaveConstants.LogTag,
                $"SpeedrunTool SaveState failed: {e.InnerException?.Message ?? e.Message}"
            );
            return false;
        }
    }

    private static void EnsureResolved() {
        if (resolveAttempted) {
            return;
        }

        resolveAttempted = true;

        foreach (EverestModule module in Everest.Modules) {
            if (module.Metadata.Name != Meta.Name) {
                continue;
            }

            Assembly assembly = module.GetType().Assembly;

            Type? saveSlotsType = assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.SaveSlotsManager");
            if (saveSlotsType == null) {
                Logger.Warn(QuicksaveConstants.LogTag, "Could not find SaveSlotsManager type.");
                return;
            }

            saveState = saveSlotsType.GetMethod(
                "SaveState",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string).MakeByRefType()],
                modifiers: null
            );

            if (saveState == null) {
                Logger.Warn(QuicksaveConstants.LogTag, "Could not find SaveSlotsManager.SaveState.");
            }

            Type? stateManagerType = assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager");
            if (stateManagerType == null) {
                Logger.Warn(QuicksaveConstants.LogTag, "Could not find StateManager type.");
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
                Logger.Warn(QuicksaveConstants.LogTag, "Could not find StateManager.Instance/State.");
                return;
            }

            try {
                getStateManagerInstance = CompileStaticObjectGetter(instanceProperty);
                getStateAsInt = CompileInstanceEnumAsIntGetter(stateProperty, stateManagerType);
            } catch (Exception e) {
                Logger.Warn(
                    QuicksaveConstants.LogTag,
                    $"Failed to compile StateManager accessors: {e.Message}"
                );
            }

            Type? settingsType = assembly.GetType("Celeste.Mod.SpeedrunTool.SpeedrunToolSettings");
            PropertyInfo? settingsInstanceProperty = settingsType?.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static
            );
            PropertyInfo? enabledProperty = settingsType?.GetProperty(
                "Enabled",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (settingsInstanceProperty != null && enabledProperty != null && settingsType != null) {
                try {
                    getEnabled = CompileSettingsEnabledGetter(settingsInstanceProperty, enabledProperty);
                } catch (Exception e) {
                    Logger.Warn(
                        QuicksaveConstants.LogTag,
                        $"Failed to compile SpeedrunToolSettings.Enabled accessor: {e.Message}"
                    );
                }
            }

            return;
        }
    }

    private static Func<bool> CompileSettingsEnabledGetter(
        PropertyInfo instanceProperty,
        PropertyInfo enabledProperty
    ) {
        MemberExpression instance = Expression.Property(null, instanceProperty);
        MemberExpression enabled = Expression.Property(instance, enabledProperty);
        return Expression.Lambda<Func<bool>>(enabled).Compile();
    }

    private static Func<object?> CompileStaticObjectGetter(PropertyInfo property) {
        Expression body = Expression.Convert(Expression.Property(null, property), typeof(object));
        return Expression.Lambda<Func<object?>>(body).Compile();
    }

    private static Func<object, int> CompileInstanceEnumAsIntGetter(PropertyInfo property, Type instanceType) {
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        UnaryExpression castInstance = Expression.Convert(instanceParam, instanceType);
        MemberExpression propertyAccess = Expression.Property(castInstance, property);
        UnaryExpression asInt = Expression.Convert(propertyAccess, typeof(int));
        return Expression.Lambda<Func<object, int>>(asInt, instanceParam).Compile();
    }
}
