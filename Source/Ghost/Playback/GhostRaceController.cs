using Celeste.Mod.QuicksaveMod.Ghost;
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
    private static bool anchorPlaybackComplete;
    private static bool started;

    public static bool IsActive => activeGhost != null;
    public static bool IsArmed => armed;

    public static QuicksaveData? RaceAnchor => activeGhost?.Anchor.Clone();

    public static void Prepare(GhostData ghost) {
        Reset();
        GhostRecordingSession.Reset();
        pendingGhost = ghost.Clone();
        armed = true;
    }

    public static void Reset() {
        pendingGhost = null;
        activeGhost = null;
        armed = false;
        anchorPlaybackComplete = false;
        started = false;
        GhostReplayerEntity.Instance?.RemoveSelf();
        SpeedrunToolRaceTimer.End();
    }

    public static void OnAnchorPlaybackComplete() {
        if (!armed) {
            return;
        }

        anchorPlaybackComplete = true;
    }

    public static void OnLoadFreezeEnded() {
        TryStartRace(requireLoadFreezeEnded: true);
    }

    public static void TryStartOnPlayerInput() {
        TryStartRace(requireLoadFreezeEnded: false);
    }

    public static void OnSrtLoadBackToAnchor() {
        if (!IsActive || activeGhost == null) {
            return;
        }

        started = false;
        armed = true;
        anchorPlaybackComplete = true;
        pendingGhost = activeGhost.Clone();
        activeGhost = null;
        GhostReplayerEntity.Instance?.RemoveSelf();
    }

    private static void TryStartRace(bool requireLoadFreezeEnded) {
        if (!armed || pendingGhost == null || started || !anchorPlaybackComplete) {
            return;
        }

        if (QuicksavePlayback.IsWatching) {
            return;
        }

        if (requireLoadFreezeEnded) {
            if (QuicksaveLoadFreeze.IsWaiting) {
                return;
            }
        } else if (QuicksaveLoadFreeze.IsWaiting || SpeedrunToolBridge.IsGameFrozen) {
            return;
        }

        if (Engine.Scene is not Level) {
            return;
        }

        activeGhost = pendingGhost;
        pendingGhost = null;
        StartRacePlayback();
    }

    private static void StartRacePlayback() {
        if (activeGhost == null || Engine.Scene is not Level level) {
            return;
        }

        started = true;
        armed = false;

        SpeedrunToolRaceTimer.ClearEndPoints();

        int frameCount = activeGhost.Rooms.Sum(room => room.Frames.Count);
        if (frameCount == 0) {
            Logger.Warn(GhostConstants.LogTag, "Ghost race started but ghost has no recorded frames.");
        }

        Ghost ghost = new(activeGhost.Rooms) {
            ForceSync = QuicksaveModModule.Settings.ResyncGhostOnRoomTransition,
            CompletedRun = activeGhost.Finish != null,
            TintColor = Microsoft.Xna.Framework.Color.White * 0.45f,
        };
        level.Add(ghost);
        level.Add(new GhostReplayerEntity(ghost));

        if (ShouldConfigureRaceTimer() && activeGhost.Finish != null) {
            SpeedrunToolRaceTimer.ScheduleBegin(activeGhost.Finish);
        }

        Logger.Info(
            GhostConstants.LogTag,
            $"Ghost race playback started ({activeGhost.Rooms.Count} room(s), {frameCount} frame(s))."
        );
    }

    private static bool ShouldConfigureRaceTimer() {
        return QuicksaveModModule.Settings.AddTimerToRace
            && SpeedrunToolBridge.IsLoaded
            && SpeedrunToolBridge.IsEnabled;
    }
}
