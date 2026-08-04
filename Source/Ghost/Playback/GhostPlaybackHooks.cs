using Celeste.Mod.QuicksaveMod.Interop;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class GhostPlaybackHooks {
    public static void Apply() {
        On.Celeste.Level.Update += OnLevelUpdate;
    }

    public static void Unapply() {
        On.Celeste.Level.Update -= OnLevelUpdate;
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
}
