namespace Celeste.Mod.QuicksaveMod.Ghost;

internal sealed class GhostRoomSegment {
    public string Level { get; set; } = "";
    public int Revisit { get; set; } = 1;
    public string? TargetLevel { get; set; }
    public List<GhostFrameData> Frames { get; set; } = [];
}
