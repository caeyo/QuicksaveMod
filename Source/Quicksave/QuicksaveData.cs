namespace Celeste.Mod.QuicksaveMod.Quicksave;

public class QuicksaveData {
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public QuicksaveStartPoint Start { get; set; } = new();
    public List<string> Inputs { get; set; } = [];

    public QuicksaveData Clone() {
        return new QuicksaveData {
            Version = Version,
            CreatedUtc = CreatedUtc,
            Start = new QuicksaveStartPoint {
                AreaSid = Start.AreaSid,
                SideMode = Start.SideMode,
                Level = Start.Level,
                RespawnX = Start.RespawnX,
                RespawnY = Start.RespawnY,
            },
            Inputs = [..Inputs],
        };
    }
}
