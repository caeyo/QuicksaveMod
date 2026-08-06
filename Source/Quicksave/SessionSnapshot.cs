using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuickTools.Quicksave;

internal static class SessionSnapshot {
    private const string BinaryModSessionPrefix = "base64:";

    private static readonly XmlSerializer SessionXmlSerializer = new(typeof(Session));

    public static string CaptureSessionXml(Session session) {
        byte[] bytes = UserIO.Serialize(session);
        return Encoding.UTF8.GetString(bytes);
    }

    public static Session RestoreSession(string sessionXml, QuicksaveStartPoint start) {
        byte[] bytes = Encoding.UTF8.GetBytes(sessionXml);
        using MemoryStream stream = new(bytes);

        Session session;
        try {
            session = (Session)SessionXmlSerializer.Deserialize(stream)!;
        } catch (Exception e) {
            throw new InvalidDataException("Failed to deserialize quicksave session snapshot.", e);
        }

        // Inputs were recorded from tracker start, not the player's current room
        ApplyStartPoint(session, start);
        session.JustStarted = false;
        session.InArea = true;
        return session;
    }

    public static Dictionary<string, string> CaptureModSessions() {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        int slot = RequireFileSlot();

        foreach (EverestModule module in Everest.Modules) {
            if (module.SessionType == null) {
                continue;
            }

            try {
                byte[]? data = module.SerializeSession(slot);
                if (data is not { Length: > 0 }) {
                    continue;
                }

                // YAML ModSessions are UTF-8 text; binary sessions stay base64-prefixed
                result[module.Metadata.Name] = module._Session is EverestModuleBinarySession
                    ? BinaryModSessionPrefix + Convert.ToBase64String(data)
                    : Encoding.UTF8.GetString(data);
            } catch (Exception e) {
                Logger.Warn(
                    QuicksaveConstants.LogTag,
                    $"Failed to serialize ModSession for {module.Metadata.Name}: {e.Message}"
                );
            }
        }

        return result;
    }

    public static void RestoreModSessions(IReadOnlyDictionary<string, string>? modSessions) {
        if (modSessions == null || modSessions.Count == 0) {
            return;
        }

        int slot = RequireFileSlot();
        Dictionary<string, EverestModule> modulesByName = BuildModuleLookup();

        foreach ((string name, string payload) in modSessions) {
            if (!modulesByName.TryGetValue(name, out EverestModule? module)
                || module.SessionType == null) {
                continue;
            }

            try {
                byte[] data = DecodeModSessionPayload(payload);
                module.DeserializeSession(slot, data);
            } catch (Exception e) {
                Logger.Warn(
                    QuicksaveConstants.LogTag,
                    $"Failed to deserialize ModSession for {name}: {e.Message}"
                );
            }
        }
    }

    private static Dictionary<string, EverestModule> BuildModuleLookup() {
        Dictionary<string, EverestModule> lookup = new(StringComparer.OrdinalIgnoreCase);
        foreach (EverestModule module in Everest.Modules) {
            lookup.TryAdd(module.Metadata.Name, module);
        }

        return lookup;
    }

    private static int RequireFileSlot() {
        if (SaveData.Instance == null) {
            throw new InvalidOperationException("No SaveData is loaded.");
        }

        return SaveData.Instance.FileSlot;
    }

    private static byte[] DecodeModSessionPayload(string payload) {
        if (payload.StartsWith(BinaryModSessionPrefix, StringComparison.Ordinal)) {
            return Convert.FromBase64String(payload[BinaryModSessionPrefix.Length..]);
        }

        return Encoding.UTF8.GetBytes(payload);
    }

    private static void ApplyStartPoint(Session session, QuicksaveStartPoint start) {
        if (start.RespawnX is { } x && start.RespawnY is { } y) {
            Vector2 respawn = new(x, y);
            LevelData? levelData = session.MapData.GetAt(respawn);
            if (levelData != null) {
                session.Level = levelData.Name;
            } else if (!string.IsNullOrWhiteSpace(start.Level)) {
                session.Level = start.Level;
            }

            session.RespawnPoint = respawn;
            session.FirstLevel = false;
            session.StartedFromBeginning = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(start.Level) && session.MapData.Get(start.Level) != null) {
            session.Level = start.Level;
            session.FirstLevel = session.LevelData == session.MapData.StartLevel();
            session.StartedFromBeginning = session.FirstLevel;
        }
    }
}
