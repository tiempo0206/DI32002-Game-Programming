# Next Work Plan After Graybox Map V1

Date: 2026-05-07

## Current Increment

User story:

As a course project developer, I want a compact graybox arena so the player can test movement, shooting, painting, and future bot behavior in a readable 3D space.

## Splatoon-Aligned Product Direction

Target:

Move the project from a simple ink shooter MVP toward a small, classroom-friendly Turf War prototype inspired by Splatoon.

Core loop to protect:

- Paint ground to claim territory.
- Use owned paint as a movement and resource advantage.
- Avoid enemy paint because it should slow, block, or pressure the player.
- Move between shooting, repositioning, refilling, contesting mid-map, retreating, and restarting quickly.
- Keep territory coverage as the primary win condition; eliminations should support map control instead of replacing it.

Current scope guardrails:

- Build one local single-player prototype first: TeamA player versus TeamB bot.
- Keep the main mode close to Turf War before adding ranked-style objectives.
- Avoid online 4v4, Salmon Run-style co-op, complex loadouts, and large weapon rosters until the core loop feels correct.
- Do not copy Nintendo branding, character names, assets, UI, music, or exact weapon names.

Priority adjustment:

The next work should now prioritize swim/squid-form movement, enemy ink movement pressure, territory-aware bot behavior, and Turf War presentation before heavier damage systems. Damage and respawn remain useful, but they should be secondary to painting, movement, and map control.

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

Status:

Implemented in the current branch.

Goal:

Make the MVP demo loop easier to restart and present after one match ends.

Implemented work:

- Added a paused match state.
- Added `R` restart and `P` / `Escape` pause controls for quick classroom playtests.
- Added match restart flow that resets timer, score, paint state, active projectiles, player spawn, and TeamB bot spawn.
- Added HUD text for paused, finished, and restartable match states.
- Updated scene generation so `GameManager` keeps explicit player, bot, and spawn references after regeneration.
- Keep reset behavior limited to timer, score, paint state, player spawn, and bot spawn.
- Avoid adding menus or broad UI refactors until the playable loop is stable.

Acceptance criteria:

- Match end communicates winner or draw.
- Restart returns the scene to a playable state without reopening Unity.
- Paint coverage resets correctly.
- Player and TeamB bot restart from their team spawn areas.

### 5. Ink Resource And Ground Benefits

Status:

Implemented in the current branch.

Goal:

Give paint ownership a gameplay impact without expanding the MVP into a large mechanics refactor.

Implemented work:

- Added a small ink tank to `InkWeapon` so repeated firing drains a readable resource.
- Blocked firing when the weapon does not have enough ink for the next shot.
- Added passive ink recovery, with faster recovery while the weapon owner stands on their own team paint.
- Used `PaintManager.TryGetTeamAtWorldPosition` as the ground ownership query for the refill benefit.
- Added HUD ink feedback for player ink level, low-ink state, and own-paint refill state.
- Reset player and TeamB bot ink resources during match restart.
- Keep penalties and damage out of scope until the resettable match loop is stable.

Acceptance criteria:

- Repeated firing has a readable resource or pacing limit.
- Standing on own paint provides one clear benefit.
- The behavior is explainable from existing paint grid ownership data.

### 6. Swim Form And Own-Ink Movement V1

Status:

Implemented in the current branch.

Goal:

Make owned paint feel like a mobility lane, not only a scoring texture.

Implemented work:

- Added hold-to-swim input on `LeftShift`.
- Enabled swim form only while the TeamA player is standing on TeamA paint.
- Increased movement speed while swimming on TeamA paint.
- Added an extra ink recovery multiplier while swimming on TeamA paint.
- Hid the humanoid renderer and showed a low `SwimFormVisual` marker while swimming.
- Disabled player firing while swimming.
- Updated HUD ink state and controls text so swim mode is readable during playtests.

Acceptance criteria:

- Player can switch between humanoid shooting and swim movement.
- Swim movement is faster only on TeamA paint.
- Ink refill is clearly faster while swimming on TeamA paint.
- Player cannot freely shoot while swimming.
- HUD or visuals make the current form readable.

### 7. Enemy Ink Movement Pressure V1

Status:

Implemented in the current branch.

Goal:

Make enemy paint matter immediately, matching the Splatoon idea that territory controls movement.

Implemented work:

- Slowed TeamA movement when the player is standing on TeamB paint.
- Kept TeamA paint as the only surface that enables swim form and extra refill.
- Left neutral ground at normal movement speed so traversal stays playable.
- Added HUD feedback that shows `Enemy ink` while the player is on TeamB paint.
- Kept hard damage out of this increment; this task is focused on movement pressure.

Acceptance criteria:

- TeamB paint slows TeamA player movement.
- TeamA paint keeps the current refill and swim benefit.
- Neutral ground stays playable and does not feel punishing.
- The behavior uses `PaintManager.TryGetTeamAtWorldPosition`.

