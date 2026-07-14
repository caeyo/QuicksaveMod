using Monocle;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

public static class QuicksavePlayback {
    private static bool _watching;
    private static bool _playbackStarted;

    public static void Apply() {
        On.Monocle.Engine.Update += WatchForEnd;
    }

    public static void Unapply() {
        On.Monocle.Engine.Update -= WatchForEnd;
        _watching = false;
        _playbackStarted = false;
    }

    public static void Start(string tasFilePath) {
        string fullPath = Path.GetFullPath(tasFilePath);
        _watching = true;
        _playbackStarted = false;

        // A prior post-playback freeze must not block Level.Update / player intro on the new load.
        QuicksaveLoadFreeze.Cancel();

        Manager.AddMainThreadAction(() => {
            if (Manager.Running) {
                Manager.DisableRun();
            }

            // RefreshInputs clears parsed inputs when NextState is Disabled.
            Manager.NextState = Manager.State.Running;
            Manager.Controller.FilePath = fullPath;
        });
    }

    private static void WatchForEnd(On.Monocle.Engine.orig_Update orig, Engine engine, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(engine, gameTime);

        if (!_watching) {
            return;
        }

        if (Manager.Running) {
            _playbackStarted = true;

            // *** breakpoint: CelesteTAS pauses with Break set.
            if (Manager.CurrState == Manager.State.Paused && Manager.Controller.Break) {
                FinishPlaybackAndFreeze();
            }

            return;
        }

        // EnableRun happens on a later Manager.Update after we set NextState.
        if (!_playbackStarted) {
            return;
        }

        // Unexpected stop (EOF without pause, abort, etc.).
        FinishPlaybackAndFreeze();
    }

    private static void FinishPlaybackAndFreeze() {
        _watching = false;
        _playbackStarted = false;

        if (Manager.Running) {
            Manager.DisableRun();
        }

        QuicksaveLoadFreeze.Begin();
        Logger.Info(nameof(QuicksavePlayback), "Quicksave playback finished; CelesteTAS stopped.");
    }
}
