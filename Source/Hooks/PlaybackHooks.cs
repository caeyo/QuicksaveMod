using Celeste.Mod.QuickTools.Ghost.Playback;
using Celeste.Mod.QuickTools.Playback;
using Monocle;

namespace Celeste.Mod.QuickTools.Hooks;

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
