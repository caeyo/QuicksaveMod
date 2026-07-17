using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class GameplayInputRecorder {
    private static TasActionsMapper? mapper;

    public static void EnsureMapper() {
        mapper ??= new TasActionsMapper();
    }

    public static void ClearMapper() {
        mapper = null;
    }

    public static void ResetMapper() {
        mapper?.Reset();
    }

    public static void OnAfterInputUpdate() {
        if (mapper == null || Engine.Scene is not Level level) {
            return;
        }

        if (!ShouldRecordCheap(level)) {
            return;
        }

        Player? player = level.Tracker.GetEntity<Player>();
        if (player is { Dead: true }) {
            return;
        }

        QuicksaveTracker.RecordFrame(mapper, level, player);
    }

    private static bool ShouldRecordCheap(Level level) =>
        QuicksaveTracker.IsTracking
        && !IsSuspended
        && !SpeedrunToolBridge.IsGameFrozen
        && CelesteTasImports.IsTasActive?.Invoke() != true
        && !(level.Paused && !level.PauseMainMenuOpen);

    public static bool IsSuspended { get; private set; }

    public static void Suspend() => IsSuspended = true;

    public static void Resume() => IsSuspended = false;
}
