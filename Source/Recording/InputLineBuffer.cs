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

    public List<string> Snapshot() => [..lines];

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
