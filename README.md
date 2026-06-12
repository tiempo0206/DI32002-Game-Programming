# Game Programming Coursework Portfolio

This repository contains my DI32002 Game Programming coursework projects, in-class exercises, and development process notes. It is organised as a small Unity coursework portfolio: each Unity project lives in its own folder with its own `Assets`, `Packages`, and `ProjectSettings`, so the projects can be opened independently in Unity Hub.

## Main Project

### `Splat Fighters/`

**Splat Fighters** is the main coursework game in this repository. It is an original third-person ink-territory arena game made in Unity 2022, where the player controls a selectable monster character and competes against a rule-based AI opponent. The game includes a complete menu flow, mode selection, AI difficulty selection, character selection with character-specific ink colours, shooter and roller tools, swimming on friendly ink, enemy ink damage, special paint burst, Tower Control objective play, Turf War scoring, HUD feedback, audio feedback, training support, and a final match results screen.

The core gameplay focuses on ink-territory control: players are rewarded for controlling the arena through paint coverage, movement routes, and objective pressure rather than only defeating opponents.

The Unity project structure, codebase, runtime systems, menu and state flow, 3D arena, character selection, AI behaviour, paint grid, objective logic, UI presentation, audio workflow, asset documentation, and testing records were designed and implemented for this coursework project. The wider game concept and implementation are original coursework work.

More details are available in:

- `Splat Fighters/Docs/Game_Design_and_Code_Documentation_CN_EN.md`
- `Splat Fighters/Docs/Splat_Fighters_Midterm_Deep_Dive.md`
- `Splat Fighters/Docs/Public_Asset_Usage.md`
- `Splat Fighters/Docs/External_Asset_Package_References.md`
- `Splat Fighters/Docs/Audio_Asset_Notes.md`
- `Splat Fighters/Docs/Performance_Notes.md`
- `Splat Fighters/Docs/Final_Map_Playtest_And_Route_Tuning.md`

## Other Coursework Projects And Exercises

### `class activity/Solar System/`

A short Unity in-class assignment for an interactive solar system scene aimed at children. The scene includes the Sun, Earth, and Moon; selectable celestial objects; smooth camera focus; child-friendly information panels; orbit and rotation motion; materials; lighting; camera interaction; and audio.

More details are available in:

- `class activity/Solar System/README.md`

### `class activity/2D-shooter/`

A Unity 2D space-shooter exercise and improvement project used for systems practice and feature iteration during the course. It includes a playable 2D shooter scene, player movement, shooting, asteroids/enemies, game flow improvements, menu work, bounds handling, and basic polish.

### `class activity/Unity Event Driven/`

Course notes and small exercises about event-driven programming, Unity input actions, MonoBehaviour lifecycle behaviour, and simple persistence patterns.

### `prototype/`

Early prototype work and concept exploration used before the main Unity gameplay direction became stable.

## Unity Version

The Unity projects in this repository use:

```text
Unity Editor 2022.3.62f3c1
```

## How To Open A Project

1. Open Unity Hub.
2. Choose **Add project from disk**.
3. Select one Unity project folder, for example:
   - `Splat Fighters`
   - `class activity/Solar System`
   - `class activity/2D-shooter`
4. Open the entry scene listed in that project's README or project notes.
5. Press **Play**.

Do not open the repository root as a Unity project. Each Unity project should be opened from its own folder.

## Entry Scenes

| Project | Entry Scene |
|---|---|
| `Splat Fighters/` | `Assets/Scenes/MainMenu.unity` |
| `class activity/Solar System/` | `Assets/Scenes/SampleScene.unity` |
| `class activity/2D-shooter/` | `Assets/_Scenes/Level1_scene.unity` |

## Repository Structure

| Path | Purpose |
|---|---|
| `Splat Fighters/` | Main coursework game: third-person ink-territory arena game |
| `Splat Fighters/Assets/Scenes/` | Main menu, gameplay, and training scenes |
| `Splat Fighters/Assets/Scripts/` | Runtime gameplay scripts for managers, painting, player, AI, weapons, UI, visuals, audio, and level logic |
| `Splat Fighters/Assets/Editor/` | Unity Editor-only development tools for generating or repairing saved scenes and prefabs |
| `Splat Fighters/Assets/Prefabs/` | Reusable actor, map, weapon, and audio prefabs |
| `Splat Fighters/Docs/` | Game design, technical notes, asset references, performance notes, feedback records, and planning documents |
| `class activity/Solar System/` | Child-friendly interactive solar system assignment |
| `class activity/2D-shooter/` | 2D space-shooter practice and improvement project |
| `class activity/Unity Event Driven/` | Course notes and topic write-ups |
| `prototype/` | Early prototype and concept work |

## Main Project Technical Summary

The main Splat Fighters gameplay is built around a real-time grid-based paint system:

- `PaintManager` is the global entry point for painting operations.
- `PaintableArea` stores paint ownership in grid cells.
- `GameManager` controls match state, timer, scoring, mode logic, pause/restart, respawn, and results.
- `PlayerController` handles third-person movement, jumping, swimming, and player interaction.
- `InkWeapon`, `InkProjectile`, `RollerPaintTool`, and `SpecialPaintBurst` create paint requests.
- `BotController` implements a rule-based AI opponent with patrol, aiming, firing, retreat, and difficulty settings.
- `TowerObjective` reads local paint ownership around the tower to drive Tower Control movement and win logic.
- `ScoreUI`, `MainMenuController`, and `MatchResultsUI` provide menu, HUD, and results presentation.
- `CharacterVisualCatalog`, `CharacterSelectionManager`, and `CharacterVisualController` connect saved character selections to 3D models, animation states, and character-specific ink colours.

Runtime gameplay data such as paint ownership, coverage score, tower control, AI targeting, ink recovery, health damage, special charge, HUD values, and match results are calculated live during play. The map layout, spawn points, tower route, bot waypoints, prefab references, and default tuning values are authored and saved in Unity scenes or prefabs.

## Development And Assessment Evidence

This repository is used to show the development process behind the coursework: concept planning, implementation, testing, debugging, iteration, documentation, asset provenance, and final presentation preparation.

The main project records evidence for:

- Game concept and design goals
- Unity project structure and prefab architecture
- Grid-based painting and coverage scoring
- Input, camera, player movement, swimming, weapons, roller, and special ability systems
- Rule-based AI behaviour and difficulty tuning
- Turf War and Tower Control mode logic
- UI, HUD, audio, menu, training, and match results flow
- Imported public assets, copyright notes, and source references
- Testing, debugging, performance optimisation, and presentation preparation
- GitHub Issues, Pull Requests, and Kanban-based Agile tracking

## Asset And Copyright Notes

Splat Fighters uses original gameplay code and project-created audio, plus documented public or Asset Store visual assets for environment and character presentation. It does not use copyrighted commercial game assets.

Important documentation:

- `Splat Fighters/Docs/Public_Asset_Usage.md`
- `Splat Fighters/Docs/External_Asset_Package_References.md`
- `Splat Fighters/Docs/Audio_Asset_Notes.md`

## Version Control Notes

Generated Unity folders and local build output are intentionally excluded from version control, including:

```text
Library/
Logs/
Temp/
Builds/
UserSettings/
```

Unity can recreate these folders when a project is opened. The repository keeps the source files, scripts, scenes, prefabs, project settings, resource records, documentation, and testing evidence needed for review and continued development.
