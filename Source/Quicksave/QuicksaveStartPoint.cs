using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

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

    public string BuildConsoleLoadCommand() {
        var command = new StringBuilder("console ");
        command.Append(SideMode switch {
            "BSide" => "hard",
            "CSide" => "rmx2",
            _ => "load",
        });
        command.Append(' ').Append(AreaSid);

        if (RespawnX is { } x && RespawnY is { } y) {
            command.Append(' ').Append(x.ToString(CultureInfo.InvariantCulture));
            command.Append(' ').Append(y.ToString(CultureInfo.InvariantCulture));
        } else if (!string.IsNullOrEmpty(Level)) {
            command.Append(' ').Append(Level);
        }

        return command.ToString();
    }
}
