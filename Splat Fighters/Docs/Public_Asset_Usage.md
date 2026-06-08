# Public Asset Usage

Last updated: 2026-06-07

## Current Status

Splat Fighters now includes a user-provided public 3D environment package for the MVP arena presentation pass.

For a dedicated package reference list, see `External_Asset_Package_References.md`.

The current prototype uses:

- Unity primitive meshes for expanded graybox level geometry, player placeholders, bot placeholders, objectives, elevated routes, cover, and route markers.
- Imported modular hangar meshes, prefabs, textures, and materials for visible arena dressing.
- Imported RPG Monster Wave PBR character prefabs, materials, and animations for selectable player and opponent visuals.
- Unity-generated materials created by editor setup scripts.
- Procedural ink splatter particle effects generated at runtime from original C# scripts.
- Project-original procedural music and sound effects created for this course project under `Assets/Resources/Audio`; see `Audio_Asset_Notes.md`.
- Original C# scripts written for this course project.
- Runtime UI built from Unity UI text and image components.

## Required Tracking For Future Public Assets

If a future increment imports public assets from the internet, add each asset to the table below before opening the pull request.

| Asset | Source URL | Author / Publisher | License | Imported Path | Project Use | Modifications |
| --- | --- | --- | --- | --- | --- | --- |
| Hangar Building Modular | Verified public reference URL: `https://www.mobygames.com/game/190023/midnight-fight-express/credits/windows/`; verified Unity publisher profile URL for 3dfactorio: `https://assetstore.unity.com/publishers/41464` | 3dfactorio | Exact package listing and license page are not included in the local package; do not cite an unverified direct store URL unless it is recovered from the original download source | `Assets/Hangar Building Modular/Materials`, `Assets/Hangar Building Modular/Meshes`, `Assets/Hangar Building Modular/Prefabs`, `Assets/Hangar Building Modular/Textures` | Visible hangar-style arena shell, floor, wall dressing, crates, scaffolds, pallets, lamps, and industrial props in `MVP_ShootingTest` | Imported only gameplay-relevant Mesh, Material, Texture, and Prefab folders; skipped bundled demo scene, lightmaps, and reflection probes to reduce repository size and runtime cost; colliders and lights are removed from instantiated scene dressing so existing gameplay collision and paint blockers stay authoritative |
| RPG Monster Wave PBR | Unity Asset Store URL: `https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-wave-pbr-158727` | DM Dungeon Mason | Standard Unity Asset Store EULA | `Assets/RPG Monster Wave PBR`, `Assets/Resources/CharacterPrefabs`, `Assets/Resources/CharacterVisualCatalog.asset` | Animated selectable Team A player and Team B opponent visuals in the menu preview and gameplay arena | Uses lightweight mask-tint prefabs, character-specific ink-color property blocks, runtime scale fitting, and the original package animation controllers; gameplay colliders remain separate from visual prefabs |

## Review Rules

- Use only assets whose license allows the intended course-project use.
- Keep source URLs specific enough for a reviewer to verify the original asset.
- Do not import Nintendo, Splatoon, or other copyrighted game assets.
- Prefer simple placeholder assets until the gameplay loop is stable.
- Keep each imported asset tied to a GitHub issue or pull request.
- Before final submission, replace any "pending confirmation" fields with the original asset page URL, author, and exact license text.
