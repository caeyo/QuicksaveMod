using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuicksaveMod.Ghost.Playback;

[Tracked(false)]
internal sealed class SpectateHintHud : Entity {
    private const int Padding = 25;
    private const float VisibleDuration = 2f;

    private readonly string text = "Spectating - use CelesteTAS binds for playback control";
    private float alpha;
    private float unEasedAlpha;

    public SpectateHintHud() {
        Tag = Tags.HUD | Tags.Global | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;
        Depth = -100;

        Vector2 messageSize = ActiveFont.Measure(text);
        Position = new Vector2(Padding, Engine.Height - messageSize.Y - Padding / 2f);
        Add(new Coroutine(Show()));
    }

    private IEnumerator Show() {
        while (alpha < 1f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
            alpha = Ease.SineOut(unEasedAlpha);
            yield return null;
        }

        yield return VisibleDuration;

        while (alpha > 0f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
            alpha = Ease.SineIn(unEasedAlpha);
            yield return null;
        }

        RemoveSelf();
    }

    public override void Render() {
        ActiveFont.DrawOutline(
            text,
            Position,
            Vector2.Zero,
            Vector2.One,
            Color.White * alpha,
            2f,
            Color.Black * alpha * alpha * alpha
        );
    }
}
