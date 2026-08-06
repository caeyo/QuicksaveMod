using System.Text.Json.Serialization;

namespace Celeste.Mod.QuickTools.Ghost.Serialization;

internal sealed class GhostFinishDto {
    [JsonPropertyName("r")]
    public string Room { get; set; } = "";

    [JsonPropertyName("rv")]
    public int Revisit { get; set; } = 1;

    [JsonPropertyName("p")]
    public float[] Position { get; set; } = [];

    [JsonPropertyName("t")]
    public long SessionTimeTicks { get; set; }
}
