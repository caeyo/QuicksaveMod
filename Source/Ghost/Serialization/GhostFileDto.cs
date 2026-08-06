using System.Text.Json.Serialization;
using Celeste.Mod.QuickTools.Ghost;
using Celeste.Mod.QuickTools.Quicksave;

namespace Celeste.Mod.QuickTools.Ghost.Serialization;

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
