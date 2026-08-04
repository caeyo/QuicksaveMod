using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuicksaveMod.Ghost;

internal sealed class GhostFinishData {
    public string Room { get; init; } = "";
    public int Revisit { get; init; } = 1;
    public Vector2 Position { get; init; }
    public long SessionTimeTicks { get; init; }
}
