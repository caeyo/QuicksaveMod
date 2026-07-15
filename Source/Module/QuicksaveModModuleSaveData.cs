namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModModuleSaveData : EverestModuleSaveData {
    public string? SaveUid { get; set; }

    public string EnsureSaveUid() {
        if (string.IsNullOrWhiteSpace(SaveUid)) {
            SaveUid = Guid.NewGuid().ToString("N");
        }

        return SaveUid;
    }
}
