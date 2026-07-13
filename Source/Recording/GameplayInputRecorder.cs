using Celeste;
using Celeste.Mod.QuicksaveMod.Interop;
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

        if (mapper == null || Engine.Scene is not Level level || !ShouldRecord(level)) {
            return;
        }

        QuicksaveTracker.Instance.RecordFrame(mapper.Sample(level));
    }

    public static void ResetMapper() {
        mapper?.Reset();
    }

    private static bool ShouldRecord(Level level) =>
        QuicksaveTracker.Instance.IsTracking
        && !IsSuspended
        && CelesteTasImports.IsTasActive?.Invoke() != true
        && level.Tracker.GetEntity<Player>() is not { Dead: true };

    public static bool IsSuspended { get; private set; }

    public static void Suspend() => IsSuspended = true;

    public static void Resume() => IsSuspended = false;
}
