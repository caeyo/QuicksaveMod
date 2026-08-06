using Celeste.Mod.QuicksaveMod.Ghost;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal sealed class Ghost : Actor {
    public bool Done { get; private set; }
    public bool ForceSync { get; init; }
    public bool CompletedRun { get; init; }
    public bool NotSynced { get; private set; }

    private readonly IReadOnlyList<GhostRoomSegment> rooms;
    private int roomIndex;
    private int frameIndex = -1;
    private GhostFrameData currentFrame;
    private bool hasCurrentFrame;

    private Color tintedSpriteColor;
    private Color tintedHairColor;
    private Color lastSpriteSource;
    private Color lastHairSource;
    private Color lastAppliedTint = Color.White;

    public Microsoft.Xna.Framework.Color TintColor = Microsoft.Xna.Framework.Color.White;

    public Ghost(IReadOnlyList<GhostRoomSegment> roomSegments)
        : base(Vector2.Zero) {
        // Room-local entity; GhostReplayerEntity re-adds us after transitions so render order matches the player.
        Tag = Tags.TransitionUpdate;
        Active = false;
        Visible = true;
        rooms = roomSegments;
        roomIndex = 0;
        SkipEmptyRoomsForward();

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

    public bool HasRooms => roomIndex < rooms.Count;

    private GhostRoomSegment CurrentRoom => rooms[roomIndex];

    public string CurrentRoomName => HasRooms ? CurrentRoom.Level : "";

    public int CurrentRevisit => HasRooms ? CurrentRoom.Revisit : 1;

    public void UpdateByReplayer() {
        if (Done || NotSynced || !HasRooms) {
            return;
        }

        frameIndex++;
        if (frameIndex < 0) {
            return;
        }

        if (frameIndex >= CurrentRoom.Frames.Count) {
            GotoNextRoom();
            if (Done || !HasRooms) {
                return;
            }
        }

        if (CurrentRoom.Frames.Count == 0) {
            return;
        }

        RefreshCurrentFrame();
        Visible = currentFrame.HasPlayer;
        base.Update();
        UpdateSprite();
        UpdateHair();
        Hair.AfterUpdate();
    }

    private void GotoNextRoom() {
        roomIndex++;
        SkipEmptyRoomsForward();

        if (!HasRooms) {
            FinishPlayback();
            return;
        }

        frameIndex = 0;
        NotSynced = ForceSync;
        hasCurrentFrame = false;
    }

    internal void Sync(string roomName, int revisit) {
        NotSynced = true;

        if (IsLevelExit(roomName)) {
            SyncLevelExit();
            return;
        }

        if (Done) {
            return;
        }

        int orig = roomIndex;
        for (int i = orig; i < rooms.Count; i++) {
            if (MatchesRoom(rooms[i], roomName, revisit)) {
                JumpToRoom(i);
                return;
            }
        }

        for (int i = 0; i < orig; i++) {
            if (MatchesRoom(rooms[i], roomName, revisit)) {
                JumpToRoom(i);
                return;
            }
        }
    }

    private void SyncLevelExit() {
        if (!CompletedRun || Done) {
            return;
        }

        FinishPlayback();
        NotSynced = false;
    }

    private void JumpToRoom(int index) {
        roomIndex = index;
        frameIndex = -1;
        NotSynced = false;
        hasCurrentFrame = false;
        SkipEmptyRoomsForward();
    }

    private static bool IsLevelExit(string roomName) =>
        string.Equals(roomName, "LevelExit", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesRoom(GhostRoomSegment segment, string roomName, int revisit) =>
        string.Equals(segment.Level, roomName, StringComparison.OrdinalIgnoreCase)
        && segment.Revisit == revisit;

    private void FinishPlayback() {
        Done = true;

        if (rooms.Count == 0) {
            Visible = false;
            return;
        }

        GhostRoomSegment lastRoom = rooms[^1];
        if (lastRoom.Frames.Count == 0) {
            Visible = false;
            return;
        }

        roomIndex = rooms.Count - 1;
        frameIndex = lastRoom.Frames.Count - 1;
        RefreshCurrentFrame();
        Visible = currentFrame.HasPlayer;
        UpdateSprite();
        UpdateHair();
    }

    private void SkipEmptyRoomsForward() {
        while (roomIndex < rooms.Count && rooms[roomIndex].Frames.Count == 0) {
            roomIndex++;
        }

        if (roomIndex >= rooms.Count) {
            Done = true;
        }
    }

    public override void Update() {
        // Driven by GhostReplayerEntity
    }

    private void RefreshCurrentFrame() {
        IReadOnlyList<GhostFrameData> frames = CurrentRoom.Frames;
        if (frames.Count == 0) {
            hasCurrentFrame = false;
            return;
        }

        int index = Math.Clamp(frameIndex, 0, frames.Count - 1);
        currentFrame = frames[index];
        hasCurrentFrame = true;
    }

    private void UpdateHair() {
        if (!hasCurrentFrame || !currentFrame.HasPlayer) {
            return;
        }

        Hair.Facing = (Facings) currentFrame.Facing;
        Hair.SimulateMotion = currentFrame.HairSimulateMotion;
        ApplyTintedHairColor(currentFrame.HairColor);
    }

    private void UpdateSprite() {
        if (!hasCurrentFrame || !currentFrame.HasPlayer) {
            return;
        }

        Position = currentFrame.Position;
        // Y-based depth keeps the ghost in the gameplay layer; +1 keeps it behind Madeline (depth 0).
        Depth = Math.Max((int) Position.Y, 1);
        Sprite.Rotation = currentFrame.Rotation;
        Sprite.Scale = currentFrame.Scale;
        Sprite.Scale.X *= currentFrame.Facing;
        ApplyTintedSpriteColor(currentFrame.SpriteColor);
        Sprite.HairCount = currentFrame.HairCount;

        try {
            if (Sprite.CurrentAnimationID != currentFrame.CurrentAnimationId) {
                Sprite.Play(currentFrame.CurrentAnimationId);
            }

            Sprite.SetAnimationFrame(currentFrame.CurrentAnimationFrame);
        } catch {
            // Missing animation IDs are ignored.
        }
    }

    private void ApplyTintedSpriteColor(Color source) {
        if (source == lastSpriteSource && TintColor == lastAppliedTint) {
            Sprite.Color = tintedSpriteColor;
            return;
        }

        tintedSpriteColor.R = (byte) (source.R * TintColor.R / 255);
        tintedSpriteColor.G = (byte) (source.G * TintColor.G / 255);
        tintedSpriteColor.B = (byte) (source.B * TintColor.B / 255);
        tintedSpriteColor.A = (byte) (source.A * TintColor.A / 255);
        Sprite.Color = tintedSpriteColor;
        lastSpriteSource = source;
        lastAppliedTint = TintColor;
    }

    private void ApplyTintedHairColor(Color source) {
        if (source == lastHairSource && TintColor == lastAppliedTint) {
            Hair.Color = tintedHairColor;
            return;
        }

        tintedHairColor.R = (byte) (source.R * TintColor.R / 255);
        tintedHairColor.G = (byte) (source.G * TintColor.G / 255);
        tintedHairColor.B = (byte) (source.B * TintColor.B / 255);
        tintedHairColor.A = (byte) (source.A * TintColor.A / 255);
        Hair.Color = tintedHairColor;
        lastHairSource = source;
        lastAppliedTint = TintColor;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        Visible = true;
        if (HasRooms && CurrentRoom.Frames.Count > 0) {
            Hair.Facing = (Facings) CurrentRoom.Frames[0].Facing;
        }

        Hair.Start();
        RefreshCurrentFrame();
        UpdateHair();
    }

    public override void Render() {
        if (!Visible) {
            return;
        }

        // GhostModForTas keeps Sprite/Hair inactive and draws them here instead of via Actor.Render().
        foreach (Component component in Components) {
            if (component.Visible) {
                component.Render();
            }
        }
    }
}
