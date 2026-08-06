using Celeste.Mod.ImGuiHelper;
using Celeste.Mod.QuickTools.Ghost;
using Celeste.Mod.QuickTools.Hooks;
using Celeste.Mod.QuickTools.Interop;
using Celeste.Mod.QuickTools.Quicksave;
using Celeste.Mod.QuickTools.UI;
using MonoMod.ModInterop;

namespace Celeste.Mod.QuickTools.Module;

public class QuickToolsModule : EverestModule {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static QuickToolsModule Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public override Type SettingsType => typeof(QuickToolsSettings);
    public static QuickToolsSettings Settings => (QuickToolsSettings) Instance._Settings;

    public override Type SaveDataType => typeof(QuickToolsSaveData);
    public static QuickToolsSaveData SaveData => (QuickToolsSaveData) Instance._SaveData;

    private QuicksaveBrowserHandler? quicksaveBrowserHandler;
    private GhostBrowserHandler? ghostBrowserHandler;
    private ModBrowserCoordinator? browserCoordinator;

    public QuickToolsModule() {
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
        typeof(QuickToolsInterop).ModInterop();
        QuickToolsInterop.InitExports();
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
