# Visual Style

## Direction

The project UI and generated UI-kit sprite work uses a gold/blue steampunk art deco direction:

- polished brass, antique gold, champagne highlights, deep navy, cobalt blue glass, desaturated steel, dark ink shadows;
- stepped art deco geometry, sunburst arcs, fan corners, bevels, thin inlaid lines, rivets, gauges, vents, and mechanical filigree;
- readable 2D game silhouettes first, ornamental detail second;
- high contrast between interactable foreground edges and darker background plates.

Use this style as the palette and motif reference for UI frames, panels, buttons, icons, ornaments, separators, damage feedback, and supporting chrome.

## Generated Sprite Rules

Generated sprites must not contain baked text, letters, numbers, labels, glyph words, fake UI copy, or placeholder captions. Text is authored in Unity through TMP/localization, not inside image pixels.

UI-kit sprite requests must be decomposed before generation. Do not ask for a complete finished window, button with text, or flattened HUD panel when the intended asset is reusable. Generate separate sprite pieces that can be composed in Unity:

- background plates: base fills, blue glass/metal backing, subtle texture, broad shadow shapes;
- mid-plane frames: brass/gold borders, bevel bands, panel rims, button rims, tab rims;
- foreground highlights: thin bright edge catches, shine strips, inner glow slivers, hover/selected overlays;
- ornaments: art deco corners, sunburst caps, brass brackets, rivets, gears, vents, separators;
- icons: textless pictograms only, built on the same gold/blue material language;
- damage accents: cracks, scorch marks, broken brass edges, red/amber warning glows, sparking overlays.

Scalable frames, buttons, panels, tabs, slots, and plate sprites must be authored as 9-slice-ready assets. Keep corners and ornamental caps outside the stretch center, keep edges tileable or stretch-safe, and leave the center clean enough for Unity text and dynamic content.

## UI Kit Production Output

Use production-oriented folders so sprite agents and Unity authors can import only the needed layer family:

- `Assets/Sprites/UI/UIKit/Plates/`
- `Assets/Sprites/UI/UIKit/Frames/`
- `Assets/Sprites/UI/UIKit/Highlights/`
- `Assets/Sprites/UI/UIKit/Ornaments/`
- `Assets/Sprites/UI/UIKit/Icons/`
- `Assets/Sprites/UI/UIKit/DamageAccents/`
- `Assets/Sprites/UI/UIKit/Source/` for layered source files, prompts, contact sheets, or non-imported reference exports when explicitly requested.

Name generated UI sprites with the project `Type Domain Tags` convention from `ProjectStructure.md`. Use `Sprite UI Kit <LayerFamily> <Role> <Variant>` for reusable UI-kit PNGs.

Examples:

- `Sprite UI Kit Plate Background Navy Brass.png`
- `Sprite UI Kit Frame Button Gold Beveled.png`
- `Sprite UI Kit Highlight Selected Cobalt Edge.png`
- `Sprite UI Kit Ornament Corner Sunburst Brass.png`
- `Sprite UI Kit Icon Inventory Gear.png`
- `Sprite UI Kit DamageAccent Cracked Brass Warning.png`

Each sprite-agent output batch should include the intended layer family, target folder, size, transparent-background expectation, 9-slice border guidance where applicable, and whether the asset is a base, frame, overlay, ornament, icon, or damage accent.
