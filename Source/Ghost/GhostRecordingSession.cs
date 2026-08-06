using Celeste.Mod.QuickTools.Quicksave;
using Celeste.Mod.QuickTools.Recording;
using Monocle;

namespace Celeste.Mod.QuickTools.Ghost;

internal static class GhostRecordingSession {
    internal static InputLineBuffer InputBuffer { get; } = new();
    private static readonly List<GhostRoomSegment> RoomSegments = [];
    private static readonly Dictionary<string, int> RevisitCounts = new(StringComparer.OrdinalIgnoreCase);

    private static QuicksaveData? recordingStartAnchor;
    private static GhostRoomSegment? currentSegment;
    private static bool suspended;

    private static long finishElapsedTicks;
    private static GhostFrameData? lastLiveFrame;
    private static GhostRoomSegment? lastLiveSegment;

    // Celeste's gameplay timer advances by a fixed 0.017s per frame.
    private const long TicksPerGameplayFrame = 170_000;

    public static bool IsAnchored => recordingStartAnchor != null;
    internal static bool IsRecordingInputs => IsAnchored && !suspended;
    public static QuicksaveData? RecordingStartAnchor => recordingStartAnchor?.Clone();

    public static void Suspend() => suspended = true;

    public static void Resume() => suspended = false;

    public static void AnchorFrom(QuicksaveData data) {
        recordingStartAnchor = data.Clone();
        InputBuffer.Clear();
        RoomSegments.Clear();
        RevisitCounts.Clear();
        currentSegment = null;
        ClearFinishTracking();

        if (Engine.Scene is Level level) {
            BeginSegment(level.Session.Level);
        }

        Logger.Info(GhostConstants.LogTag, "Ghost recording anchored.");
    }

    public static void Reset() {
        recordingStartAnchor = null;
        InputBuffer.Clear();
        RoomSegments.Clear();
        RevisitCounts.Clear();
        currentSegment = null;
        ClearFinishTracking();
    }

    public static void OnRoomTransition(Level level) {
        if (!IsAnchored || currentSegment == null) {
            return;
        }

        BeginSegment(level.Session.Level);
    }

    public static void OnLevelExit(Level level) {
        if (!IsAnchored) {
            return;
        }

        currentSegment = null;
    }

    public static bool TimelineMatchesRecordingStart(QuicksaveData timeline) {
        if (recordingStartAnchor == null) {
            return false;
        }

        return AnchorEquality.Equals(timeline, recordingStartAnchor);
    }

    public static GhostData? BuildGhostData() {
        if (recordingStartAnchor == null) {
            return null;
        }

        FlushCurrentSegment();

        var rooms = new List<GhostRoomSegment>(RoomSegments.Count);
        foreach (GhostRoomSegment segment in RoomSegments) {
            rooms.Add(new GhostRoomSegment {
                Level = segment.Level,
                Revisit = segment.Revisit,
                Frames = new List<GhostFrameData>(segment.Frames),
            });
        }

        return new GhostData {
            CreatedUtc = DateTime.UtcNow,
            Anchor = recordingStartAnchor.Clone(),
            Inputs = InputBuffer.Snapshot(),
            Finish = ComputeFinish(),
            Rooms = rooms,
        };
    }

    internal static void CaptureFrame(Player? player) {
        if (!IsAnchored || currentSegment == null) {
            return;
        }

        GhostFrameData frame = player is { Dead: false }
            ? GhostFrameData.FromPlayer(player)
            : GhostFrameData.WithoutPlayer;
        AppendFrame(frame);
    }

    internal static void AppendFrame(GhostFrameData frame) {
        currentSegment?.Frames.Add(frame);
        finishElapsedTicks += TicksPerGameplayFrame;

        if (frame.HasPlayer) {
            lastLiveFrame = frame;
            lastLiveSegment = currentSegment;
        }
    }

    internal static string CurrentRoomName =>
        currentSegment?.Level ?? (Engine.Scene is Level level ? level.Session.Level : "");

    internal static int CurrentRevisit =>
        currentSegment?.Revisit ?? 1;

    private static void BeginSegment(string roomName) {
        int revisit = RevisitCounts.TryGetValue(roomName, out int count) ? count + 1 : 1;
        RevisitCounts[roomName] = revisit;

        currentSegment = new GhostRoomSegment {
            Level = roomName,
            Revisit = revisit,
        };
        RoomSegments.Add(currentSegment);
    }

    private static void FlushCurrentSegment() {
        if (currentSegment != null && currentSegment.Frames.Count == 0) {
            RoomSegments.Remove(currentSegment);
            currentSegment = null;
        }
    }

    private static GhostFinishData? ComputeFinish() {
        if (lastLiveFrame == null || lastLiveSegment == null) {
            return null;
        }

        GhostFrameData frame = lastLiveFrame.Value;
        return new GhostFinishData {
            Room = lastLiveSegment.Level,
            Revisit = lastLiveSegment.Revisit,
            Position = frame.Position,
            SessionTimeTicks = finishElapsedTicks,
        };
    }

    private static void ClearFinishTracking() {
        finishElapsedTicks = 0;
        lastLiveFrame = null;
        lastLiveSegment = null;
    }

    internal static class AnchorEquality {
        public static bool Equals(QuicksaveData left, QuicksaveData right) {
            if (!string.Equals(left.SessionXml, right.SessionXml, StringComparison.Ordinal)) {
                return false;
            }

            if (left.Inputs.Count != right.Inputs.Count) {
                return false;
            }

            for (int i = 0; i < left.Inputs.Count; i++) {
                if (left.Inputs[i] != right.Inputs[i]) {
                    return false;
                }
            }

            return true;
        }
    }
}
