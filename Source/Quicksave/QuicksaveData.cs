namespace Celeste.Mod.QuicksaveMod.Quicksave;

internal sealed class QuicksaveData {
    public const int CurrentVersion = 3;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? SaveUid { get; set; }
    public QuicksaveStartPoint Start { get; init; } = new();
    public List<string> Inputs { get; init; } = [];

    // Session XML at input-buffer start (last death / load), not at save time.
    public string? SessionXml { get; init; }

    // Everest ModSession payloads at buffer start (YAML text, or base64: for binary).
    public Dictionary<string, string>? ModSessions { get; init; }

    internal static bool IsValidSaveUid(string? value) {
        if (value is not { Length: 32 }) {
            return false;
        }

        foreach (char t in value) {
            if (!Uri.IsHexDigit(t)) {
                return false;
            }
        }

        return true;
    }

    public QuicksaveData Clone() {
        return new QuicksaveData {
            Version = Version,
            CreatedUtc = CreatedUtc,
            SaveUid = SaveUid,
            Start = Start.Clone(),
            Inputs = [..Inputs],
            SessionXml = SessionXml,
            ModSessions = ModSessions == null
                ? null
                : new Dictionary<string, string>(ModSessions, StringComparer.OrdinalIgnoreCase),
        };
    }
}
