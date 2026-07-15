using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public class QuicksaveStartPoint {
    public string AreaSid { get; set; } = "";

    [JsonPropertyName("areaMode")]
    public string SideMode { get; set; } = "Normal";
    public string? Level { get; set; }
    public int? RespawnX { get; set; }
    public int? RespawnY { get; set; }

    public static QuicksaveStartPoint FromSession(Session session) {
        var point = new QuicksaveStartPoint {
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
        var areaKey = new AreaKey(area.ID, mode);

        if (area.Mode[(int) mode] == null) {
            throw new InvalidDataException($"Quicksave area {AreaSid} has no {mode} mode.");
        }

        var session = new Session(areaKey);
        if (RespawnX is { } x && RespawnY is { } y) {
            var respawn = new Vector2(x, y);
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
