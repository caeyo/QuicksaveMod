using System.Globalization;
using System.Text;

namespace Celeste.Mod.QuicksaveMod.Recording;

internal static class TasLineFormatter {
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
            builder.Append(",F,").Append(angle.ToString(CultureInfo.InvariantCulture));
            if (featherMagnitude is { } magnitude) {
                builder.Append(',').Append(magnitude.ToString(CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }
}
