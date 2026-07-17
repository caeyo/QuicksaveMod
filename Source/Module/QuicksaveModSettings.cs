using Celeste.Mod.QuicksaveMod.Playback;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModSettings : EverestModuleSettings {
    [SettingName("modoptions_quicksavemod_openbrowser")]
    [DefaultButtonBinding(buttons: [], keys: [Keys.Q])]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ButtonBinding OpenBrowser { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
