using Celeste.Mod.QuickTools.Ghost;

namespace Celeste.Mod.QuickTools.Recording;

internal static class RecordingSessionControls {
    public static void SuspendAll() {
        GameplayInputRecorder.Suspend();
        GhostRecordingSession.Suspend();
    }

    public static void ResumeAll() {
        GameplayInputRecorder.Resume();
        GhostRecordingSession.Resume();
    }
}
