using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq.Expressions;
using System.Reflection;

namespace Celeste.Mod.QuicksaveMod.Interop;

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

    internal static void ConfigureRaceTimer(GhostFinishData? finish) {
        if (!IsLoaded || !IsEnabled || finish == null) {
            return;
        }

        EnsureRaceTimerResolved();
        pendingRaceFinish = finish;
        try {
            switchRoomTimer?.Invoke(null, [currentRoomTimerTypeValue]);
            clearPbTimes?.Invoke(null, [false]);
        } catch (Exception e) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to configure SRT race timer: {e.Message}");
        }
    }

    internal static void UpdateRaceTimerEndpoint(Level level) {
        if (pendingRaceFinish == null || !IsLoaded || !IsEnabled) {
            return;
        }

        if (level.Session.Level != pendingRaceFinish.Room) {
            return;
        }

        EnsureRaceTimerResolved();
        Player? player = level.Tracker.GetEntity<Player>();
        if (player is not { Dead: false }) {
            return;
        }

        try {
            if (createEndPointAtPosition != null) {
                createEndPointAtPosition(level, player, pendingRaceFinish.Position);
            }

            seedPbTime?.Invoke(level, pendingRaceFinish.SessionTimeTicks);
            pendingRaceFinish = null;
        } catch (Exception e) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to spawn SRT race endpoint: {e.Message}");
        }
    }

    private static GhostFinishData? pendingRaceFinish;
    private static bool raceTimerResolveAttempted;
    private static object? currentRoomTimerTypeValue;
    private static MethodInfo? switchRoomTimer;
    private static MethodInfo? clearPbTimes;
    private static Action<Level, Player, Vector2>? createEndPointAtPosition;
    private static Action<Level, long>? seedPbTime;

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

    private static void EnsureRaceTimerResolved() {
        if (raceTimerResolveAttempted) {
            return;
        }

        raceTimerResolveAttempted = true;

        foreach (EverestModule module in Everest.Modules) {
            if (module.Metadata.Name != Meta.Name) {
                continue;
            }

            Assembly assembly = module.GetType().Assembly;

            Type? roomTimerManagerType = assembly.GetType("Celeste.Mod.SpeedrunTool.RoomTimer.RoomTimerManager");
            Type? roomTimerType = assembly.GetType("Celeste.Mod.SpeedrunTool.RoomTimer.RoomTimerType");
            Type? endPointType = assembly.GetType("Celeste.Mod.SpeedrunTool.RoomTimer.EndPoint");
            Type? roomTimerDataType = assembly.GetType("Celeste.Mod.SpeedrunTool.RoomTimer.RoomTimerData");

            if (roomTimerManagerType == null || roomTimerType == null || endPointType == null) {
                Logger.Warn(GhostConstants.LogTag, "Could not resolve SpeedrunTool room timer types.");
                return;
            }

            currentRoomTimerTypeValue = Enum.Parse(roomTimerType, "CurrentRoom");
            switchRoomTimer = roomTimerManagerType.GetMethod(
                "SwitchRoomTimer",
                BindingFlags.Public | BindingFlags.Static
            );
            clearPbTimes = roomTimerManagerType.GetMethod(
                "ClearPbTimes",
                BindingFlags.Public | BindingFlags.Static
            );

            ConstructorInfo? endPointCtor = endPointType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: [typeof(Player)],
                modifiers: null
            );

            if (endPointCtor != null) {
                createEndPointAtPosition = (level, player, position) => {
                    Vector2 saved = player.Position;
                    player.Position = position;
                    try {
                        endPointType.GetMethod(
                            "ClearAll",
                            BindingFlags.Public | BindingFlags.Static
                        )?.Invoke(null, null);
                        object endpoint = endPointCtor.Invoke([player]);
                        level.Add((Entity) endpoint);
                    } finally {
                        player.Position = saved;
                    }
                };
            }

            if (roomTimerDataType != null) {
                FieldInfo? currentDataField = roomTimerManagerType.GetField(
                    "CurrentRoomTimerData",
                    BindingFlags.NonPublic | BindingFlags.Static
                );
                PropertyInfo? pbTimesProperty = roomTimerDataType.GetProperty(
                    "PbTimes",
                    BindingFlags.Public | BindingFlags.Instance
                );
                MethodInfo? updateTimeKeys = roomTimerDataType.GetMethod(
                    "UpdateTimeKeys",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (currentDataField != null && pbTimesProperty != null && updateTimeKeys != null) {
                    seedPbTime = (level, ticks) => {
                        object? timerData = currentDataField.GetValue(null);
                        if (timerData == null) {
                            return;
                        }

                        updateTimeKeys.Invoke(timerData, [level]);
                        if (pbTimesProperty.GetValue(timerData) is not Dictionary<string, long> pbTimes) {
                            return;
                        }

                        string prefix = (string) roomTimerDataType
                            .GetProperty("TimeKeyPrefix", BindingFlags.Public | BindingFlags.Instance)!
                            .GetValue(timerData)!;
                        pbTimes[prefix + "EndPoint"] = ticks;
                    };
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
