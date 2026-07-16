using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public static class QuicksaveSerializer {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Write(string path, QuicksaveData data) {
        data.Version = QuicksaveData.CurrentVersion;
        string json = JsonSerializer.Serialize(data, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    public static QuicksaveData Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"Quicksave file not found: {path}");
        }

        var data = JsonSerializer.Deserialize<QuicksaveData>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"Failed to deserialize quicksave: {path}");

        if (data.Version is < 1 or > QuicksaveData.CurrentVersion) {
            throw new InvalidDataException($"Unsupported quicksave version {data.Version} in {path}");
        }

        if (data.Version == 1) {
            data.SaveUid = null;
        } else if (string.IsNullOrWhiteSpace(data.SaveUid)) {
            data.SaveUid = null;
        } else if (!QuicksaveData.IsValidSaveUid(data.SaveUid)) {
            throw new InvalidDataException($"Quicksave has an invalid save UID: {path}");
        }

        if (string.IsNullOrWhiteSpace(data.Start.AreaSid)) {
            throw new InvalidDataException($"Quicksave missing start area SID: {path}");
        }

        if (data.Version >= 3 && string.IsNullOrWhiteSpace(data.SessionXml)) {
            throw new InvalidDataException($"Quicksave missing session snapshot: {path}");
        }

        return data;
    }
}
