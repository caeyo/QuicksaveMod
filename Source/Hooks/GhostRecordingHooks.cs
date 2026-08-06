using Celeste.Mod.QuickTools.Ghost;

namespace Celeste.Mod.QuickTools.Hooks;

internal static class GhostRecordingHooks {
    public static void Apply() {
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        Everest.Events.Level.OnExit += OnLevelExit;
    }

    public static void Unapply() {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        Everest.Events.Level.OnExit -= OnLevelExit;
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (playerIntro == Player.IntroTypes.Transition) {
            GhostRecordingSession.OnRoomTransition(level);
        }
    }

    private static void OnLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode is LevelExit.Mode.Completed or LevelExit.Mode.CompletedInterlude) {
            GhostRecordingSession.OnLevelExit(level);
        }
    }
}
