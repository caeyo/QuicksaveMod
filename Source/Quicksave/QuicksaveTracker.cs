using Celeste.Mod.QuicksaveMod.Recording;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

internal static class QuicksaveTracker {
    internal static InputLineBuffer InputBuffer { get; } = new();
    private static QuicksaveStartPoint? startPoint;
    private static string? startSessionXml;
    private static Dictionary<string, string>? startModSessions;

    public static bool IsTracking => startPoint != null && startSessionXml != null;

    public static QuicksaveData? Current {
        get {
            if (startPoint == null || startSessionXml == null) {
                return null;
            }

            return new QuicksaveData {
                Start = startPoint.Clone(),
                Inputs = InputBuffer.Snapshot(),
                // Session at input-buffer start — must match what playback begins from.
                SessionXml = startSessionXml,
                ModSessions = startModSessions == null
                    ? null
                    : new Dictionary<string, string>(startModSessions, StringComparer.OrdinalIgnoreCase),
            };
        }
    }

    public static void Reset(Session session, Level level) {
        startPoint = QuicksaveStartPoint.FromSession(session);
        startSessionXml = SessionSnapshot.CaptureSessionXml(session);
        startModSessions = SessionSnapshot.CaptureModSessions();
        InputBuffer.Clear();
    }

    // After load, keep the quicksave's start + inputs as the base of the next tracking session.
    public static void SeedFrom(QuicksaveData data) {
        if (string.IsNullOrWhiteSpace(data.SessionXml)) {
            throw new InvalidOperationException(
                "Cannot seed tracker from a quicksave without a session snapshot."
            );
        }

        startPoint = data.Start.Clone();
        startSessionXml = data.SessionXml;
        startModSessions = data.ModSessions == null
            ? null
            : new Dictionary<string, string>(data.ModSessions, StringComparer.OrdinalIgnoreCase);
        InputBuffer.Seed(data.Inputs);
    }
}
