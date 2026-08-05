namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostPlaybackHooks {
    public static void Apply() {
        Everest.Events.Level.OnExit += OnLevelExit;
    }

    public static void Unapply() {
        Everest.Events.Level.OnExit -= OnLevelExit;
        GhostRaceController.Reset();
        GhostSpectateController.Reset();
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
