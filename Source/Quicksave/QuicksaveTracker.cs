using Celeste.Mod.QuicksaveMod.Recording;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public sealed class QuicksaveTracker {
    public static QuicksaveTracker Instance { get; } = new();

    private readonly InputLineBuffer buffer = new();
    private QuicksaveStartPoint? startPoint;
    private string? startSessionXml;
    private Dictionary<string, string>? startModSessions;

    public bool IsTracking => startPoint != null && startSessionXml != null;

    public QuicksaveData? Current {
        get {
            if (startPoint == null || startSessionXml == null) {
                return null;
            }

            return new QuicksaveData {
                Start = new QuicksaveStartPoint {
                    AreaSid = startPoint.AreaSid,
                    SideMode = startPoint.SideMode,
                    Level = startPoint.Level,
                    RespawnX = startPoint.RespawnX,
                    RespawnY = startPoint.RespawnY,
                },
                Inputs = buffer.Snapshot(),
                // Session at input-buffer start — must match what playback begins from.
                SessionXml = startSessionXml,
                ModSessions = startModSessions == null
                    ? null
                    : new Dictionary<string, string>(startModSessions, StringComparer.OrdinalIgnoreCase),
            };
        }
    }

    public void Reset(Session session, Level level) {
        startPoint = QuicksaveStartPoint.FromSession(session);
        startSessionXml = SessionSnapshot.CaptureSessionXml(session);
        startModSessions = SessionSnapshot.CaptureModSessions();
        buffer.Clear();
    }

    /// <summary>
    /// After loading a quicksave, keep its start point, start session, and inputs as the
    /// base of the current tracking session so later saves include the full path.
    /// </summary>
    public void SeedFrom(QuicksaveData data) {
        if (string.IsNullOrWhiteSpace(data.SessionXml)) {
            throw new InvalidOperationException("Cannot seed tracker from a quicksave without a session snapshot.");
        }

        startPoint = new QuicksaveStartPoint {
            AreaSid = data.Start.AreaSid,
            SideMode = data.Start.SideMode,
            Level = data.Start.Level,
            RespawnX = data.Start.RespawnX,
            RespawnY = data.Start.RespawnY,
        };
        startSessionXml = data.SessionXml;
        startModSessions = data.ModSessions == null
            ? null
            : new Dictionary<string, string>(data.ModSessions, StringComparer.OrdinalIgnoreCase);
        buffer.Seed(data.Inputs);
    }

    public void RecordFrame(string line) {
        if (!IsTracking) {
            return;
        }

        buffer.PushFrame(line);
    }
}
