using System.Text.Json.Serialization;

namespace Celeste.Mod.QuicksaveMod.Ghost.Serialization;

internal sealed class GhostRoomDto {
    [JsonPropertyName("l")]
    public string Level { get; set; } = "";

    [JsonPropertyName("rv")]
    public int Revisit { get; set; } = 1;

    [JsonPropertyName("frames")]
    public List<GhostFrameEntry> Frames { get; set; } = [];
}
