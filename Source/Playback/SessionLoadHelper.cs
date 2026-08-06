using System.Text;
using Celeste.Mod.QuickTools.Module;
using Celeste.Mod.QuickTools.Quicksave;
using Celeste.Mod.QuickTools.Recording;

namespace Celeste.Mod.QuickTools.Playback;

internal static class SessionLoadHelper {
    private static readonly UTF8Encoding TasFileEncoding = new(encoderShouldEmitUTF8Identifier: false);

    public static Session PrepareSession(QuicksaveData data) {
        int targetSlot = SaveSlotResolver.ResolveSlot(data.SaveUid);
        SaveSlotResolver.ActivateSaveSlot(targetSlot);

        Session session = BuildSession(data);
        SessionSnapshot.RestoreModSessions(data.ModSessions);
        if (SaveData.Instance != null) {
            // Treat this load as the overworld stats baseline so return-to-map does not
            // count up deaths/berries that were already on the file before the load.
            session.OldStats = SaveData.Instance.Areas[session.Area.ID].Clone();
            SaveData.Instance.CurrentSession = session;
        }

        return session;
    }

    private static Session BuildSession(QuicksaveData data) {
        if (!string.IsNullOrWhiteSpace(data.SessionXml)) {
            return SessionSnapshot.RestoreSession(data.SessionXml, data.Start);
        }

        return data.Start.BuildSession();
    }

    public static string CreateTempTasPath(string tempDirectory, string tempPrefix) {
        Directory.CreateDirectory(tempDirectory);
        return Path.Combine(tempDirectory, $"{tempPrefix}{Guid.NewGuid():N}.tas");
    }

    private static string GetPlaybackBreakpointLine() {
        PlaybackSpeed speed = QuickToolsModule.Settings.PlaybackSpeed;
        return speed == PlaybackSpeed.Max ? "***" : $"***{(int) speed}";
    }

    public static void WriteAnchorTasFile(
        string path,
        IReadOnlyList<string> anchorInputs,
        IReadOnlyList<string>? inputsAfterBreakpoint = null,
        bool appendLoadFreezeFrame = false
    ) {
        using StreamWriter writer = new(path, false, TasFileEncoding);

        foreach (string line in anchorInputs) {
            TasLineFormatter.WriteFileLine(writer, line);
        }

        writer.WriteLine(GetPlaybackBreakpointLine());

        if (inputsAfterBreakpoint != null) {
            foreach (string line in inputsAfterBreakpoint) {
                TasLineFormatter.WriteFileLine(writer, line);
            }
        }

        if (appendLoadFreezeFrame) {
            TasLineFormatter.WriteFileLine(writer, "1");
        }
    }
}
