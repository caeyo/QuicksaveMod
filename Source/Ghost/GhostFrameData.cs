using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuickTools.Ghost;

internal readonly struct GhostFrameData {
    public bool HasPlayer { get; init; }
    public Vector2 Position { get; init; }
    public int Facing { get; init; }
    public string? CurrentAnimationId { get; init; }
    public int CurrentAnimationFrame { get; init; }
    public float Rotation { get; init; }
    public Vector2 Scale { get; init; }
    public Color SpriteColor { get; init; }
    public Color HairColor { get; init; }
    public bool HairSimulateMotion { get; init; }
    public int HairCount { get; init; }

    public static GhostFrameData WithoutPlayer => new() { HasPlayer = false };

    public static GhostFrameData FromPlayer(Player player) => new() {
        HasPlayer = true,
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
    };
}
