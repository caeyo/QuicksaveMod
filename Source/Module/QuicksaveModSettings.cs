using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.QuicksaveMod.Module;

public enum PlaybackSpeed {
    Speed1 = 1,
    Speed2 = 2,
    Speed3 = 3,
    Speed4 = 4,
    Speed5 = 5,
    Speed10 = 10,
    Speed20 = 20,
    Speed30 = 30,
    Speed40 = 40,
    Speed50 = 50,
    Speed100 = 100,
    Speed150 = 150,
    Speed200 = 200,
    Speed250 = 250,
    Speed300 = 300,
    Speed350 = 350,
    Max = 10_000,
}

public class QuicksaveModSettings : EverestModuleSettings {
    [SettingName("modoptions_quicksavemod_openbrowser")]
    [DefaultButtonBinding(buttons: [], keys: new[] { Keys.Q })]
    public ButtonBinding OpenBrowser { get; set; }

    [SettingName("modoptions_quicksavemod_playbackspeed")]
    public PlaybackSpeed PlaybackSpeed { get; set; } = PlaybackSpeed.Max;

    [SettingName("modoptions_quicksavemod_savestateonquicksaveload")]
    public bool SavestateOnQuicksaveLoad { get; set; } = true;

    public void CreateSavestateOnQuicksaveLoadEntry(TextMenu menu, bool inGame) {
        if (!Interop.SpeedrunToolBridge.IsLoaded) {
            return;
        }

        menu.Add(
            new TextMenu.OnOff(
                Dialog.Clean("modoptions_quicksavemod_savestateonquicksaveload"),
                SavestateOnQuicksaveLoad
            ).Change(value => SavestateOnQuicksaveLoad = value)
        );
    }
}
