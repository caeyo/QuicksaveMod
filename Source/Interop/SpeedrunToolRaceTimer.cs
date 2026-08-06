using Celeste.Mod.QuickTools.Ghost;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq.Expressions;
using System.Reflection;

namespace Celeste.Mod.QuickTools.Interop;

/// <summary>
/// Ghost-race integration with SpeedrunTool's room timer and physical EndPoint flag.
/// </summary>
internal static class SpeedrunToolRaceTimer {
    private static long ghostPbTicks;
    private static string finishRoom = "";
    private static Vector2 finishPosition;
    private static string pbKey = "";

    private static bool endPointSpawned;
    private static bool endPointCollidable;
    private static bool endPointTouched;
    private static Entity? raceEndPoint;

    private static Dictionary<string, long>? pbTimes;
    private static Dictionary<string, long>? lastPbTimes;

    private static GhostFinishData? scheduledFinish;

    private static bool resolved;
    private static object? currentRoomTimerMode;
    private static MethodInfo? switchRoomTimer;
    private static MethodInfo? resetTimer;
    private static Action? clearEndPoints;
    private static MethodInfo? endPointReadyForTime;
    private static ConstructorInfo? endPointCtor;
    private static FieldInfo? endPointRoomNameField;
    private static Func<Entity, bool>? isEndPointActivated;

    public static bool IsActive => ghostPbTicks > 0;

    public static bool IsWatchingFlagTouch => ghostPbTicks > 0 && endPointCollidable && !endPointTouched;

    public static bool NeedsReplayerSupport => IsActive && !endPointTouched;

    internal static void WarmUp() {
        if (SpeedrunToolBridge.IsLoaded) {
            Resolve();
        }
    }

    public static void ScheduleBegin(GhostFinishData finish) {
        if (!SpeedrunToolBridge.IsLoaded || !SpeedrunToolBridge.IsEnabled) {
            return;
        }

        scheduledFinish = finish;
    }

    public static void TryBeginScheduled(Level level) {
        if (scheduledFinish == null) {
            return;
        }

        GhostFinishData finish = scheduledFinish;
        scheduledFinish = null;
        Begin(level, finish);
    }

