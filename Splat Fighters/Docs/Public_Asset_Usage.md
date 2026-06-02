# Public Asset Usage

Last updated: 2026-06-02

## Current Status

Splat Fighters now includes a user-provided public 3D environment package for the MVP arena presentation pass.

The current prototype uses:

- Unity primitive meshes for expanded graybox level geometry, player placeholders, bot placeholders, objectives, elevated routes, cover, and route markers.
- Imported modular hangar meshes, prefabs, textures, and materials for visible arena dressing.
- Imported RPG Monster Wave PBR character prefabs, materials, and animations for selectable player and opponent visuals.
- Unity-generated materials created by editor setup scripts.
- Procedural ink splatter particle effects generated at runtime from original C# scripts.
- Original C# scripts written for this course project.
- Runtime UI built from Unity UI text and image components.

## Required Tracking For Future Public Assets

If a future increment imports public assets from the internet, add each asset to the table below before opening the pull request.

| Asset | Source URL | Author / Publisher | License | Imported Path | Project Use | Modifications |
| --- | --- | --- | --- | --- | --- | --- |
| Hangar Building Modular | User-provided public `.unitypackage`; original public source URL pending project owner confirmation | 3dfactorio, based on public third-party credit references; package metadata does not include a direct author field | Public-use license pending confirmation from original source | `Assets/Hangar Building Modular/Materials`, `Assets/Hangar Building Modular/Meshes`, `Assets/Hangar Building Modular/Prefabs`, `Assets/Hangar Building Modular/Textures` | Visible hangar-style arena shell, floor, wall dressing, crates, scaffolds, pallets, lamps, and industrial props in `MVP_ShootingTest` | Imported only gameplay-relevant Mesh, Material, Texture, and Prefab folders; skipped bundled demo scene, lightmaps, and reflection probes to reduce repository size and runtime cost; colliders and lights are removed from instantiated scene dressing so existing gameplay collision and paint blockers stay authoritative |
| RPG Monster Wave PBR | Imported public / Asset Store package already present in the project; original source URL pending project owner confirmation | Original package publisher pending confirmation from the source page | Package license pending confirmation from the original source | `Assets/RPG Monster Wave PBR`, `Assets/Resources/CharacterPrefabs`, `Assets/Resources/CharacterVisualCatalog.asset` | Animated selectable Team A player and Team B opponent visuals in the menu preview and gameplay arena | Uses lightweight mask-tint prefabs, team-color property blocks, runtime scale fitting, and the original package animation controllers; gameplay colliders remain separate from visual prefabs |

## Review Rules

- Use only assets whose license allows the intended course-project use.
- Keep source URLs specific enough for a reviewer to verify the original asset.
- Do not import Nintendo, Splatoon, or other copyrighted game assets.
- Prefer simple placeholder assets until the gameplay loop is stable.
- Keep each imported asset tied to a GitHub issue or pull request.
- Before final submission, replace any "pending confirmation" fields with the original asset page URL, author, and exact license text.
