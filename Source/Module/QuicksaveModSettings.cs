using Celeste.Mod.QuicksaveMod.Playback;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.QuicksaveMod.Module;

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
