using Celeste.Mod.QuicksaveMod.Ghost.Serialization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostFileMapper {
    public static GhostFileDto ToDto(GhostData data) {
        return new GhostFileDto {
            Version = GhostData.CurrentVersion,
            CreatedUtc = data.CreatedUtc,
            Anchor = data.Anchor,
            Inputs = [..data.Inputs],
            Finish = data.Finish == null
                ? null
                : new GhostFinishDto {
                    Room = data.Finish.Room,
                    Revisit = data.Finish.Revisit,
                    Position = [data.Finish.Position.X, data.Finish.Position.Y],
                    SessionTimeTicks = data.Finish.SessionTimeTicks,
                },
            Rooms = data.Rooms.Select(room => new GhostRoomDto {
                Level = room.Level,
                Revisit = room.Revisit,
                Frames = GhostFrameCodec.Encode(room.Frames),
            }).ToList(),
        };
    }

    public static GhostData FromDto(GhostFileDto file) {
        return new GhostData {
            Version = file.Version,
            CreatedUtc = file.CreatedUtc,
            Anchor = file.Anchor,
            Inputs = [..file.Inputs],
            Finish = file.Finish == null
                ? null
                : new GhostFinishData {
                    Room = file.Finish.Room,
                    Revisit = file.Finish.Revisit,
                    Position = file.Finish.Position.Length >= 2
                        ? new Vector2(file.Finish.Position[0], file.Finish.Position[1])
                        : Vector2.Zero,
                    SessionTimeTicks = file.Finish.SessionTimeTicks,
                },
            Rooms = file.Rooms.Select(room => new GhostRoomSegment {
                Level = room.Level,
                Revisit = room.Revisit,
                Frames = GhostFrameCodec.Decode(room.Frames),
            }).ToList(),
        };
    }
}
