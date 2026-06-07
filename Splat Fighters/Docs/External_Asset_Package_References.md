# External Asset Package References

Last updated: 2026-06-07

## Purpose

This document lists the external asset packages currently present in the Splat Fighters Unity project. It is intended for course review, attribution checks, and final submission preparation.

The project does not use Nintendo, Splatoon, or other copyrighted commercial game assets. External assets are used only as presentation, character, or plugin support around the original Splat Fighters gameplay code.

Project-original generated audio is documented separately in `Generated_Audio_Notes.md` because it is not an external asset package.

## Summary Table

| Package | Type | Source / Reference | Publisher / Author | License Status | Imported Project Path | Current Project Use |
| --- | --- | --- | --- | --- | --- | --- |
| Hangar Building Modular | 3D environment asset package | Verified public reference URL: `https://www.mobygames.com/game/190023/midnight-fight-express/credits/windows/`; verified Unity publisher profile URL for 3dfactorio: `https://assetstore.unity.com/publishers/41464` | 3dfactorio | The exact package listing and license page are not included in the local package. Do not cite an unverified direct store URL unless it is recovered from the original download source. | `Assets/Hangar Building Modular/Materials`, `Assets/Hangar Building Modular/Meshes`, `Assets/Hangar Building Modular/Prefabs`, `Assets/Hangar Building Modular/Textures` | Visible hangar arena dressing, floor, walls, crates, scaffolds, pallets, lamps, and industrial props in `MVP_ShootingTest.unity` |
| RPG Monster Wave PBR | 3D character asset package | Unity Asset Store URL: `https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-wave-pbr-158727` | DM Dungeon Mason | Standard Unity Asset Store EULA; package metadata marks imported assets as Store license type | `Assets/RPG Monster Wave PBR`, `Assets/Resources/CharacterPrefabs`, `Assets/Resources/CharacterVisualCatalog.asset` | Animated selectable Team A player and Team B opponent visuals, character preview models, and character-specific ink color mapping |
| Zibra Liquid | Third-party Unity plugin / VFX support package | Unity Asset Store: `https://assetstore.unity.com/packages/tools/physics/zibra-liquid-266451` | Zibra AI | Standard Unity Asset Store EULA; local open-source notices are stored in `Assets/Plugins/Zibra/Liquids/NativePlugin/OpenSourceNotices.txt` | `Assets/Plugins/Zibra` | Present as a third-party plugin dependency. It is not the main game logic and should remain disabled or lightweight on MacBook Air performance profiles unless explicitly needed for a later VFX pass |

## Detailed Notes

### Hangar Building Modular

Imported paths:

- `Assets/Hangar Building Modular/Materials`
- `Assets/Hangar Building Modular/Meshes`
- `Assets/Hangar Building Modular/Prefabs`
- `Assets/Hangar Building Modular/Textures`

Use in Splat Fighters:

- Provides the visible hangar-style map dressing for the MVP arena.
- Replaces the early untextured graybox look with industrial floor, wall, crate, scaffold, pallet, lamp, trolley, and generator meshes.
- The gameplay collision remains controlled by original Splat Fighters graybox blockers and invisible containment colliders.

Modifications:

- Only gameplay-relevant folders are kept in the repository.
- Bundled demo scenes, lightmaps, and reflection probes were skipped or removed to reduce repository size and runtime cost.
- Scene-instantiated hangar props have package colliders, rigidbodies, and lights removed by editor setup code so that imported visuals do not override gameplay collision or paint blockers.
- Some materials were normalized to URP Lit to fix pink material rendering in the project render pipeline.

Reference status:

- The package was provided as a `.unitypackage` by the project owner.
- The current repository does not include a package readme, license text, or official package-store URL for this asset.
- A public MobyGames credits page lists `Hangar Building Modular` and credits `3dfactorio`.
- Unity's public publisher profile confirms `3dfactorio` as an Asset Store publisher.
- The current project does not have a recovered official direct package listing URL for `Hangar Building Modular`; this document intentionally records only real, verified URLs instead of guessing a store URL.
- Because the original package listing URL is not available from the local package, the final submission should either include the original package page if recovered or explicitly cite the verified public reference URLs above.

### RPG Monster Wave PBR

Imported paths:

- `Assets/RPG Monster Wave PBR`
- `Assets/Resources/CharacterPrefabs`
- `Assets/Resources/CharacterVisualCatalog.asset`

Use in Splat Fighters:

- Provides animated 3D character prefabs for player and opponent selection.
- Used by `CharacterVisualCatalog`, `CharacterPreviewPresenter`, `CharacterSelectionManager`, and `CharacterVisualController`.
- Each selected character is mapped to a distinct ink color for Team A or Team B.

Modifications:

- Runtime code fits the imported visuals to the gameplay `CharacterController`.
- Prototype cylinder renderers are hidden when imported character visuals are active.
- Materials are tinted through property blocks so the same character package can support multiple ink-color identities.
- Gameplay colliders remain separate from imported visual prefabs.

Reference:

- Official Unity Asset Store page: `https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-wave-pbr-158727`
- Publisher: DM Dungeon Mason
- License agreement shown on the Asset Store: Standard Unity Asset Store EULA

### Zibra Liquid

Imported path:

- `Assets/Plugins/Zibra`

Use in Splat Fighters:

- Present as a third-party plugin dependency for optional liquid / VFX experimentation.
- Current performance direction keeps heavy liquid or ink splatter effects disabled or deferred because the MacBook Air development machine showed frame-rate issues with expensive visual effects.

Reference:

- Official Unity Asset Store page: `https://assetstore.unity.com/packages/tools/physics/zibra-liquid-266451`
- Publisher: Zibra AI
- License agreement shown on the Asset Store: Standard Unity Asset Store EULA
- Local notices: `Assets/Plugins/Zibra/Liquids/NativePlugin/OpenSourceNotices.txt`

## Final Submission Checklist

- [ ] Recover the original direct package listing URL for `Hangar Building Modular` if possible.
- [ ] If the direct package listing cannot be recovered, cite the public MobyGames reference URL and the 3dfactorio Unity publisher profile URL.
- [ ] Confirm the exact license text for `Hangar Building Modular` before final submission.
- [ ] Keep `RPG Monster Wave PBR` attribution tied to the Unity Asset Store page and publisher.
- [ ] Keep `Zibra Liquid` attribution tied to the Unity Asset Store page and local open-source notices.
- [ ] Do not add Nintendo, Splatoon, or other copyrighted commercial game assets.
- [ ] Document any future imported public audio, music, texture, model, or VFX package in this file before final submission.
