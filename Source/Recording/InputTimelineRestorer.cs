using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Ghost.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class InputTimelineRestorer {
    internal enum GhostRestoreMode {
        None,
        AlwaysAnchor,
        MatchRecordingStartOrRaceAnchor,
    }

    public static void Restore(QuicksaveData timeline, GhostRestoreMode ghostMode) {
        QuicksaveTracker.SeedFrom(timeline);
        GameplayInputRecorder.ResetMapper();

        switch (ghostMode) {
            case GhostRestoreMode.AlwaysAnchor:
                GhostRecordingSession.AnchorFrom(timeline);
                break;

            case GhostRestoreMode.MatchRecordingStartOrRaceAnchor:
                if (GhostRecordingSession.TimelineMatchesRecordingStart(timeline)) {
                    GhostRecordingSession.AnchorFrom(timeline);
                } else if (GhostRaceController.RaceAnchor is { } raceAnchor
                    && GhostRecordingSession.AnchorEquality.Equals(timeline, raceAnchor)) {
                    GhostRaceController.OnSrtLoadBackToAnchor();
                }

                break;
        }
    }
}
