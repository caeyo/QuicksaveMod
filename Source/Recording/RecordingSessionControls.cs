using Celeste.Mod.QuicksaveMod.Ghost;

namespace Celeste.Mod.QuicksaveMod.Recording;

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
