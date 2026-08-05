// Adapted from GhostModForTas (MIT) — https://github.com/LozenChen/GhostModForTas
using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Interop;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

[Tracked(false)]
internal sealed class GhostReplayerEntity : Entity {
    public static GhostReplayerEntity? Instance { get; private set; }

    public Ghost Ghost { get; }
    public bool Done => Ghost.Done;

    private readonly Dictionary<string, int> playerRevisitCounts = new(StringComparer.OrdinalIgnoreCase);
    private string playerRoom = "";

    public GhostReplayerEntity(Ghost ghost) : base(Vector2.Zero) {
        Ghost = ghost;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;
        Depth = 1;
        Instance = this;

        if (Engine.Scene is Level level) {
            playerRoom = level.Session.Level;
            playerRevisitCounts[playerRoom] = 1;
        }
    }

    public override void Update() {
        if (Engine.Scene is not Level level) {
            return;
        }

        EnsureGhostInScene(level);
        SpeedrunToolRaceTimer.TryBeginScheduled(level);

        if (Ghost.Done) {
            if (Ghost.Scene != null) {
                Ghost.RemoveSelf();
            }

            if (SpeedrunToolRaceTimer.NeedsReplayerSupport) {
                TrackPlayerRoom(level);
                if (SpeedrunToolRaceTimer.IsWatchingFlagTouch) {
                    SpeedrunToolRaceTimer.WatchFlagTouch();
                }

                base.Update();
                return;
            }

            RemoveSelf();
            return;
        }

        TrackPlayerRoom(level);
        if (SpeedrunToolRaceTimer.IsWatchingFlagTouch) {
            SpeedrunToolRaceTimer.WatchFlagTouch();
        }

        Ghost.Visible = true;
        Ghost.UpdateByReplayer();

        if (Ghost.ForceSync && (!Ghost.HasRooms || !GhostMatchesPlayerRoom())) {
            Ghost.Visible = false;
        }

        base.Update();
    }

    private void EnsureGhostInScene(Level level) {
        if (Ghost.Done || Ghost.Scene != null) {
            return;
        }

        level.Add(Ghost);
    }

    private void TrackPlayerRoom(Level level) {
        string room = level.Session.Level;
        if (room == playerRoom) {
            return;
        }

        playerRoom = room;
        EnsureGhostInScene(level);
        playerRevisitCounts.TryGetValue(room, out int count);
        int revisit = count + 1;
        playerRevisitCounts[room] = revisit;

        SpeedrunToolRaceTimer.OnRoomChanged(level);

        if (Ghost.ForceSync) {
            Ghost.Sync(room, revisit);
        }
    }

    private bool GhostMatchesPlayerRoom() {
        if (!Ghost.HasRooms) {
            return false;
        }

        if (!string.Equals(Ghost.CurrentRoomName, playerRoom, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (!playerRevisitCounts.TryGetValue(playerRoom, out int playerRevisit)) {
            playerRevisit = 1;
        }

        return Ghost.CurrentRevisit == playerRevisit;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        if (Ghost.Scene != null) {
            Ghost.RemoveSelf();
        }

        if (Instance == this) {
            Instance = null;
        }
    }
}
