using Celeste.Mod.QuickTools.Quicksave;
using Celeste.Mod.QuickTools.Recording;

namespace Celeste.Mod.QuickTools.Hooks;

internal static class LevelLoadHooks {
    public static void Apply() {
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    }

    public static void Unapply() {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (playerIntro == Player.IntroTypes.Transition) {
            return;
        }

        QuicksaveTracker.Reset(level.Session, level);
        GameplayInputRecorder.ResetMapper();
    }
}
