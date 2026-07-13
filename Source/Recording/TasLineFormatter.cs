using System.Globalization;
using System.Text;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class TasLineFormatter {
    private const int MaxFramesDigits = 4;

    internal static string Format(
        int frames,
        IReadOnlyList<char> actions,
        float? featherAngle = null,
        float? featherMagnitude = null
    ) {
        var builder = new StringBuilder();
        builder.Append(frames.ToString(CultureInfo.InvariantCulture));

        foreach (char action in actions) {
            builder.Append(',').Append(action);
        }

        if (featherAngle is { } angle) {
            builder.Append(",F,").Append(((int) angle).ToString(CultureInfo.InvariantCulture));
            if (featherMagnitude is { } magnitude) {
                builder.Append(',').Append(magnitude.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    internal static string FormatFileLine(string line) {
        line = line.TrimStart();
        int comma = line.IndexOf(',');
        ReadOnlySpan<char> framePart = comma < 0 ? line : line.AsSpan(0, comma);
        string suffix = comma < 0 ? "" : line[comma..];

        if (!int.TryParse(framePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int frames)) {
            return line;
        }

        return frames.ToString(CultureInfo.InvariantCulture).PadLeft(MaxFramesDigits) + suffix;
    }
}
