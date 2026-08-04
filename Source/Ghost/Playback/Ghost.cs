using Celeste.Mod.QuicksaveMod.Ghost;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal sealed class Ghost : Actor {
    public bool Done { get; private set; }
    public bool ForceSync { get; init; }
    public bool NotSynced { get; private set; }

    private readonly IReadOnlyList<GhostRoomSegment> rooms;
    private int roomIndex;
    private int frameIndex = -1;

    public Microsoft.Xna.Framework.Color TintColor = Microsoft.Xna.Framework.Color.White;

    public Ghost(IReadOnlyList<GhostRoomSegment> roomSegments)
        : base(Vector2.Zero) {
        Tag = Tags.Global;
        Active = false;
        Depth = 1;
        rooms = roomSegments;
        roomIndex = 0;

        PlayerSpriteMode spriteMode = PlayerSpriteMode.Madeline;
        Sprite = new PlayerSprite(spriteMode);
        Add(Hair = new PlayerHair(Sprite));
        Add(Sprite);
        Sprite.Active = false;
        origHairColor = Player.NormalHairColor;
        Hair.Color = origHairColor;
    }

    public PlayerSprite Sprite { get; }
    public PlayerHair Hair { get; }
    private readonly Microsoft.Xna.Framework.Color origHairColor;

    private GhostRoomSegment CurrentRoom => rooms[roomIndex];
    private GhostFrameData Frame => CurrentRoom.Frames[Math.Clamp(frameIndex, 0, CurrentRoom.Frames.Count - 1)];

    public string CurrentRoomName => CurrentRoom.Level;
    public int CurrentRevisit => CurrentRoom.Revisit;

    public void UpdateByReplayer() {
        if (Done || NotSynced || rooms.Count == 0) {
            return;
        }

        frameIndex++;
        if (frameIndex < 0) {
            return;
        }

        if (frameIndex >= CurrentRoom.Frames.Count) {
            GotoNextRoom();
            if (Done) {
                return;
            }
        }

        Visible &= Frame.HasPlayer;
        base.Update();
        UpdateSprite();
        UpdateHair();
    }

    private void GotoNextRoom() {
        roomIndex++;
        if (roomIndex < rooms.Count) {
            frameIndex = 0;
            NotSynced = ForceSync;
        } else {
            Done = true;
            if (frameIndex >= CurrentRoom.Frames.Count && CurrentRoom.Frames.Count > 0) {
                frameIndex = CurrentRoom.Frames.Count - 1;
                Visible = CurrentRoom.Frames[frameIndex].HasPlayer;
                UpdateSprite();
                UpdateHair();
            }
        }
    }

    public override void Update() {
        // Driven by GhostReplayerEntity
    }

    private void UpdateHair() {
        if (!Frame.HasPlayer) {
            return;
        }

        Hair.Facing = (Facings) Frame.Facing;
        Hair.SimulateMotion = Frame.HairSimulateMotion;
        Hair.Color = new Color(
            Frame.HairColor.R * TintColor.R / 255,
            Frame.HairColor.G * TintColor.G / 255,
            Frame.HairColor.B * TintColor.B / 255,
            Frame.HairColor.A * TintColor.A / 255
        );
    }

    private void UpdateSprite() {
        if (!Frame.HasPlayer) {
            return;
        }

        Position = Frame.Position;
        Sprite.Rotation = Frame.Rotation;
        Sprite.Scale = Frame.Scale;
        Sprite.Scale.X *= Frame.Facing;
        Sprite.Color = new Color(
            Frame.SpriteColor.R * TintColor.R / 255,
            Frame.SpriteColor.G * TintColor.G / 255,
            Frame.SpriteColor.B * TintColor.B / 255,
            Frame.SpriteColor.A * TintColor.A / 255
        );
        Sprite.HairCount = Frame.HairCount;

        try {
            if (Sprite.CurrentAnimationID != Frame.CurrentAnimationId) {
                Sprite.Play(Frame.CurrentAnimationId);
            }

            Sprite.SetAnimationFrame(Frame.CurrentAnimationFrame);
        } catch {
            // Missing animation IDs are ignored.
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (CurrentRoom.Frames.Count > 0) {
            Hair.Facing = (Facings) CurrentRoom.Frames[0].Facing;
        }

        Hair.Start();
        UpdateHair();
    }
}
