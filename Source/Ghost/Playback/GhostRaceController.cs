using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostRaceController {
    private static GhostData? pendingGhost;
    private static GhostData? activeGhost;
    private static bool armed;
    private static bool started;

    public static bool IsActive => activeGhost != null;

    public static QuicksaveData? RaceAnchor => activeGhost?.Anchor.Clone();

    public static void Prepare(GhostData ghost) {
        Reset();
        pendingGhost = ghost.Clone();
        armed = true;
    }

    public static void Reset() {
        pendingGhost = null;
        activeGhost = null;
        armed = false;
        started = false;
        GhostReplayerEntity.Instance?.RemoveSelf();
    }

    public static void OnLoadFreezeEnded() {
        if (!armed || pendingGhost == null || started) {
            return;
        }

        activeGhost = pendingGhost;
        pendingGhost = null;
        StartRacePlayback();
    }

    public static void TryStartOnPlayerInput() {
        if (!armed || started || QuicksaveLoadFreeze.IsWaiting || SpeedrunToolBridge.IsGameFrozen) {
            return;
        }

        if (Engine.Scene is not Level) {
            return;
        }

        OnLoadFreezeEnded();
    }

    public static void OnSrtLoadBackToAnchor() {
        if (!IsActive || activeGhost == null) {
            return;
        }

        started = false;
        armed = true;
        pendingGhost = activeGhost.Clone();
        activeGhost = null;
        GhostReplayerEntity.Instance?.RemoveSelf();
    }

    private static void StartRacePlayback() {
        if (activeGhost == null || Engine.Scene is not Level level) {
            return;
        }

        started = true;
        armed = false;

        Ghost ghost = new(activeGhost.Rooms) { ForceSync = false };
        level.Add(ghost);
        level.Add(new GhostReplayerEntity(ghost));

        if (ShouldConfigureRaceTimer()) {
            SpeedrunToolBridge.ConfigureRaceTimer(activeGhost.Finish);
        }

        Logger.Info(GhostConstants.LogTag, "Ghost race playback started.");
    }

    private static bool ShouldConfigureRaceTimer() {
        return QuicksaveModModule.Settings.AddTimerToRace
            && SpeedrunToolBridge.IsLoaded
            && SpeedrunToolBridge.IsEnabled;
    }
}
