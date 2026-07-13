using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

public static class QuicksavePlayback {
    public static void Start(string tasFilePath) {
        string fullPath = Path.GetFullPath(tasFilePath);

        Manager.AddMainThreadAction(() => {
            if (Manager.Running) {
                Manager.DisableRun();
            }

            // RefreshInputs clears parsed inputs when NextState is Disabled.
            Manager.NextState = Manager.State.Running;
            Manager.Controller.FilePath = fullPath;
        });
    }
}
