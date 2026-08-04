using Celeste.Mod.QuicksaveMod.Interop;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostPlaybackHooks {
    public static void Apply() {
        On.Celeste.Level.Update += OnLevelUpdate;
        Everest.Events.Level.OnExit += OnLevelExit;
    }

    public static void Unapply() {
        On.Celeste.Level.Update -= OnLevelUpdate;
        Everest.Events.Level.OnExit -= OnLevelExit;
        GhostRaceController.Reset();
        GhostSpectateController.Reset();
    }

    private static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level level) {
        orig(level);
        if (GhostRaceController.IsActive) {
            SpeedrunToolBridge.UpdateRaceTimerEndpoint(level);
        }
    }

    internal static void OnLoadFreezeEnded() {
        GhostRaceController.OnLoadFreezeEnded();
    }

    private static void OnLevelExit(
        Level level,
        LevelExit exit,
        LevelExit.Mode mode,
        Session session,
        HiresSnow snow
    ) {
        if (mode is not (LevelExit.Mode.Completed or LevelExit.Mode.CompletedInterlude)) {
            return;
        }

        if (GhostReplayerEntity.Instance is not { Ghost.ForceSync: true } replayer) {
            return;
        }

        replayer.Ghost.Sync("LevelExit", 1);
    }
}
