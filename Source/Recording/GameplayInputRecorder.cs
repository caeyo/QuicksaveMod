using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

public static class GameplayInputRecorder {
    private static TasActionsMapper? mapper;

    public static void Apply() {
        mapper = new TasActionsMapper();
        On.Monocle.MInput.Update += OnMInputUpdate;
    }

    public static void Unapply() {
        On.Monocle.MInput.Update -= OnMInputUpdate;
        mapper = null;
    }

    private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();

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

        QuicksaveTracker.Instance.RecordFrame(mapper, level, player);
    }

    public static void ResetMapper() {
        mapper?.Reset();
    }

    /// <summary>Gates that do not need a Player lookup.</summary>
    private static bool ShouldRecordCheap(Level level) =>
        QuicksaveTracker.Instance.IsTracking
        && !IsSuspended
        && !QuicksaveLoadFreeze.IsWaiting
        && !SpeedrunToolBridge.IsGameFrozen
        && CelesteTasImports.IsTasActive?.Invoke() != true
        && !(level.Paused && !level.PauseMainMenuOpen);

    public static bool IsSuspended { get; private set; }

    public static void Suspend() => IsSuspended = true;

    public static void Resume() => IsSuspended = false;
}
