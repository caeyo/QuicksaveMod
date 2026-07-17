using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Recording;
using Celeste.Mod.QuicksaveMod.UI;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModModule : EverestModule {
    public static QuicksaveModModule Instance { get; private set; }

    public override Type SettingsType => typeof(QuicksaveModSettings);
    public static QuicksaveModSettings Settings => (QuicksaveModSettings) Instance._Settings;

    public override Type SaveDataType => typeof(QuicksaveModSaveData);
    public static QuicksaveModSaveData SaveData => (QuicksaveModSaveData) Instance._SaveData;

    private BrowserHandler? browserHandler;

    public QuicksaveModModule() {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel(QuicksaveConstants.LogTag, LogLevel.Verbose);
#else
        Logger.SetLogLevel(QuicksaveConstants.LogTag, LogLevel.Info);
#endif
    }

    public override void Initialize() {
        typeof(CelesteTasImports).ModInterop();
        typeof(QuicksaveModInterop).ModInterop();
        QuicksaveModInterop.InitExports();
    }

    public override void Load() {
        QuicksavePlayback.OnSeedNeeded = SeedTrackerFromLoadedQuicksave;

        LevelLoadHooks.Apply();
        BrowserInputHooks.Apply();
        RecordingHooks.Apply();
        PlaybackHooks.Apply();
        LoadFreezeHooks.Apply();

        browserHandler = new BrowserHandler();
        if (!ImGuiManager.Handlers.OfType<BrowserHandler>().Any()) {
            ImGuiManager.Handlers.Add(browserHandler);
        }
    }

    public override void Unload() {
        if (browserHandler != null) {
            browserHandler.Close();
            ImGuiManager.Handlers.Remove(browserHandler);
            BrowserHandler.ClearInstance();
            browserHandler = null;
        }

        LoadFreezeHooks.Unapply();
        PlaybackHooks.Unapply();
        RecordingHooks.Unapply();
        BrowserInputHooks.Unapply();
        LevelLoadHooks.Unapply();

        QuicksavePlayback.OnSeedNeeded = null;
    }

    private static void SeedTrackerFromLoadedQuicksave(QuicksaveData data) {
        QuicksaveTracker.SeedFrom(data);
        GameplayInputRecorder.ResetMapper();
    }
}
