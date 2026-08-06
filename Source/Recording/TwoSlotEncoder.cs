namespace Celeste.Mod.QuickTools.Recording;

internal enum TasPressSlot {
    None,
    A,
    B,
}

internal static class TwoSlotEncoder {
    internal static TasPressSlot Update(TasPressSlot current, bool pressed, bool check) {
        if (pressed) {
            return current == TasPressSlot.A ? TasPressSlot.B : TasPressSlot.A;
        }

        if (check) {
            return current == TasPressSlot.None ? TasPressSlot.A : current;
        }

        return TasPressSlot.None;
    }

    internal static char CharFor(TasPressSlot slot, char slotAChar, char slotBChar) =>
        slot switch {
            TasPressSlot.A => slotAChar,
            TasPressSlot.B => slotBChar,
            _ => ' ',
        };
}
