// Adapted from GhostModForTas (MIT) — https://github.com/LozenChen/GhostModForTas
using Celeste.Mod.QuicksaveMod.Ghost;
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
    }

    public override void Update() {
        if (Engine.Scene is not Level level) {
            return;
        }

        TrackPlayerRoom(level);
        Ghost.UpdateByReplayer();
        Ghost.Visible = GhostMatchesPlayerRoom() && Ghost.Visible;
        base.Update();
    }

    private void TrackPlayerRoom(Level level) {
        string room = level.Session.Level;
        if (room == playerRoom) {
            return;
        }

        playerRoom = room;
        playerRevisitCounts.TryGetValue(room, out int count);
        playerRevisitCounts[room] = count + 1;
    }

    private bool GhostMatchesPlayerRoom() {
        if (!playerRevisitCounts.TryGetValue(Ghost.CurrentRoomName, out int revisit)) {
            revisit = 1;
        }

        return Ghost.CurrentRoomName == playerRoom && Ghost.CurrentRevisit == revisit;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Ghost.RemoveSelf();
        if (Instance == this) {
            Instance = null;
        }
    }
}
