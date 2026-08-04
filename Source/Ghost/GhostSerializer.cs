using System.Text.Json;
using System.Text.Json.Serialization;
using Celeste.Mod.QuicksaveMod.Serialization;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostSerializer {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new Vector2JsonConverter() },
    };

    public static void Write(string path, GhostData data) {
        data.Version = GhostData.CurrentVersion;
        string json = JsonSerializer.Serialize(data, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    public static GhostData Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"Ghost file not found: {path}");
        }

        GhostData data = JsonSerializer.Deserialize<GhostData>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"Failed to deserialize ghost: {path}");

        if (data.Version is < 1 or > GhostData.CurrentVersion) {
            throw new InvalidDataException($"Unsupported ghost version {data.Version} in {path}");
        }

        if (string.IsNullOrWhiteSpace(data.Anchor.Start.AreaSid)) {
            throw new InvalidDataException($"Ghost missing anchor start area SID: {path}");
        }

        if (string.IsNullOrWhiteSpace(data.Anchor.SessionXml)) {
            throw new InvalidDataException($"Ghost missing anchor session snapshot: {path}");
        }

        return data;
    }
}
