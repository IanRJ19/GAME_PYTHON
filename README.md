# PixelDepth Prototype

Base architecture for a 2D game in Godot 4 with:
- `GameEvents` autoload as signal bus.
- `GameState` autoload for runtime references and pause state.
- `SceneRouter` autoload for scene transitions.
- `Player` with smooth movement, coyote time, jump buffer, and melee attack.
- `DummyEnemy` with `HealthComponent` + `Hurtbox` for damage tests.

## Controls
- Move: `A/D` or arrow keys.
- Jump: `Space`, `W`, or up arrow.
- Attack: `J`, `K`, or `F`.
- Skills: `Q`, `E`, `R`.
- Character/Inventory panel: `C` or `I`.
- Pause: `Esc`.

## Art assets
- Integrated free CC0 sprites/tiles from OpenGameArt.
- Credits and links: `assets/ASSET_CREDITS.md`.

## Next immediate decisions
1. Keep this as a platformer baseline or pivot to tile/chunk world now.
2. Add combat first or inventory/mining first.
3. Keep pure GDScript or start hybrid C# modules later.
