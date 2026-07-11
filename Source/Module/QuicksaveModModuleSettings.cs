using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.QuicksaveMod.Module;

public class QuicksaveModModuleSettings : EverestModuleSettings {
    [SettingName("Open Quicksave Browser")]
    [DefaultButtonBinding(buttons: [], keys: new[] { Keys.Q })]
    public ButtonBinding OpenBrowser { get; set; }
}
