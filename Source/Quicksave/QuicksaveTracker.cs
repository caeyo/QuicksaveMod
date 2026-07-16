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

    /// <summary>
    /// After loading a quicksave, keep its start point and inputs as the base of the
    /// current tracking session so later saves include the full path.
    /// </summary>
    public void SeedFrom(QuicksaveData data) {
        startPoint = new QuicksaveStartPoint {
            AreaSid = data.Start.AreaSid,
            SideMode = data.Start.SideMode,
            Level = data.Start.Level,
            RespawnX = data.Start.RespawnX,
            RespawnY = data.Start.RespawnY,
        };
        buffer.Seed(data.Inputs);
    }

    public void RecordFrame(string line) {
        if (!IsTracking) {
            return;
        }

        buffer.PushFrame(line);
    }
}
