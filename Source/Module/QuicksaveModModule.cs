using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Playback;
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

    private QuicksaveBrowserHandler? browserHandler;

    public QuicksaveModModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(QuicksaveModModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(QuicksaveModModule), LogLevel.Info);
#endif
    }

    public override void Initialize() {
        typeof(CelesteTasImports).ModInterop();
        typeof(QuicksaveModInterop).ModInterop();
        QuicksaveModInterop.InitExports();
    }

    public override void Load() {
        QuicksaveHooks.Apply();
        QuicksaveBrowserHooks.Apply();
        GameplayInputRecorder.Apply();
        QuicksavePlayback.Apply();
        QuicksaveLoadFreeze.Apply();

        browserHandler = new QuicksaveBrowserHandler();
        if (!ImGuiManager.Handlers.OfType<QuicksaveBrowserHandler>().Any()) {
            ImGuiManager.Handlers.Add(browserHandler);
        }
    }

    public override void Unload() {
        if (browserHandler != null) {
            browserHandler.Close();
            ImGuiManager.Handlers.Remove(browserHandler);
            QuicksaveBrowserHandler.ClearInstance();
            browserHandler = null;
        }

        QuicksaveBrowserHooks.Unapply();
        GameplayInputRecorder.Unapply();
        QuicksaveHooks.Unapply();
        QuicksavePlayback.Unapply();
        QuicksaveLoadFreeze.Unapply();
    }
}
