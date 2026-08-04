using Celeste.Mod.QuicksaveMod.Recording;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Recording;

[Tracked(false)]
internal sealed class GhostFrameRecorder : Entity {
    public GhostFrameRecorder() {
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;
        Depth = -10_000_000;
    }

    public override void Update() {
        base.Update();

        if (Engine.Scene is not Level level || level.Session is not Session session) {
            return;
        }

        if (!GhostRecordingSession.IsAnchored) {
            RemoveSelf();
            return;
        }

        if (!GhostRecordingSession.IsRecordingInputs || !GameplayInputRecorder.ShouldRecordFrame(level)) {
            return;
        }

        Player? player = level.Tracker.GetEntity<Player>();
        if (player is not { Dead: false }) {
            GhostRecordingSession.AppendFrame(new GhostFrameData {
                HasPlayer = false,
                SessionTimeTicks = session.Time,
            });
            return;
        }

        GhostRecordingSession.AppendFrame(new GhostFrameData {
            HasPlayer = true,
            SessionTimeTicks = session.Time,
            Position = player.Position,
            Facing = (int) player.Facing,
            CurrentAnimationId = player.Sprite.CurrentAnimationID,
            CurrentAnimationFrame = player.Sprite.CurrentAnimationFrame,
            Rotation = player.Sprite.Rotation,
            Scale = player.Sprite.Scale,
            SpriteColor = player.Sprite.Color,
            HairColor = player.Hair.Color,
            HairSimulateMotion = player.Hair.SimulateMotion,
            HairCount = player.Sprite.HairCount,
            HitboxWidth = player.Collider.Width,
            HitboxHeight = player.Collider.Height,
            HitboxLeft = player.Collider.Position.X,
            HitboxTop = player.Collider.Position.Y,
        });
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
    }
}
