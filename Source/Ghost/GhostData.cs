using Celeste.Mod.QuickTools.Quicksave;

namespace Celeste.Mod.QuickTools.Ghost;

internal sealed class GhostData {
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public QuicksaveData Anchor { get; init; } = new();
    public List<string> Inputs { get; init; } = [];
    public GhostFinishData? Finish { get; init; }
    public List<GhostRoomSegment> Rooms { get; init; } = [];

    public GhostData Clone() {
        var rooms = new List<GhostRoomSegment>(Rooms.Count);
        foreach (GhostRoomSegment room in Rooms) {
            rooms.Add(new GhostRoomSegment {
                Level = room.Level,
                Revisit = room.Revisit,
                Frames = new List<GhostFrameData>(room.Frames),
            });
        }

        return new GhostData {
            Version = Version,
            CreatedUtc = CreatedUtc,
            Anchor = Anchor.Clone(),
            Inputs = [..Inputs],
            Finish = Finish == null
                ? null
                : new GhostFinishData {
                    Room = Finish.Room,
                    Revisit = Finish.Revisit,
                    Position = Finish.Position,
                    SessionTimeTicks = Finish.SessionTimeTicks,
                },
            Rooms = rooms,
        };
    }
}
