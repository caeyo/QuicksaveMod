using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

internal static class SpectateHintHud {
    private const int Padding = 25;
    private const float VisibleDuration = 2f;

    private const string Text = "Use CelesteTAS binds for playback control - press Resume to start";

    private static bool active;
    private static float fade;
    private static float timeout;

    public static void Show() {
        active = true;
        fade = 0f;
        timeout = VisibleDuration;
    }

    public static void Hide() {
        active = false;
        fade = 0f;
        timeout = 0f;
    }

    public static void OnPostRender() {
        if (!active || Engine.Scene is not Level) {
            return;
        }

        if (ActiveFont.Font == null) {
            return;
        }

        float deltaTime = Engine.RawDeltaTime;
        float target = timeout > 0f ? 1f : 0f;
        fade = Calc.Approach(fade, target, deltaTime * 5f);

        Draw.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.Default,
            RasterizerState.CullNone,
            null,
            Engine.ScreenMatrix
        );

        Vector2 messageSize = ActiveFont.Measure(Text);
        float y = Engine.Height - messageSize.Y - Padding / 2f;
        float alpha = timeout > 0f ? Ease.SineIn(fade) : Ease.SineOut(fade);

        ActiveFont.DrawOutline(
            Text,
            new Vector2(Padding, y),
            Vector2.Zero,
            Vector2.One,
            Color.White * alpha,
            2f,
            Color.Black * (alpha * alpha * alpha)
        );

        Draw.SpriteBatch.End();

        if (fade >= 1f) {
            timeout -= deltaTime;
        }

        if (timeout <= 0f && fade <= 0f) {
            active = false;
        }
    }
}
