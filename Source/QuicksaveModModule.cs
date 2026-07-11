using System;

namespace Celeste.Mod.QuicksaveMod;

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

    public override void Load() {
        // TODO: apply any hooks that should always be active
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
    }
}