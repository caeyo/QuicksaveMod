using Celeste.Mod.QuicksaveMod.Ghost.Serialization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostFrameCodec {
    private const int KeyframeInterval = 300;

    public static List<GhostFrameEntry> Encode(IReadOnlyList<GhostFrameData> frames) {
        List<GhostFrameEntry> entries = new(frames.Count);
        GhostFrameState state = new();
        bool hasState = false;

        for (int i = 0; i < frames.Count; i++) {
            GhostFrameData frame = frames[i];
            bool forceKeyframe = !hasState
                || i % KeyframeInterval == 0
                || frame.HasPlayer != state.HasPlayer
                || RequiresKeyframe(frame, state);

            if (forceKeyframe) {
                entries.Add(ToKeyframe(frame));
                state.CopyFrom(frame);
                hasState = true;
                continue;
            }

            entries.Add(ToDelta(frame, state));
            state.CopyFrom(frame);
        }

        return entries;
    }

    public static List<GhostFrameData> Decode(IReadOnlyList<GhostFrameEntry> entries) {
        List<GhostFrameData> frames = new(entries.Count);
        GhostFrameState state = new();

        foreach (GhostFrameEntry entry in entries) {
            state.Apply(entry);
            frames.Add(state.ToFrame());
        }

        return frames;
    }

    private static bool RequiresKeyframe(GhostFrameData frame, GhostFrameState state) {
        if (!frame.HasPlayer) {
            return false;
        }

        return !string.Equals(frame.CurrentAnimationId, state.CurrentAnimationId, StringComparison.Ordinal)
            || !ColorsEqual(frame.SpriteColor, state.SpriteColor)
            || !ColorsEqual(frame.HairColor, state.HairColor)
            || frame.Rotation != state.Rotation
            || frame.Scale != state.Scale
            || frame.HairSimulateMotion != state.HairSimulateMotion
            || frame.HairCount != state.HairCount;
    }

    private static GhostFrameEntry ToKeyframe(GhostFrameData frame) {
        if (!frame.HasPlayer) {
            return new GhostFrameEntry {
                Keyframe = true,
                HasPlayer = false,
            };
        }

        GhostFrameEntry entry = new() {
            Keyframe = true,
            HasPlayer = true,
            Position = PackVector(frame.Position),
            Facing = frame.Facing,
            AnimationId = frame.CurrentAnimationId,
            AnimationFrame = frame.CurrentAnimationFrame,
        };

        if (frame.Rotation != 0f) {
            entry.Rotation = frame.Rotation;
        }

        if (frame.Scale != Vector2.One) {
            entry.Scale = PackVector(frame.Scale);
        }

        if (!ColorsEqual(frame.SpriteColor, Color.White)) {
            entry.SpriteColor = PackColor(frame.SpriteColor);
        }

        entry.HairColor = PackColor(frame.HairColor);

        if (!frame.HairSimulateMotion) {
            entry.HairSimulateMotion = false;
        }

        entry.HairCount = frame.HairCount;

        return entry;
    }

    private static GhostFrameEntry ToDelta(GhostFrameData frame, GhostFrameState state) {
        if (!frame.HasPlayer) {
            return new GhostFrameEntry { HasPlayer = false };
        }

        GhostFrameEntry entry = new() { HasPlayer = true };

        if (frame.Position != state.Position) {
            entry.Position = PackVector(frame.Position);
        }

        if (frame.Facing != state.Facing) {
            entry.Facing = frame.Facing;
        }

        if (!string.Equals(frame.CurrentAnimationId, state.CurrentAnimationId, StringComparison.Ordinal)) {
            entry.AnimationId = frame.CurrentAnimationId;
        }

        if (frame.CurrentAnimationFrame != state.CurrentAnimationFrame) {
            entry.AnimationFrame = frame.CurrentAnimationFrame;
        }

        if (frame.Rotation != state.Rotation) {
            entry.Rotation = frame.Rotation;
        }

        if (frame.Scale != state.Scale) {
            entry.Scale = PackVector(frame.Scale);
        }

        if (!ColorsEqual(frame.SpriteColor, state.SpriteColor)) {
            entry.SpriteColor = PackColor(frame.SpriteColor);
        }

        if (!ColorsEqual(frame.HairColor, state.HairColor)) {
            entry.HairColor = PackColor(frame.HairColor);
        }

        if (frame.HairSimulateMotion != state.HairSimulateMotion) {
            entry.HairSimulateMotion = frame.HairSimulateMotion;
        }

        if (frame.HairCount != state.HairCount) {
            entry.HairCount = frame.HairCount;
        }

        return entry;
    }

    private static float[] PackVector(Vector2 value) => [value.X, value.Y];

    private static byte[] PackColor(Color color) => [color.R, color.G, color.B, color.A];

    private static bool ColorsEqual(Color left, Color right) {
        return left.R == right.R
            && left.G == right.G
            && left.B == right.B
            && left.A == right.A;
    }

    private sealed class GhostFrameState {
        public bool HasPlayer { get; private set; }
        public Vector2 Position { get; private set; }
        public int Facing { get; private set; } = 1;
        public string? CurrentAnimationId { get; private set; }
        public int CurrentAnimationFrame { get; private set; }
        public float Rotation { get; private set; }
        public Vector2 Scale { get; private set; } = Vector2.One;
        public Color SpriteColor { get; private set; } = Color.White;
        public Color HairColor { get; private set; } = Player.NormalHairColor;
        public bool HairSimulateMotion { get; private set; } = true;
        public int HairCount { get; private set; }

        public void CopyFrom(GhostFrameData frame) {
            HasPlayer = frame.HasPlayer;
            Position = frame.Position;
            Facing = frame.Facing;
            CurrentAnimationId = frame.CurrentAnimationId;
            CurrentAnimationFrame = frame.CurrentAnimationFrame;
            Rotation = frame.Rotation;
            Scale = frame.Scale;
            SpriteColor = frame.SpriteColor;
            HairColor = frame.HairColor;
            HairSimulateMotion = frame.HairSimulateMotion;
            HairCount = frame.HairCount;
        }

        public void Apply(GhostFrameEntry entry) {
            if (entry.HasPlayer is { } hasPlayer) {
                if (!hasPlayer) {
                    ResetVisualDefaults();
                }

                HasPlayer = hasPlayer;
            }

            if (!HasPlayer) {
                return;
            }

            if (entry.Position is { Length: 2 } position) {
                Position = new Vector2(position[0], position[1]);
            }

            if (entry.Facing is { } facing) {
                Facing = facing;
            }

            if (entry.AnimationId != null) {
                CurrentAnimationId = entry.AnimationId;
            }

            if (entry.AnimationFrame is { } animationFrame) {
                CurrentAnimationFrame = animationFrame;
            }

            if (entry.Rotation is { } rotation) {
                Rotation = rotation;
            }

            if (entry.Scale is { Length: 2 } scale) {
                Scale = new Vector2(scale[0], scale[1]);
            }

            if (entry.SpriteColor is { Length: 4 } spriteColor) {
                SpriteColor = UnpackColor(spriteColor);
            }

            if (entry.HairColor is { Length: 4 } hairColor) {
                HairColor = UnpackColor(hairColor);
            }

            if (entry.HairSimulateMotion is { } hairSimulateMotion) {
                HairSimulateMotion = hairSimulateMotion;
            }

            if (entry.HairCount is { } hairCount) {
                HairCount = hairCount;
            }
        }

        private void ResetVisualDefaults() {
            SpriteColor = Color.White;
            HairColor = Player.NormalHairColor;
            HairSimulateMotion = true;
            HairCount = 0;
        }

        public GhostFrameData ToFrame() =>
            HasPlayer
                ? new GhostFrameData {
                    HasPlayer = true,
                    Position = Position,
                    Facing = Facing,
                    CurrentAnimationId = CurrentAnimationId,
                    CurrentAnimationFrame = CurrentAnimationFrame,
                    Rotation = Rotation,
                    Scale = Scale,
                    SpriteColor = SpriteColor,
                    HairColor = HairColor,
                    HairSimulateMotion = HairSimulateMotion,
                    HairCount = HairCount,
                }
                : GhostFrameData.WithoutPlayer;

        private static Color UnpackColor(byte[] rgba) => new(rgba[0], rgba[1], rgba[2], rgba[3]);
    }
}
