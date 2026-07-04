# Guardian run animation — Unity

## Deliverables

- `guardian_run_8dir_6f_256.png`: final Unity spritesheet
- `frames/guardian_run_<direction>_<frame>.png`: 48 individual RGBA frames
- `source/guardian_run_<direction>_source.png`: cleaned high-resolution directional sources

## Spritesheet layout

- Cell size: `256x256`
- Sheet size: `1536x2048`
- Columns: frames `00` through `05`
- Rows: `N`, `NE`, `E`, `SE`, `S`, `SW`, `W`, `NW`
- Character feet baseline: `y = 248`
- Recommended normalized pivot: `(0.5, 0.03125)`

## Unity import settings

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Pixels Per Unit: `128`
- Mesh Type: `Full Rect`
- Filter Mode: `Bilinear`
- Wrap Mode: `Clamp`
- Compression: `None` while developing
- Slice: `Grid By Cell Size`, `256x256`
- Animation sample rate: start at `10 FPS`

Apply the same bottom-center custom pivot to every sliced sprite.