### 8. Damage And Respawn V1

Status:

Implemented in the current branch.

Goal:

Add one direct opponent consequence while keeping territory control as the main objective.

Implemented work:

- Added lightweight `CharacterHealth` to the TeamA player and TeamB bot.
- Let enemy-owned paint apply continuous damage only to opposing characters.
- Added short delayed respawns back to each team spawn without restarting the match.
- Disabled movement, firing, and renderers while a character is eliminated.
- Added HUD health feedback for the player.
- Kept eliminations secondary to territory score.

Acceptance criteria:

- Player and bot can be defeated by enemy ink.
- Defeated characters return to their team spawn without restarting the match.
- Paint score, timer, restart, pause, ink resource, and swim movement still work.
- No friendly-fire damage is applied.

### 9. Turf War Presentation V1

Status:

Implemented in the current branch.

Goal:

Make the match read like a small Turf War round during classroom demos.

Implemented work:

- Added a runtime presentation banner that clearly shows `TeamA vs TeamB | Turf War`.
- Improved finished-state presentation with winner and final TeamA/TeamB percentages.
- Kept the existing score panel, timer, ink, HP, and controls visible during demos.
- Kept Restart controls available for fast repeated classroom demonstrations.

Acceptance criteria:

- Match start clearly communicates TeamA versus TeamB.
- Match end clearly shows which team covered more ground.
- Restart returns to the intro or ready state without reopening the scene.
- The scoring explanation is understandable without reading code.

### 10. TeamB Territory Bot V2

Goal:

Make the bot contest territory more like an opponent instead of only following fixed targets.

Status: Implemented in the current branch.

Implemented work:

- Added nearest-cell ownership queries in `PaintableArea` and `PaintManager`.
- Updated `BotController` to prioritize nearby TeamA paint, then unpainted cells, before fixed fallback targets.
- Added retreat behavior for low ink, low health, or standing on enemy paint.
- Kept waypoint movement as the stable fallback so the classroom demo remains deterministic.

Acceptance criteria:

- Bot chooses at least some paint targets from current territory state.
- Bot can repaint TeamA territory during the match.
- Bot retreats toward TeamB territory when low on ink or under paint pressure.
- Bot behavior remains deterministic enough for classroom testing.
- Score changes remain readable.

### 11. Wall Painting And Vertical Routes V1

Goal:

Add a small vertical movement payoff without turning the graybox map into a large traversal project.

Status: Implemented in the current branch.

Implemented work:

- Added `PaintRouteSurface` as a limited route gate driven by paint ownership.
- Added a TeamA-activated route probe and vertical route surface to the graybox map.
- Let the player climb the route only while holding swim input and the route probe is owned by TeamA.
- Kept scoring focused on ground territory; the vertical route is traversal-only for this increment.

Acceptance criteria:

- At least one vertical route can be enabled by painting its route probe.
- Player can climb or traverse it only when owned by TeamA.
- The route creates a useful flank or recovery path.
- Existing ground score remains stable.

### 12. Weapon Variety And Special Meter V1

Goal:

Introduce Splatoon-like weapon identity without building a large arsenal.

Status: In progress through smaller Agile slices.

Implemented work:

- Added `SpecialMeter` so TeamA painting charges a visible player special meter.
- Added `PaintManager` paint events so territory changes can drive future progression systems.
- Added HUD special meter text with a ready state.
- Reset the player special meter when paint is cleared or a match resets.
- Added `SpecialPaintBurst` so a ready meter can be spent on one larger paint burst.

Remaining work:

- Add one alternate close-range paint tool, such as a roller-style prototype.
- Keep weapon switching editor-configured instead of building a full loadout UI.

Acceptance criteria:

- Painting territory can charge a special meter. Implemented.
- A special action provides a visible temporary advantage. Implemented as a paint burst.
- The alternate tool has a different paint pattern from the shooter.
- The current shooter remains the default classroom-demo weapon.

### 13. Stretch Modes After Turf War Core

Goal:

Only expand modes after the local Turf War loop is fun and explainable.

Suggested future modes:

- Splat Zones-style center-area control using existing paint coverage logic.
- Tower Control-style moving objective after bot movement is more reliable.
- Salmon Run-style co-op should remain a long-term stretch because it requires wave spawning, PvE enemy roles, and team revive rules.

Acceptance criteria:

- Turf War loop is stable before any alternate mode work starts.
- New modes reuse existing paint, timer, score, respawn, and UI systems.
- Each mode is introduced as a separate small Agile increment.

## Risks To Watch

- The side platforms are still block-based and may need slope tuning for `CharacterController`.
- Tall cover can block the camera if placed too close to the player.
- Swim form can make the player too fast if the arena stays small.
- Enemy ink pressure can become frustrating if neutral ground is not useful.
- Bot movement should stay simple until the map layout has been playtested with swim movement.
- Wall painting should start with one or two authored surfaces before any general wall-paint system.

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
