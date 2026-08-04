using Celeste.Mod.QuicksaveMod.Ghost.Playback;
using Celeste.Mod.QuicksaveMod.Playback;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Hooks;

internal static class PlaybackHooks {
    public static void Apply() {
        On.Monocle.Engine.Update += OnEngineUpdate;
    }

    public static void Unapply() {
        On.Monocle.Engine.Update -= OnEngineUpdate;
        QuicksavePlayback.Reset();
        GhostSpectateController.Reset();
    }

    private static void OnEngineUpdate(
        On.Monocle.Engine.orig_Update orig,
        Engine engine,
        Microsoft.Xna.Framework.GameTime gameTime
    ) {
        orig(engine, gameTime);
        QuicksavePlayback.OnEngineUpdate();
        GhostSpectateController.OnEngineUpdate();
    }
}
