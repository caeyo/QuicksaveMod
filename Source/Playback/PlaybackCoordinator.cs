using Celeste.Mod.QuicksaveMod.Ghost.Playback;

namespace Celeste.Mod.QuicksaveMod.Playback;

internal enum ActivePlayback {
    None,
    QuicksaveAnchor,
    GhostSpectate,
}

internal static class PlaybackCoordinator {
    private static ActivePlayback active = ActivePlayback.None;

    public static void Begin(ActivePlayback playback) {
        switch (playback) {
            case ActivePlayback.QuicksaveAnchor:
                GhostSpectateController.Reset();
                QuicksavePlayback.Reset();
                break;

            case ActivePlayback.GhostSpectate:
                QuicksavePlayback.Reset();
                GhostRaceController.Reset();
                GhostSpectateController.Reset();
                break;
        }

        active = playback;
    }

    public static void Clear(ActivePlayback playback) {
        if (active == playback) {
            active = ActivePlayback.None;
        }
    }
}
