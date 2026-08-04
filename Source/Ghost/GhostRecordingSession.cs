using Celeste.Mod.QuicksaveMod.Ghost.Recording;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Recording;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal static class GhostRecordingSession {
    internal static InputLineBuffer InputBuffer { get; } = new();
    private static readonly List<GhostRoomSegment> RoomSegments = [];
    private static readonly Dictionary<string, int> RevisitCounts = new(StringComparer.OrdinalIgnoreCase);

    private static QuicksaveData? recordingStartAnchor;
    private static GhostRoomSegment? currentSegment;
    private static GhostFrameRecorder? recorder;
    private static bool suspended;

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

        recorder?.RemoveSelf();
        recorder = null;

        if (Engine.Scene is Level level) {
            AttachRecorder(level);
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
        recorder?.RemoveSelf();
        recorder = null;
    }

    public static void OnRoomTransition(Level level) {
        if (!IsAnchored || currentSegment == null) {
            return;
        }

        currentSegment.TargetLevel = level.Session.Level;
        BeginSegment(level.Session.Level);
    }

    public static void OnLevelExit(Level level) {
        if (!IsAnchored || currentSegment == null) {
            return;
        }

        currentSegment.TargetLevel = "LevelExit";
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

        GhostFinishData? finish = ComputeFinish();
        return new GhostData {
            CreatedUtc = DateTime.UtcNow,
            Anchor = recordingStartAnchor.Clone(),
            Inputs = InputBuffer.Snapshot(),
            Finish = finish,
            Rooms = RoomSegments.Select(segment => new GhostRoomSegment {
                Level = segment.Level,
                Revisit = segment.Revisit,
                TargetLevel = segment.TargetLevel,
                Frames = segment.Frames.Select(frame => new GhostFrameData {
                    HasPlayer = frame.HasPlayer,
                    SessionTimeTicks = frame.SessionTimeTicks,
                    Position = frame.Position,
                    Facing = frame.Facing,
                    CurrentAnimationId = frame.CurrentAnimationId,
                    CurrentAnimationFrame = frame.CurrentAnimationFrame,
                    Rotation = frame.Rotation,
                    Scale = frame.Scale,
                    SpriteColor = frame.SpriteColor,
                    HairColor = frame.HairColor,
                    HairSimulateMotion = frame.HairSimulateMotion,
                    HairCount = frame.HairCount,
                    HitboxWidth = frame.HitboxWidth,
                    HitboxHeight = frame.HitboxHeight,
                    HitboxLeft = frame.HitboxLeft,
                    HitboxTop = frame.HitboxTop,
                }).ToList(),
            }).ToList(),
        };
    }

    internal static void AppendFrame(GhostFrameData frame) {
        currentSegment?.Frames.Add(frame);
    }

    internal static string CurrentRoomName =>
        currentSegment?.Level ?? (Engine.Scene is Level level ? level.Session.Level : "");

    internal static int CurrentRevisit =>
        currentSegment?.Revisit ?? 1;

    private static void AttachRecorder(Level level) {
        recorder?.RemoveSelf();
        level.Add(recorder = new GhostFrameRecorder());
    }

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
        GhostFrameData? last = null;
        GhostRoomSegment? lastSegment = null;

        foreach (GhostRoomSegment segment in RoomSegments) {
            foreach (GhostFrameData frame in segment.Frames) {
                if (!frame.HasPlayer) {
                    continue;
                }

                last = frame;
                lastSegment = segment;
            }
        }

        if (last == null || lastSegment == null) {
            return null;
        }

        return new GhostFinishData {
            Room = lastSegment.Level,
            Revisit = lastSegment.Revisit,
            Position = last.Position,
            SessionTimeTicks = last.SessionTimeTicks,
        };
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
