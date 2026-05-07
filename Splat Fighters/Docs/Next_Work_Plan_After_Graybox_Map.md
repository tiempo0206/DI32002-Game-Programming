# Next Work Plan After Graybox Map V1

Date: 2026-05-07

## Current Increment

User story:

As a course project developer, I want a compact graybox arena so the player can test movement, shooting, painting, and future bot behavior in a readable 3D space.

Acceptance criteria for the current map increment:

- `MVP_ShootingTest` contains a `LevelRoot` hierarchy.
- The arena has boundary walls that keep the player inside the play area.
- The arena has readable lanes, center contest cover, side platforms, and spawn markers.
- TeamA and TeamB spawn points are placed on opposite sides.
- The map still uses the existing rectangular `PaintableArea` as the main scoring surface.
- The MVP movement, aim, shooting, painting, timer, and score loop should remain playable.

## Manual Playtest Checklist

Run these checks in `Assets/Scenes/MVP_ShootingTest.unity`.

1. Reload the scene after Unity detects external file changes.
2. Confirm the hierarchy contains `LevelRoot`.
3. Press Play.
4. Move from TeamA spawn to the center contest area.
5. Try to leave the arena through all four edges.
6. Aim and paint near center cover, side lanes, and spawn-side cover.
7. Move onto both side platform areas and confirm the camera remains usable.
8. Confirm the reticle still paints the intended ground location.
9. Confirm the score HUD updates after painting.
10. Confirm no new console errors appear.

## Recommended Next Agile Increments

### 1. PaintableArea Map Integration

Status:

Implemented in the current branch.

Goal:

Decide whether the full rectangular ground remains one scoring area or whether the arena should be split into multiple paintable scoring rectangles.

Implemented work:

- The MVP keeps one large rectangular scoring area for easy course-project explanation.
- `PaintBlocker` marks cover, walls, ramps, and platforms as non-score geometry.
- `PaintableArea` rebuilds a paintable-cell mask from those blockers at runtime.
- Paint hits must be close to the ground plane before they can change territory.
- `PaintManager` exposes paintability and team-owner queries for future movement benefits and bot logic.

Acceptance criteria:

- Main arena ground remains paintable.
- Cover, boundaries, ramps, and platforms do not count as paintable territory.
- Shots that hit obstacle surfaces do not paint hidden ground cells.
- Shots near platform and cover edges still paint nearby valid ground cells.
- Score changes remain easy to understand.
- Obstacles do not need wall painting yet.

### 2. TeamB Bot V1

Status:

Implemented in the current branch.

Goal:

Add the first opponent pressure source.

Implemented work:

- Added a simple TeamB bot scene object.
- Added deterministic waypoint movement with `CharacterController`.
- Added fixed paint targets so the bot paints readable contested ground.
- The bot calls `InkWeapon.SetAimTarget` and `InkWeapon.TryFire`.
- The bot uses TeamB weapon ownership and TeamB projectile coloring.
- Avoid NavMesh until the arena has been playtested with the current `CharacterController` and blockout geometry.
- Use the existing `PaintManager.TryGetTeamAtWorldPosition` query later for territory-aware movement.

Acceptance criteria:

- Bot belongs to TeamB.
- Bot moves through the arena.
- Bot paints TeamB territory.
- Score UI shows both teams changing territory.

### 3. Team Visual Readability

Status:

Implemented in the current branch.

Goal:

Make TeamA and TeamB easier to read during playtests.

Implemented work:

- Added a shared `TeamVisualPalette` for TeamA and TeamB labels and colors.
- Added `TeamVisualBinder` so character renderers can use the same team palette.
- Added team material assets for the TeamA player, TeamA projectile, TeamB bot, and TeamB projectile.
- Updated the MVP scene setup and graybox map builder so regenerated scenes preserve player and bot team visuals.
- Kept TeamA and TeamB colors aligned across player, bot, projectiles, spawn pads, paint overlay, and HUD.

Acceptance criteria:

- TeamA and TeamB are distinguishable at a glance.
- Spawn pads clearly communicate team ownership.
- TeamB setup can be reused by the bot.

### 4. Match Flow V1

Goal:

Make the MVP demo loop easier to restart and present after one match ends.

Suggested work:

- Add a clear match-finished UI state.
- Add restart/reset input for quick classroom playtests.
- Keep reset behavior limited to timer, score, paint state, player spawn, and bot spawn.
- Avoid adding menus or broad UI refactors until the playable loop is stable.

Acceptance criteria:

- Match end communicates winner or draw.
- Restart returns the scene to a playable state without reopening Unity.
- Paint coverage resets correctly.
- Player and TeamB bot restart from their team spawn areas.

## Risks To Watch

- The side platforms are still block-based and may need slope tuning for `CharacterController`.
- Tall cover can block the camera if placed too close to the player.
- Bot movement should stay simple until the map layout has been playtested.
- Wall painting remains out of scope for this phase.
- The first bot should use simple scripted movement before any pathfinding dependency is added.

## GitHub Workflow For Next Increment

Use the single shared development branch:

```text
Tianbo-Cao
```

Keep the PR focused on one outcome and include:

- `Description`
- `User Story`
- `Changes`
- `Acceptance Criteria`
- `Validation`
