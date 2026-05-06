# Next Work Plan After Graybox Map V1

Date: 2026-05-06

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

Goal:

Add the first opponent pressure source.

Suggested work:

- Create a simple TeamB bot scene object.
- Move with deterministic waypoints first.
- Avoid NavMesh until the arena has been playtested with the current `CharacterController` and blockout geometry.
- Make the bot call `InkWeapon.SetAimTarget` and `InkWeapon.TryFire`.
- Use the existing `PaintManager.TryGetTeamAtWorldPosition` query later for territory-aware movement.

Acceptance criteria:

- Bot belongs to TeamB.
- Bot moves through the arena.
- Bot paints TeamB territory.
- Score UI shows both teams changing territory.

### 3. Team Visual Readability

Goal:

Make TeamA and TeamB readable before adding the bot.

Suggested work:

- Add TeamB material assets.
- Add a `TeamVisualBinder` if repeated team-colored renderers become hard to manage manually.
- Use the same team colors for player/bot, projectiles, spawn pads, paint overlay, and HUD.

Acceptance criteria:

- TeamA and TeamB are distinguishable at a glance.
- Spawn pads clearly communicate team ownership.
- TeamB setup can be reused by the bot.

## Risks To Watch

- The side platforms are still block-based and may need slope tuning for `CharacterController`.
- Tall cover can block the camera if placed too close to the player.
- Bot movement should stay simple until the map layout has been playtested.
- Wall painting remains out of scope for this phase.
- The first bot should use simple scripted movement before any pathfinding dependency is added.

## GitHub Workflow For Next Increment

Use a new branch from latest `main`:

```text
Tianbo-Cao-team-b-bot-v1
```

Keep the PR focused on one outcome and include:

- `Description`
- `User Story`
- `Changes`
- `Acceptance Criteria`
- `Validation`
