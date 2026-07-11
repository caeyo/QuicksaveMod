using Celeste.Mod.QuicksaveMod.Recording;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

public sealed class QuicksaveTracker {
    public static QuicksaveTracker Instance { get; } = new();

    private readonly InputLineBuffer buffer = new();
    private QuicksaveStartPoint? startPoint;

    public bool IsTracking => startPoint != null;

    public QuicksaveData? Current {
        get {
            if (startPoint == null) {
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
            };
        }
    }

    public void Reset(Session session, Level level) {
        startPoint = QuicksaveStartPoint.FromSession(session);
        buffer.Clear();
    }

    public void RecordFrame(string line) {
        if (!IsTracking) {
            return;
        }

        buffer.PushFrame(line);
    }
}
