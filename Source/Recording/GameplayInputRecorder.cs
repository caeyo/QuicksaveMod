using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class GameplayInputRecorder {
    private static TasActionsMapper? mapper;

    public static void EnsureMapper() {
        mapper ??= new TasActionsMapper();
    }

    internal static TasActionsMapper? GetMapper() => mapper;

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

        bool trackQuicksave = QuicksaveTracker.IsTracking;
        bool trackGhost = GhostRecordingSession.IsRecordingInputs;
        if (!trackQuicksave && !trackGhost) {
            return;
        }

        if (!ShouldRecordFrame(level)) {
            return;
        }

        Player? player = level.Tracker.GetEntity<Player>();
        if (trackQuicksave && player is { Dead: true }) {
            trackQuicksave = false;
            if (!trackGhost) {
                return;
            }
        }

        InputLineBuffer? quicksaveBuffer = trackQuicksave ? QuicksaveTracker.Buffer : null;
        InputLineBuffer? ghostBuffer = trackGhost ? GhostRecordingSession.InputBuffer : null;
        if (quicksaveBuffer == null) {
            mapper.Sample(level, player, ghostBuffer!);
        } else if (ghostBuffer == null) {
            mapper.Sample(level, player, quicksaveBuffer);
        } else {
            // One Sample per frame — TwoSlotEncoder state must not advance twice (breaks J/K, X/C, etc.).
            mapper.Sample(level, player, quicksaveBuffer, ghostBuffer);
        }

        if (trackGhost) {
            GhostRecordingSession.CaptureFrame(player);
        }
    }

    internal static bool ShouldRecordFrame(Level level) =>
        !IsSuspended
        && !QuicksaveLoadFreeze.IsWaiting
        && !SpeedrunToolBridge.IsGameFrozen
        && CelesteTasImports.IsTasActive?.Invoke() != true
        && level is not { Paused: true, PauseMainMenuOpen: false };

    public static bool IsSuspended { get; private set; }

    public static void Suspend() => IsSuspended = true;

    public static void Resume() => IsSuspended = false;
}
