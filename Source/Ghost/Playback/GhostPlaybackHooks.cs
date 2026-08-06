using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostPlaybackHooks {
    public static void Apply() {
        Everest.Events.Level.OnExit += OnLevelExit;
        On.Monocle.Engine.RenderCore += OnRenderCore;
    }

    public static void Unapply() {
        Everest.Events.Level.OnExit -= OnLevelExit;
        On.Monocle.Engine.RenderCore -= OnRenderCore;
        GhostRaceController.Reset();
        GhostSpectateController.Reset();
    }

    private static void OnRenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        orig(self);
        SpectateHintHud.OnPostRender();
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
