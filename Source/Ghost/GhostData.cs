using Celeste.Mod.QuicksaveMod.Quicksave;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal sealed class GhostData {
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public QuicksaveData Anchor { get; init; } = new();
    public List<string> Inputs { get; init; } = [];
    public GhostFinishData? Finish { get; init; }
    public List<GhostRoomSegment> Rooms { get; init; } = [];

    public GhostData Clone() {
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
            Rooms = Rooms.Select(room => new GhostRoomSegment {
                Level = room.Level,
                Revisit = room.Revisit,
                Frames = room.Frames.Select(frame => new GhostFrameData {
                    HasPlayer = frame.HasPlayer,
                    Position = frame.Position,
                    Facing = frame.Facing,
                    CurrentAnimationId = frame.CurrentAnimationId,
                    CurrentAnimationFrame = frame.CurrentAnimationFrame,
                    Rotation = frame.Rotation,
                    Scale = frame.Scale,
                    SpriteColor = frame.SpriteColor,
                    HairColor = frame.HairColor,
                    HairSimulateMotion = frame.HairSimulateMotion,
                    HairCount = frame.HairCount,
                }).ToList(),
            }).ToList(),
        };
    }
}
