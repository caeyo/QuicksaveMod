using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal sealed class GhostFrameData {
    public bool HasPlayer { get; init; }
    public Vector2 Position { get; init; }
    public int Facing { get; init; } = 1;
    public string? CurrentAnimationId { get; init; }
    public int CurrentAnimationFrame { get; init; }
    public float Rotation { get; init; }
    public Vector2 Scale { get; init; } = Vector2.One;
    public Color SpriteColor { get; init; } = Color.White;
    public Color HairColor { get; init; } = Player.NormalHairColor;
    public bool HairSimulateMotion { get; init; } = true;
    public int HairCount { get; init; }
}
