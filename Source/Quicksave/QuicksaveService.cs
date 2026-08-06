using Celeste.Mod.QuickTools.Module;
using Celeste.Mod.QuickTools.Playback;
using Celeste.Mod.QuickTools.Quicksave.Storage;
using Monocle;

namespace Celeste.Mod.QuickTools.Quicksave;

internal static class QuicksaveService {
    public static QuicksaveData? Current => QuicksaveTracker.Current;
    public static bool IsTracking => QuicksaveTracker.IsTracking;

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

        Session session = SessionLoadHelper.PrepareSession(data);
        string tempTasPath = SessionLoadHelper.CreateTempTasPath(
            QuicksavePath.TempDirectory,
            QuicksaveConstants.TempTasPrefix
        );
        SessionLoadHelper.WriteAnchorTasFile(tempTasPath, data.Inputs, appendLoadFreezeFrame: true);

        Engine.Scene = new LevelLoader(session);
        QuicksavePlayback.Start(tempTasPath, data);
        Logger.Info(
            QuicksaveConstants.LogTag,
            $"Loading quicksave playback from {fullPath} in save slot {SaveSlotResolver.ResolveSlot(data.SaveUid)}"
        );
    }
}
