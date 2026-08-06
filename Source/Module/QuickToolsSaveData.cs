namespace Celeste.Mod.QuickTools.Module;

public class QuickToolsSaveData : EverestModuleSaveData {
    public string? SaveUid { get; set; }

    public string EnsureSaveUid() {
        if (string.IsNullOrWhiteSpace(SaveUid)) {
            SaveUid = Guid.NewGuid().ToString("N");
        }

        return SaveUid;
    }
}
