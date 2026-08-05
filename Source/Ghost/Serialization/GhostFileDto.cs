using System.Text.Json.Serialization;
using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.Ghost.Serialization;

internal sealed class GhostFileDto {
    [JsonPropertyName("v")]
    public int Version { get; set; } = GhostData.CurrentVersion;

    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    public QuicksaveData Anchor { get; set; } = new();

    public List<string> Inputs { get; set; } = [];

    public GhostFinishDto? Finish { get; set; }

    public List<GhostRoomDto> Rooms { get; set; } = [];
}
