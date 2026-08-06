# QuickTools

A Celeste mod that adds some tools to help you go quick.

## Quicksaves

A **quicksave** is a snapshot of the game's state that is saved to a file so that it can be loaded from anywhere, at any
time - from a different level, from the main menu, after the game has been closed and reopened, and even on someone
else's computer.

### How it works

Truly "saving" Celeste's game state to a file is largely infeasible. Instead, this mod heavily relies
upon [CelesteTAS](https://github.com/EverestAPI/CelesteTAS-EverestInterop) to replay a recording of the player's inputs
from a deterministic start point in order to recreate the same conditions under which the quicksave was made -
maintaining the player's and other objects' positions, speeds, state, etc. Anything persisted in the session data is
also saved and restored.

### How to use

Press the "Open Browsers" button or `Q` by default (this is rebindable) to open the quicksave browser. This
window is where you view your quicksaves, create new ones and load them. You can also create folders to better organise
your quicksaves.

To create a new quicksave, open the browser to pause the game at the moment you want to save. Right click the window to
open the context menu, click "Save", and name the file.

To load a quicksave, you can right click and click "Load", double click it, or hit Enter while it's selected.

Quicksave `.qs` files are stored in the Celeste game directory under the `Quicksaves` folder.

### Notes

Quicksaves do not replace savestates from [SpeedrunTool](https://github.com/DemoJameson/Celeste.SpeedrunTool) - they are
by design significantly faster to load than quicksaves and should be preferred for repetitive loading. If SpeedrunTool
is enabled, QuickTools will attempt to create a SpeedrunTool savestate when a quicksave finishes loading so that it
can be easily reloaded. This behaviour can be disabled in settings.

The replaying of inputs by CelesteTAS is, by default, set to max speed. Sometimes this can cause the inputs to desync
and the saved state to not be restored correctly, so the QuickTools settings has a slider for how fast the quicksave
loading speed (i.e. replay speed) should be.

## Ghosts

A **ghost** is a recording of your gameplay that you can play back in-game (via CelesteTAS) or race against. Similar
to quicksaves, ghosts are saved to a file that you can keep for later use or share with others.

### How to use

Press the "Open Browsers" button or `Q` by default (this is rebindable) to open the ghost browser. This
window is where you view your ghosts, create new ones and race/spectate them. You can also create folders to better organise
your ghosts.

To create a new ghost, you first must load a quicksave - this sets a consistent start point for the ghost playback. Play the
game until you're satisfied with the ghost recording. Then, open the browser to pause the game at that finish point.
Right click the window to open the context menu, click "Save from last Load", and name the file.

To race a ghost, you can right click and click "Race" or double click it. To spectate, right click and select "Spectate".

Ghost `.ghost` files are stored in the Celeste game directory under the `Ghosts` folder.
