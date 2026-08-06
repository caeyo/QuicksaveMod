using System.Globalization;
using System.Text;

namespace Celeste.Mod.QuickTools.Recording;

internal static class TasLineFormatter {
    private const int MaxFramesDigits = 4;
    // Shared across calls; safe because Celeste's update loop is single-threaded
    private static readonly StringBuilder SharedBuilder = new(64);

    // Everything after the frame count (e.g. ",L,J" or ",F,90,1"), or empty for a neutral frame
    internal static string FormatSuffix(
        IReadOnlyList<string> actions,
        float? featherAngle = null,
        float? featherMagnitude = null
    ) {
        SharedBuilder.Clear();
        AppendSuffix(SharedBuilder, actions, featherAngle, featherMagnitude);
        return SharedBuilder.ToString();
    }

    internal static string FormatLine(int frames, string suffix) {
        SharedBuilder.Clear();
        SharedBuilder.Append(frames);
        SharedBuilder.Append(suffix);
        return SharedBuilder.ToString();
    }

    // Left-padded (4-digit) TAS line matching historic FormatFileLine output
    internal static void WriteFileLine(TextWriter writer, string line) {
        ReadOnlySpan<char> span = line.AsSpan().TrimStart();
        int comma = span.IndexOf(',');
        ReadOnlySpan<char> framePart = comma < 0 ? span : span[..comma];
        ReadOnlySpan<char> suffix = comma < 0 ? ReadOnlySpan<char>.Empty : span[comma..];

        if (!int.TryParse(framePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int frames)) {
            writer.WriteLine(span);
            return;
        }

        Span<char> digits = stackalloc char[16];
        if (!frames.TryFormat(digits, out int written, provider: CultureInfo.InvariantCulture)) {
            writer.WriteLine(span);
            return;
        }

        int pad = MaxFramesDigits - written;
        for (int i = 0; i < pad; i++) {
            writer.Write(' ');
        }

        writer.Write(digits[..written]);
        if (!suffix.IsEmpty) {
            writer.Write(suffix);
        }

        writer.WriteLine();
    }

    private static void AppendSuffix(
        StringBuilder builder,
        IReadOnlyList<string> actions,
        float? featherAngle,
        float? featherMagnitude
    ) {
        foreach (string action in actions) {
            builder.Append(',').Append(action);
        }

        if (featherAngle is { } angle) {
            builder.Append(",F,").Append((int) angle);
            if (featherMagnitude is { } magnitude) {
                builder.Append(',').Append(magnitude.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
    }
}
