using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuicksaveMod.Ghost;
using Celeste.Mod.QuicksaveMod.Ghost.Playback;
using Celeste.Mod.QuicksaveMod.Ghost.Recording;
using Celeste.Mod.QuicksaveMod.Hooks;
using Celeste.Mod.QuicksaveMod.Interop;
using Celeste.Mod.QuicksaveMod.Playback;
using Celeste.Mod.QuicksaveMod.Quicksave;
using Celeste.Mod.QuicksaveMod.Recording;
using Celeste.Mod.QuicksaveMod.UI;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModModule : EverestModule {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static QuicksaveModModule Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public override Type SettingsType => typeof(QuicksaveModSettings);
    public static QuicksaveModSettings Settings => (QuicksaveModSettings) Instance._Settings;

    public override Type SaveDataType => typeof(QuicksaveModSaveData);
    public static QuicksaveModSaveData SaveData => (QuicksaveModSaveData) Instance._SaveData;

    private QuicksaveBrowserHandler? quicksaveBrowserHandler;
    private GhostBrowserHandler? ghostBrowserHandler;
    private ModBrowserCoordinator? browserCoordinator;

    public QuicksaveModModule() {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel(QuicksaveConstants.LogTag, LogLevel.Verbose);
        Logger.SetLogLevel(GhostConstants.LogTag, LogLevel.Verbose);
#else
        Logger.SetLogLevel(QuicksaveConstants.LogTag, LogLevel.Info);
        Logger.SetLogLevel(GhostConstants.LogTag, LogLevel.Info);
#endif
    }

    public override void Initialize() {
        typeof(CelesteTasImports).ModInterop();
        typeof(QuicksaveModInterop).ModInterop();
        QuicksaveModInterop.InitExports();
    }

    public override void Load() {
        typeof(SpeedrunToolSaveLoadImports).ModInterop();

        LevelLoadHooks.Apply();
        BrowserInputHooks.Apply();
        RecordingHooks.Apply();
        PlaybackHooks.Apply();
        LoadFreezeHooks.Apply();
        SpeedrunToolSaveLoadImports.Apply();
        SpeedrunToolRaceTimer.WarmUp();
        GhostRecordingHooks.Apply();
        GhostPlaybackHooks.Apply();

        quicksaveBrowserHandler = new QuicksaveBrowserHandler();
        ghostBrowserHandler = new GhostBrowserHandler();
        browserCoordinator = new ModBrowserCoordinator(quicksaveBrowserHandler, ghostBrowserHandler);
        quicksaveBrowserHandler.SetCoordinator(browserCoordinator);

        if (!ImGuiManager.Handlers.OfType<QuicksaveBrowserHandler>().Any()) {
            ImGuiManager.Handlers.Add(quicksaveBrowserHandler);
        }

        if (!ImGuiManager.Handlers.OfType<GhostBrowserHandler>().Any()) {
            ImGuiManager.Handlers.Add(ghostBrowserHandler);
        }
    }

    public override void Unload() {
        if (quicksaveBrowserHandler != null) {
            quicksaveBrowserHandler.Close();
            ImGuiManager.Handlers.Remove(quicksaveBrowserHandler);
            QuicksaveBrowserHandler.ClearInstance();
            quicksaveBrowserHandler = null;
        }

        if (ghostBrowserHandler != null) {
            ghostBrowserHandler.Close();
            ImGuiManager.Handlers.Remove(ghostBrowserHandler);
            GhostBrowserHandler.ClearInstance();
            ghostBrowserHandler = null;
        }

        browserCoordinator = null;

        GhostPlaybackHooks.Unapply();
        GhostRecordingHooks.Unapply();
        LoadFreezeHooks.Unapply();
        PlaybackHooks.Unapply();
        RecordingHooks.Unapply();
        BrowserInputHooks.Unapply();
        SpeedrunToolSaveLoadImports.Unapply();
        LevelLoadHooks.Unapply();
    }
}
