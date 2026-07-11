namespace Celeste.Mod.QuicksaveMod.Recording;

internal enum TasSlot {
    None,
    A,
    B,
}

internal static class TwoSlotEncoder {
    internal static TasSlot Update(TasSlot current, bool pressed, bool check) {
        if (pressed) {
            return current == TasSlot.A ? TasSlot.B : TasSlot.A;
        }

        if (check) {
            return current == TasSlot.None ? TasSlot.A : current;
        }

        return TasSlot.None;
    }

    internal static char CharFor(TasSlot slot, char slotAChar, char slotBChar) =>
        slot switch {
            TasSlot.A => slotAChar,
            TasSlot.B => slotBChar,
            _ => ' ',
        };
}
