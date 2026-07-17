using System.Globalization;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal class InputLineBuffer {
    private readonly List<CommittedEntry> lines = [];
    private readonly List<char> pendingActions = [];
    private float? pendingFeatherAngle;
    private float? pendingFeatherMagnitude;
    private int pendingFrames;
    // Set after Seed; otherwise computed from structural pending at flush.
    private string? pendingSuffix;
    private bool hasPending;

    public void PushFrame(List<char> actions, float? featherAngle, float? featherMagnitude) {
        if (hasPending && MatchesPending(actions, featherAngle, featherMagnitude)) {
            pendingFrames++;
            return;
        }

        FlushPending();

        pendingActions.Clear();
        pendingActions.AddRange(actions);
        pendingFeatherAngle = featherAngle;
        pendingFeatherMagnitude = featherMagnitude;
        pendingFrames = 1;
        pendingSuffix = null;
        hasPending = true;
    }

    public void Clear() {
        lines.Clear();
        pendingActions.Clear();
        pendingFeatherAngle = null;
        pendingFeatherMagnitude = null;
        pendingFrames = 0;
        pendingSuffix = null;
        hasPending = false;
    }

    public void Seed(IReadOnlyList<string> seededLines) {
        Clear();
        if (seededLines.Count == 0) {
            return;
        }

        for (int i = 0; i < seededLines.Count - 1; i++) {
            lines.Add(ParseCommitted(seededLines[i]));
        }

        // Last line becomes the live hold so RLE can continue without rewriting strings.
        if (!TryParseLine(seededLines[^1], out pendingFrames, out string suffix)) {
            lines.Add(CommittedEntry.Raw(seededLines[^1]));
            return;
        }

        pendingSuffix = suffix;
        ParseSuffixIntoPending(suffix);
        hasPending = true;
    }

    public List<string> Snapshot() {
        var result = new List<string>(lines.Count + (hasPending ? 1 : 0));
        for (int i = 0; i < lines.Count; i++) {
            result.Add(Materialize(lines[i]));
        }

        if (hasPending) {
            result.Add(TasLineFormatter.FormatLine(pendingFrames, ResolvePendingSuffix()));
        }

        return result;
    }

    private void FlushPending() {
        if (!hasPending) {
            return;
        }

        lines.Add(new CommittedEntry(pendingFrames, ResolvePendingSuffix()));
        hasPending = false;
        pendingSuffix = null;
        pendingActions.Clear();
        pendingFeatherAngle = null;
        pendingFeatherMagnitude = null;
        pendingFrames = 0;
    }

    private string ResolvePendingSuffix() =>
        pendingSuffix ?? TasLineFormatter.FormatSuffix(
            pendingActions,
            pendingFeatherAngle,
            pendingFeatherMagnitude
        );

    private bool MatchesPending(List<char> actions, float? featherAngle, float? featherMagnitude) {
        if (actions.Count != pendingActions.Count) {
            return false;
        }

        for (int i = 0; i < actions.Count; i++) {
            if (actions[i] != pendingActions[i]) {
                return false;
            }
        }

        if (featherAngle.HasValue != pendingFeatherAngle.HasValue) {
            return false;
        }

        if (featherAngle is { } angle
            && pendingFeatherAngle is { } pendingAngle
            && (int) angle != (int) pendingAngle) {
            return false;
        }

        if (featherMagnitude.HasValue != pendingFeatherMagnitude.HasValue) {
            return false;
        }

        if (featherMagnitude is { } magnitude
            && pendingFeatherMagnitude is { } pendingMagnitude
            && !MagnitudesEqual(magnitude, pendingMagnitude)) {
            return false;
        }

        return true;
    }

    // Match the formatter's "0.###" rounding so hold detection agrees with written output.
    private static bool MagnitudesEqual(float left, float right) =>
        ToMilli(left) == ToMilli(right);

    private static int ToMilli(float value) => (int) Math.Round(value * 1000.0);

    private static CommittedEntry ParseCommitted(string line) {
        if (TryParseLine(line, out int frames, out string suffix)) {
            return new CommittedEntry(frames, suffix);
        }

        return CommittedEntry.Raw(line);
    }

    private static string Materialize(CommittedEntry entry) =>
        entry.IsRaw ? entry.Suffix : TasLineFormatter.FormatLine(entry.Frames, entry.Suffix);

    private static bool TryParseLine(string line, out int frames, out string suffix) {
        frames = 0;
        suffix = "";
        int comma = line.IndexOf(',');
        ReadOnlySpan<char> framePart = comma < 0 ? line.AsSpan() : line.AsSpan(0, comma);
        if (!int.TryParse(framePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out frames)) {
            return false;
        }

        suffix = comma < 0 ? "" : line[comma..];
        return true;
    }

    private void ParseSuffixIntoPending(string suffix) {
        pendingActions.Clear();
        pendingFeatherAngle = null;
        pendingFeatherMagnitude = null;

        if (suffix.Length == 0) {
            return;
        }

        // suffix is like ",L,J" or ",F,90,1" or ",L,F,90"
        int index = 0;
        while (index < suffix.Length) {
            if (suffix[index] != ',') {
                break;
            }

            index++;
            if (index >= suffix.Length) {
                break;
            }

            if (suffix[index] == 'F') {
                index++;
                if (index < suffix.Length && suffix[index] == ',') {
                    index++;
                }

                int angleEnd = suffix.IndexOf(',', index);
                ReadOnlySpan<char> angleSpan = angleEnd < 0
                    ? suffix.AsSpan(index)
                    : suffix.AsSpan(index, angleEnd - index);
                if (int.TryParse(angleSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out int angle)) {
                    pendingFeatherAngle = angle;
                }

                if (angleEnd < 0) {
                    return;
                }

                index = angleEnd + 1;
                int magEnd = suffix.IndexOf(',', index);
                ReadOnlySpan<char> magSpan = magEnd < 0
                    ? suffix.AsSpan(index)
                    : suffix.AsSpan(index, magEnd - index);
                if (float.TryParse(magSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out float magnitude)) {
                    pendingFeatherMagnitude = magnitude;
                }

                return;
            }

            pendingActions.Add(suffix[index]);
            index++;
        }
    }

    private readonly struct CommittedEntry {
        public CommittedEntry(int frames, string suffix) {
            Frames = frames;
            Suffix = suffix;
            IsRaw = false;
        }

        private CommittedEntry(string rawLine) {
            Frames = 0;
            Suffix = rawLine;
            IsRaw = true;
        }

        public static CommittedEntry Raw(string line) => new(line);

        public int Frames { get; }
        public string Suffix { get; }
        public bool IsRaw { get; }
    }
}
