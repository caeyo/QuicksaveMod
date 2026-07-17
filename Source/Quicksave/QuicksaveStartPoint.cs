using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

internal class QuicksaveStartPoint {
    public string AreaSid { get; init; } = "";

    [JsonPropertyName("areaMode")]
    public string SideMode { get; init; } = "Normal";
    public string? Level { get; init; }
    public int? RespawnX { get; set; }
    public int? RespawnY { get; set; }

    public QuicksaveStartPoint Clone() => new() {
        AreaSid = AreaSid,
        SideMode = SideMode,
        Level = Level,
        RespawnX = RespawnX,
        RespawnY = RespawnY,
    };

    public static QuicksaveStartPoint FromSession(Session session) {
        QuicksaveStartPoint point = new() {
            AreaSid = session.Area.SID,
            SideMode = session.Area.Mode switch {
                AreaMode.Normal => "Normal",
                AreaMode.BSide => "BSide",
                AreaMode.CSide => "CSide",
                _ => "Normal",
            },
            Level = session.Level,
        };

        if (session.RespawnPoint is { } respawn) {
            point.RespawnX = (int) respawn.X;
            point.RespawnY = (int) respawn.Y;
        }

        return point;
    }

    public Session BuildSession() {
        AreaData area = AreaData.Get(AreaSid)
            ?? throw new InvalidDataException($"Quicksave area does not exist: {AreaSid}");
        AreaMode mode = SideMode switch {
            "BSide" => AreaMode.BSide,
            "CSide" => AreaMode.CSide,
            _ => AreaMode.Normal,
        };
        AreaKey areaKey = new(area.ID, mode);

        if (area.Mode[(int) mode] == null) {
            throw new InvalidDataException($"Quicksave area {AreaSid} has no {mode} mode.");
        }

        Session session = new(areaKey);
        if (RespawnX is { } x && RespawnY is { } y) {
            Vector2 respawn = new(x, y);
            LevelData levelData = session.MapData.GetAt(respawn)
                ?? throw new InvalidDataException(
                    $"Quicksave position {x}, {y} is not inside a room in {AreaSid}."
                );

            session.Level = levelData.Name;
            if (AreaData.GetCheckpoint(areaKey, session.Level) != null) {
                session = new Session(areaKey, session.Level);
            }

            session.FirstLevel = false;
            session.StartedFromBeginning = false;
            session.RespawnPoint = respawn;
            return session;
        }

        if (string.IsNullOrWhiteSpace(Level) || session.MapData.Get(Level) == null) {
            throw new InvalidDataException(
                $"Quicksave room '{Level}' does not exist in {AreaSid}."
            );
        }

        session.Level = Level;
        session.FirstLevel = session.LevelData == session.MapData.StartLevel();
        session.StartedFromBeginning = session.FirstLevel;
        return session;
    }
}
