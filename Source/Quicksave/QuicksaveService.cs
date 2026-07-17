using System.Text;
using Celeste.Mod.QuicksaveMod.Module;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave.Storage;
using Celeste.Mod.QuicksaveMod.Recording;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Quicksave;

internal static class QuicksaveService {
    private static readonly UTF8Encoding TasFileEncoding = new(encoderShouldEmitUTF8Identifier: false);

    public static QuicksaveData? Current => QuicksaveTracker.Current;
    public static bool IsTracking => QuicksaveTracker.IsTracking;
    public static bool IsTrackingSuspended => GameplayInputRecorder.IsSuspended;

    public static void SuspendTracking() => GameplayInputRecorder.Suspend();

    public static void ResumeTracking() => GameplayInputRecorder.Resume();

    public static void SaveQuicksave(string? fileName = null, string? subdirectory = null) {
        QuicksaveData data = QuicksaveTracker.Current
            ?? throw new InvalidOperationException("No quicksave tracking session is active.");

        if (Engine.Scene is not Level) {
            throw new InvalidOperationException("Quicksaves can only be saved while in a level.");
        }

        if (SaveData.Instance == null) {
            throw new InvalidOperationException("No SaveData is loaded.");
        }

        string directory = QuicksavePath.ResolveSaveDirectory(subdirectory);
        Directory.CreateDirectory(directory);

        fileName ??= $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{QuicksaveConstants.Extension}";
        if (!fileName.EndsWith(QuicksaveConstants.Extension, StringComparison.OrdinalIgnoreCase)) {
            fileName += QuicksaveConstants.Extension;
        }

        data.SaveUid = SaveSlotResolver.EnsureCurrentSaveUid();
        data.CreatedUtc = DateTime.UtcNow;
        string path = Path.Combine(directory, fileName);
        QuicksaveSerializer.Write(path, data);
        Logger.Info(QuicksaveConstants.LogTag, $"Saved quicksave to {path}");
    }

    public static void LoadQuicksave(string filePath) {
        string fullPath = QuicksavePath.ResolveQuicksaveFilePath(filePath, mustExist: true);
        QuicksaveData data = QuicksaveSerializer.Read(fullPath);
        int targetSlot = SaveSlotResolver.ResolveSlot(data.SaveUid);
        SaveSlotResolver.ActivateSaveSlot(targetSlot);

        Session session = BuildSessionForLoad(data);
        SessionSnapshot.RestoreModSessions(data.ModSessions);
        if (SaveData.Instance != null) {
            SaveData.Instance.CurrentSession = session;
        }

        string tempDir = QuicksavePath.TempDirectory;
        Directory.CreateDirectory(tempDir);

        string tempTasPath = Path.Combine(
            tempDir,
            $"{QuicksaveConstants.TempTasPrefix}{Guid.NewGuid():N}.tas"
        );
        WriteTempTasFile(tempTasPath, data);

        Engine.Scene = new LevelLoader(session);
        QuicksavePlayback.Start(tempTasPath, data);
        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Loading quicksave playback from {fullPath} in save slot {targetSlot}"
        );
    }

    private static Session BuildSessionForLoad(QuicksaveData data) {
        if (!string.IsNullOrWhiteSpace(data.SessionXml)) {
            return SessionSnapshot.RestoreSession(data.SessionXml, data.Start);
        }

        return data.Start.BuildSession();
    }

    private static void WriteTempTasFile(string path, QuicksaveData data) {
        using StreamWriter writer = new(path, false, TasFileEncoding);

        foreach (string line in data.Inputs) {
            TasLineFormatter.WriteFileLine(writer, line);
        }

        writer.WriteLine(GetPlaybackBreakpointLine());
        TasLineFormatter.WriteFileLine(writer, "1");
    }

    private static string GetPlaybackBreakpointLine() {
        PlaybackSpeed speed = QuicksaveModModule.Settings.PlaybackSpeed;
        return speed == PlaybackSpeed.Max ? "***" : $"***{(int) speed}";
    }
}
