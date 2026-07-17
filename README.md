# Quicksave Mod

A Celeste mod that adds quicksave functionality.

A **quicksave** is a snapshot of the game's state that is saved to a file so that it can be loaded from anywhere, at any
time - from a different level, from the main menu, after the game has been closed and reopened, and even on someone
else's computer.

## How it works

Truly "saving" Celeste's game state to a file is largely infeasible. Instead, this mod heavily relies
upon [CelesteTAS](https://github.com/EverestAPI/CelesteTAS-EverestInterop) to replay a recording of the player's inputs
from a deterministic start point in order to recreate the same conditions under which the quicksave was made -
maintaining the player's and other objects' positions, speeds, state, etc. Anything persisted in the session data is
also saved and restored.

## How to use

Press the "Open Quicksave Browser" button or `Q` by default (this is rebindable) to open the quicksave browser. This
window is where you view your quicksaves, create new ones and load them. You can also create folders to better organise
your quicksaves.

To create a new quicksave, open the browser to pause the game at the moment you want to save. Right click the window to
open the context menu, click "Save", and name the file.

To load a quicksave, you can right click and click "Load", double click it, or hit Enter while it's selected.

Quicksave `.qs` files are stored in the Celeste game directory under the `Quicksaves` folder.

## Notes

This mod does not replace savestates from [SpeedrunTool](https://github.com/DemoJameson/Celeste.SpeedrunTool) - they are
by design significantly faster to load than quicksaves and should be preferred for repetitive loading. If SpeedrunTool
is enabled, Quicksave Mod will attempt to create a SpeedrunTool savestate when a quicksave finishes loading so that it
can be easily reloaded. This behaviour can be disabled in settings.

The replaying of inputs by CelesteTAS is, by default, set to max speed. Sometimes this can cause the inputs to desync
and the saved state to not be restored correctly, so the Quicksave Mod settings has a slider for how fast the quicksave
loading speed (i.e. replay speed) should be.
