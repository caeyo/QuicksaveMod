namespace Celeste.Mod.QuickTools.Ghost;

internal sealed class GhostRoomSegment {
    public string Level { get; set; } = "";
    public int Revisit { get; set; } = 1;
    public List<GhostFrameData> Frames { get; set; } = [];
}
