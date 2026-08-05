using System.Text.Json;
using System.Text.Json.Serialization;
using Celeste.Mod.QuicksaveMod.Ghost.Serialization;
using Celeste.Mod.QuicksaveMod.Serialization;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostSerializer {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new Vector2JsonConverter() },
    };

    public static void Write(string path, GhostData data) {
        GhostFileDto file = GhostFileMapper.ToDto(data);
        file.Version = GhostData.CurrentVersion;
        string json = JsonSerializer.Serialize(file, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    public static GhostData Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"Ghost file not found: {path}");
        }

        GhostFileDto file = JsonSerializer.Deserialize<GhostFileDto>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"Failed to deserialize ghost: {path}");

        if (file.Version != GhostData.CurrentVersion) {
            throw new InvalidDataException($"Unsupported ghost version {file.Version} in {path}");
        }

        GhostData data = GhostFileMapper.FromDto(file);

        if (string.IsNullOrWhiteSpace(data.Anchor.Start.AreaSid)) {
            throw new InvalidDataException($"Ghost missing anchor start area SID: {path}");
        }

        if (string.IsNullOrWhiteSpace(data.Anchor.SessionXml)) {
            throw new InvalidDataException($"Ghost missing anchor session snapshot: {path}");
        }

        return data;
    }
}
