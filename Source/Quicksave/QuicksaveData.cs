namespace Celeste.Mod.QuicksaveMod.Quicksave;

public class QuicksaveData {
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? SaveUid { get; set; }
    public QuicksaveStartPoint Start { get; set; } = new();
    public List<string> Inputs { get; set; } = [];

    internal static bool IsValidSaveUid(string? value) =>
        value is { Length: 32 } && value.All(Uri.IsHexDigit);

    public QuicksaveData Clone() {
        return new QuicksaveData {
            Version = Version,
            CreatedUtc = CreatedUtc,
            SaveUid = SaveUid,
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
