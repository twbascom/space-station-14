# Fusion Reactor RSI Blank Canvas (Simplified)

This directory contains a simplified 3-sprite blank canvas for the Fusion Reactor.

## Sprites & States

Exactly 3 sprites are defined as 32x32 transparent PNGs with a semi-transparent light gray border/grid (so they are visible in-game and in editors):

1. `control.png`: The controller console visual (idle, active, warning, and critical visuals are all mapped here).
2. `shield.png`: The shielding/hull block visual (a single sprite used for all connections, removing the need for 16 separate auto-tiling sprites).
3. `core.png`: The core visual shown when shielding is converted into an active reactor core.

## Entity Prototypes

The following prototypes have been registered in [fusionreactor.yml](file:///C:/space-station-14/Resources/Prototypes/Entities/Structures/Power/Generation/fusionreactor.yml):

* `FusionReactorController` - Controller for the fusion reactor
* `FusionReactorControllerUnanchored` - Unanchored version of the controller
* `FusionReactorShielding` - Modular shielding/hull block (without auto-tiling smoothing, using `shield.png` for all sides, and mapping core glowing states to `core.png`)
