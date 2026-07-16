namespace Celeste.Mod.QuicksaveMod.Recording;

public class InputLineBuffer {
    private readonly List<string> lines = [];
    private string? pendingLine;

    public IReadOnlyList<string> Lines => lines;

    public void PushFrame(string line) {
        if (pendingLine == line) {
            lines[^1] = IncrementFrameCount(lines[^1]);
            return;
        }

        pendingLine = line;
        lines.Add(line);
    }

    public void Clear() {
        lines.Clear();
        pendingLine = null;
    }

    public void Seed(IReadOnlyList<string> seededLines) {
        Clear();
        if (seededLines.Count == 0) {
            return;
        }

        lines.AddRange(seededLines);
        // Sample() always emits a 1-frame line; normalize so RLE can continue holding.
        pendingLine = ToSingleFrameLine(lines[^1]);
    }

    public List<string> Snapshot() => [..lines];

    private static string ToSingleFrameLine(string line) {
        int comma = line.IndexOf(',');
        if (comma <= 0) {
            return "1";
        }

        return "1" + line[comma..];
    }

    private static string IncrementFrameCount(string line) {
        int comma = line.IndexOf(',');
        if (comma <= 0) {
            return int.TryParse(line, out int neutralFrames)
                ? $"{neutralFrames + 1}"
                : line;
        }

        if (!int.TryParse(line.AsSpan(0, comma), out int frames)) {
            return line;
        }

        return $"{frames + 1}{line[comma..]}";
    }
}
