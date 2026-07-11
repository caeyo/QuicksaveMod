using Celeste.Mod.QuicksaveMod.Interop;
using Monocle;
using TAS;

namespace Celeste.Mod.QuicksaveMod.Playback;

public static class QuicksavePlayback {
    private static string? tempTasPath;
    private static bool monitorPlayback;

    public static bool IsActive => monitorPlayback;

    public static void Apply() {
        On.Monocle.Engine.Update += MonitorPlayback;
    }

    public static void Unapply() {
        On.Monocle.Engine.Update -= MonitorPlayback;
        Cancel();
    }

    public static void Start(string tasFilePath) {
        tempTasPath = tasFilePath;
        monitorPlayback = true;

        if (CelesteTasImports.IsTasActive?.Invoke() == true) {
            Manager.DisableRun();
        }

        Manager.AddMainThreadAction(() => {
            Manager.Controller.FilePath = tasFilePath;
            Manager.EnableRun();
        });
    }

    public static void Cancel() {
        monitorPlayback = false;
        CleanupTempFile();
    }

    private static void MonitorPlayback(On.Monocle.Engine.orig_Update orig, Engine engine, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(engine, gameTime);

        if (!monitorPlayback) {
            return;
        }

        if (!Manager.Running) {
            FinishPlayback();
            return;
        }

        if (Manager.CurrState == Manager.State.Paused && !Manager.Controller.CanPlayback) {
            FinishPlayback();
        }
    }

    private static void FinishPlayback() {
        monitorPlayback = false;
        CleanupTempFile();
    }

    private static void CleanupTempFile() {
        if (tempTasPath == null) {
            return;
        }

        try {
            if (File.Exists(tempTasPath)) {
                File.Delete(tempTasPath);
            }
        } catch (Exception e) {
            Logger.Warn(nameof(QuicksavePlayback), $"Failed to delete temp TAS file: {e.Message}");
        }

        tempTasPath = null;
    }
}
