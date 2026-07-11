using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Recording;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModModule : EverestModule {
    public static QuicksaveModModule Instance { get; private set; }

    public override Type SettingsType => typeof(QuicksaveModModuleSettings);
    public static QuicksaveModModuleSettings Settings => (QuicksaveModModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(QuicksaveModModuleSession);
    public static QuicksaveModModuleSession Session => (QuicksaveModModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(QuicksaveModModuleSaveData);
    public static QuicksaveModModuleSaveData SaveData => (QuicksaveModModuleSaveData) Instance._SaveData;

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
    }

    public override void Load() {
        QuicksaveHooks.Apply();
        GameplayInputRecorder.Apply();
        Playback.QuicksavePlayback.Apply();
    }

    public override void Unload() {
        GameplayInputRecorder.Unapply();
        QuicksaveHooks.Unapply();
        Playback.QuicksavePlayback.Unapply();
    }
}