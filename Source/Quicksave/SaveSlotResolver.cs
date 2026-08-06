using System.Text;
using Celeste.Mod.QuickTools.Module;

namespace Celeste.Mod.QuickTools.Quicksave;

internal static class SaveSlotResolver {
    private const int DebugSlot = -1;

    public static string EnsureCurrentSaveUid() {
        if (SaveData.Instance == null) {
            throw new InvalidOperationException("No Celeste save file is active.");
        }

        return QuickToolsModule.SaveData.EnsureSaveUid();
    }

    public static int ResolveSlot(string? saveUid) {
        if (!QuicksaveData.IsValidSaveUid(saveUid)) {
            return DebugSlot;
        }

        if (SaveData.Instance is { } current
            && string.Equals(
                QuickToolsModule.SaveData.SaveUid,
                saveUid,
                StringComparison.OrdinalIgnoreCase
            )) {
            return current.FileSlot;
        }

        foreach (int slot in EnumerateSlotsWithModSave()) {
            if (TryReadSlotSaveUid(slot) is { } candidateUid
                && string.Equals(candidateUid, saveUid, StringComparison.OrdinalIgnoreCase)) {
                return slot;
            }
        }

        return DebugSlot;
    }

    public static void ActivateSaveSlot(int targetSlot) {
        if (SaveData.Instance?.FileSlot == targetSlot) {
            return;
        }

        if (targetSlot == DebugSlot) {
            SaveData.InitializeDebugMode();
            return;
        }

        string saveName = SaveData.GetFilename(targetSlot);
        SaveData? saveData = UserIO.Load<SaveData>(saveName);
        if (saveData == null) {
            Logger.Warn(
                QuicksaveConstants.LogTag,
                $"Save slot {targetSlot} could not be loaded; falling back to debug."
            );
            SaveData.InitializeDebugMode();
            return;
        }

        SaveData.Start(saveData, targetSlot);
    }

    private static IEnumerable<int> EnumerateSlotsWithModSave() {
        SortedSet<int> slots = [DebugSlot];
        string saveDirectory = UserIO.GetSaveFilePath();
        string modName = QuickToolsModule.Instance.Metadata.Name;
        string suffix = $"-modsave-{modName}.celeste";

        if (!Directory.Exists(saveDirectory)) {
            return slots;
        }

        foreach (string path in Directory.EnumerateFiles(saveDirectory, $"*-modsave-{modName}.celeste")) {
            string fileName = Path.GetFileName(path);
            if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string slotName = fileName[..^suffix.Length];
            if (slotName.Equals("debug", StringComparison.OrdinalIgnoreCase)) {
                slots.Add(DebugSlot);
            } else if (int.TryParse(slotName, out int slot) && slot >= 0) {
                slots.Add(slot);
            }
        }

        return slots;
    }

    private static string? TryReadSlotSaveUid(int slot) {
        byte[]? bytes = QuickToolsModule.Instance.ReadSaveData(slot);
        if (bytes == null || bytes.Length == 0) {
            return null;
        }

        try {
            // Peek a disposable copy — DeserializeSaveData would replace the live module save data.
            QuickToolsSaveData data = YamlHelper.Deserializer.Deserialize<QuickToolsSaveData>(
                Encoding.UTF8.GetString(bytes)
            );
            return string.IsNullOrWhiteSpace(data.SaveUid) ? null : data.SaveUid;
        } catch (Exception e) {
            Logger.Warn(
                QuicksaveConstants.LogTag,
                $"Failed to read QuickTools save identity for slot {slot}: {e.Message}"
            );
            return null;
        }
    }
}
