using System.Text.Json.Serialization;

namespace Celeste.Mod.QuickTools.Ghost.Serialization;

internal sealed class GhostFrameEntry {
    [JsonPropertyName("k")]
    public bool? Keyframe { get; set; }

    [JsonPropertyName("hp")]
    public bool? HasPlayer { get; set; }

    [JsonPropertyName("p")]
    public float[]? Position { get; set; }

    [JsonPropertyName("f")]
    public int? Facing { get; set; }

    [JsonPropertyName("a")]
    public string? AnimationId { get; set; }

    [JsonPropertyName("af")]
    public int? AnimationFrame { get; set; }

    [JsonPropertyName("rot")]
    public float? Rotation { get; set; }

    [JsonPropertyName("s")]
    public float[]? Scale { get; set; }

    [JsonPropertyName("sc")]
    public byte[]? SpriteColor { get; set; }

    [JsonPropertyName("hc")]
    public byte[]? HairColor { get; set; }

    [JsonPropertyName("hsm")]
    public bool? HairSimulateMotion { get; set; }

    [JsonPropertyName("hn")]
    public int? HairCount { get; set; }
}
