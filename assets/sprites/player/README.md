# Player class sprites

Original 2.5D dark-fantasy prototype sprites generated for this project.

## Available idle sets

- `guardian/idle`
- `arcanist/idle`
- `huntress/idle`
- `dark_knight/idle` (`Knight` class)
- `wizard/idle`
- `elf/idle`

Each set contains:

- One high-resolution transparent turnaround: `<class>_idle_8dir.png`
- Eight individual transparent `128x128` frames
- One packed `512x256` sheet: `<class>_idle_8dir_128.png`

Packed sheet order:

- Row 1: `N`, `NE`, `E`, `SE`
- Row 2: `S`, `SW`, `W`, `NW`

All frames use a transparent RGBA background and align the character's feet at `y = 124`.

These are static `idle` direction frames. Walk, attack, dodge, hit, and death animations still need separate production and review.

## Unity animations

- `guardian/run`: six-frame run cycle in eight directions, exported at `256x256` per frame.