    private static void Begin(Level level, GhostFinishData finish) {
        if (!SpeedrunToolBridge.IsLoaded || !SpeedrunToolBridge.IsEnabled) {
            return;
        }

        Resolve();
        End();

        ghostPbTicks = finish.SessionTimeTicks;
        finishRoom = finish.Room;
        finishPosition = finish.Position;
        pbKey = level.Session.Area + level.Session.Level + "EndPoint";
        ResetEndpointTracking();

        Player? player = level.Tracker.GetEntity<Player>();
        if (player is not { Dead: false }) {
            Logger.Warn(GhostConstants.LogTag, "Cannot begin SRT race timer without a living player.");
            ClearSession();
            return;
        }

        try {
            switchRoomTimer?.Invoke(null, [currentRoomTimerMode]);
            resetTimer?.Invoke(null, Array.Empty<object>());
            if (!level.TimerStarted) {
                level.TimerStarted = true;
            }

            CachePbDictionaries();
            SpawnEndPoint(level, player);
            TryEnableEndPointCollision(level);
            PinGhostPb();
        } catch (Exception e) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to begin SRT race timer: {e.Message}");
            End();
        }
    }

    public static void RefreshAfterLoad() {
        endPointTouched = false;
        PinGhostPb();
    }

    public static void End() {
        scheduledFinish = null;
        ClearEndPoints();

        if (!IsActive) {
            return;
        }

        ClearSession();
    }

    public static void ClearEndPoints() {
        if (!SpeedrunToolBridge.IsLoaded) {
            return;
        }

        Resolve();

        try {
            clearEndPoints?.Invoke();
        } catch (Exception e) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to clear SRT EndPoints: {e.Message}");
        }
    }

    public static void OnRoomChanged(Level level) {
        if (!IsActive) {
            return;
        }

        TryEnableEndPointCollision(level);
    }

    public static void WatchFlagTouch() {
        if (!IsWatchingFlagTouch || raceEndPoint == null || isEndPointActivated == null) {
            return;
        }

        if (!isEndPointActivated(raceEndPoint)) {
            return;
        }

        PinGhostPb();
        endPointTouched = true;
    }

    private static void PinGhostPb() {
        if (!IsActive || pbKey.Length == 0 || pbTimes == null || lastPbTimes == null) {
            return;
        }

        pbTimes[pbKey] = ghostPbTicks;
        lastPbTimes[pbKey] = ghostPbTicks;
    }

    private static void SpawnEndPoint(Level level, Player player) {
        if (endPointSpawned || endPointCtor == null) {
            return;
        }

        Vector2 saved = player.Position;
        player.Position = finishPosition;
        try {
            raceEndPoint = (Entity) endPointCtor.Invoke([player]);
            endPointRoomNameField?.SetValue(raceEndPoint, finishRoom);
            level.Add(raceEndPoint);
        }
        finally {
            player.Position = saved;
        }

        endPointSpawned = true;
    }

    private static void TryEnableEndPointCollision(Level level) {
        if (!endPointSpawned || endPointCollidable || !IsInFinishRoom(level) || raceEndPoint == null) {
            return;
        }

        if (endPointReadyForTime == null) {
            return;
        }

        try {
            endPointReadyForTime.Invoke(raceEndPoint, null);
            endPointCollidable = true;
        } catch (Exception e) {
            Logger.Warn(GhostConstants.LogTag, $"Failed to enable SRT EndPoint collision: {e.Message}");
        }
    }

    private static bool IsInFinishRoom(Level level) {
        return string.Equals(level.Session.Level, finishRoom, StringComparison.OrdinalIgnoreCase);
    }

    private static void CachePbDictionaries() {
        if (getTimerData == null || getPbTimes == null || getLastPbTimes == null) {
            pbTimes = null;
            lastPbTimes = null;
            return;
        }

        object? timerData = getTimerData();
        if (timerData == null) {
            pbTimes = null;
            lastPbTimes = null;
            return;
        }

        pbTimes = getPbTimes(timerData);
        lastPbTimes = getLastPbTimes(timerData);
    }

    private static void ResetEndpointTracking() {
        endPointSpawned = false;
        endPointCollidable = false;
        endPointTouched = false;
        raceEndPoint = null;
    }

    private static void ClearSession() {
        ghostPbTicks = 0;
        finishRoom = "";
        finishPosition = Vector2.Zero;
        pbKey = "";
        pbTimes = null;
        lastPbTimes = null;
        ResetEndpointTracking();
    }

    private static Func<object?>? getTimerData;
    private static Func<object, Dictionary<string, long>>? getPbTimes;
    private static Func<object, Dictionary<string, long>>? getLastPbTimes;

    private static void Resolve() {
        if (resolved) {
            return;
        }

        resolved = true;

        foreach (EverestModule module in Everest.Modules) {
            if (module.Metadata.Name != "SpeedrunTool") {
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

            currentRoomTimerMode = Enum.Parse(roomTimerType, "CurrentRoom");
            switchRoomTimer = roomTimerManagerType.GetMethod(
                "SwitchRoomTimer",
                BindingFlags.Public | BindingFlags.Static
            );
            resetTimer = roomTimerManagerType.GetMethod(
                "ResetTime",
                BindingFlags.Public | BindingFlags.Static
            );

            MethodInfo? clearAll = endPointType.GetMethod("ClearAll", BindingFlags.Public | BindingFlags.Static);
            clearEndPoints = clearAll?.CreateDelegate<Action>();

            endPointCtor = endPointType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: [typeof(Player)],
                modifiers: null
            );
            endPointRoomNameField = endPointType.GetField(
                "roomName",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            PropertyInfo? activatedProperty = endPointType.GetProperty(
                "Activated",
                BindingFlags.Public | BindingFlags.Instance
            );
            if (activatedProperty != null) {
                isEndPointActivated = CompileActivatedGetter(activatedProperty, endPointType);
            }

            endPointReadyForTime = endPointType.GetMethod(
                "ReadyForTime",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (roomTimerDataType == null) {
                return;
            }

            FieldInfo? timerDataField = roomTimerManagerType.GetField(
                "CurrentRoomTimerData",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            PropertyInfo? pbTimesProperty = roomTimerDataType.GetProperty(
                "PbTimes",
                BindingFlags.Public | BindingFlags.Instance
            );
            FieldInfo? lastPbTimesField = roomTimerDataType.GetField(
                "lastPbTimes",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (timerDataField != null && pbTimesProperty != null && lastPbTimesField != null) {
                getTimerData = () => timerDataField.GetValue(null);
                getPbTimes = data => (Dictionary<string, long>) pbTimesProperty.GetValue(data)!;
                getLastPbTimes = data => (Dictionary<string, long>) lastPbTimesField.GetValue(data)!;
            }

            return;
        }
    }

    private static Func<Entity, bool> CompileActivatedGetter(PropertyInfo property, Type endPointType) {
        ParameterExpression entityParam = Expression.Parameter(typeof(Entity), "entity");
        UnaryExpression castEntity = Expression.Convert(entityParam, endPointType);
        MemberExpression activated = Expression.Property(castEntity, property);
        return Expression.Lambda<Func<Entity, bool>>(activated, entityParam).Compile();
    }
}
