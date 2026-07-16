namespace Celeste.Mod.QuicksaveMod.Quicksave;

public class QuicksaveData {
    public const int CurrentVersion = 3;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? SaveUid { get; set; }
    public QuicksaveStartPoint Start { get; set; } = new();
    public List<string> Inputs { get; set; } = [];

    /// <summary>
    /// XML serialization of <see cref="Session"/> at the start of the input buffer
    /// (last death / load), not at save time.
    /// </summary>
    public string? SessionXml { get; set; }

    /// <summary>
    /// Everest ModSession payloads at input-buffer start, keyed by mod name
    /// (YAML text, or base64: for binary sessions).
    /// </summary>
    public Dictionary<string, string>? ModSessions { get; set; }

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
            SessionXml = SessionXml,
            ModSessions = ModSessions == null
                ? null
                : new Dictionary<string, string>(ModSessions, StringComparer.OrdinalIgnoreCase),
        };
    }
}
