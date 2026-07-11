using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.Hooks;

public static class QuicksaveHooks {
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

        QuicksaveTracker.Instance.Reset(level.Session, level);
        Recording.GameplayInputRecorder.ResetMapper();
    }
}
