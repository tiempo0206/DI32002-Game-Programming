# Final Map Playtest And Route Tuning

Date: 2026-06-04
Issue: SF-53 Final map playtest and route tuning v1

## Purpose

This note records the current route-readability and match-flow validation pass for the Splat Fighters MVP arena. The goal is to keep the imported hangar map playable, fair, and stable before adding heavier polish such as additional VFX, audio layers, or extra maps.

## Current Playable Baseline

- The MVP starts from `MainMenu.unity` and loads `MVP_ShootingTest.unity`.
- The match scene contains TeamA player spawn, TeamB bot spawn, center objective geometry, side platforms, flank routes, hangar visual assets, and invisible arena containment colliders.
- The player can move, jump, aim, shoot, use the roller, swim on owned paint, use the special paint burst, pause, restart, and reset the match.
- The TeamB bot patrols through center and side-route waypoints, paints contested ground, retreats when pressured, and now supports Easy, Normal, and Hard difficulty tuning.

## Route Review

### Spawn Safety

- TeamA and TeamB both have rear spawn cover and forward cover before entering the center lane.
- The spawn points remain inside the containment boundary and away from the new invisible edge colliders.
- Current decision: keep spawn cover placement unchanged for the next playtest because both teams have comparable protection.

### Center Contest

- The center block, pillars, and screens create a readable first engagement area.
- The center objective remains reachable from both spawn sides.
- Current decision: keep the center layout unchanged, because the route structure is clear and the bot can target center paint reliably.

### Side Routes

- West and east platforms provide alternate routes around the center.
- North and south perch platforms give each side a mirrored elevated route.
- Current decision: keep side-route geometry stable, but continue testing camera framing around platform edges during manual play.

### Boundary Safety

- `NorthArenaContainmentWall`, `SouthArenaContainmentWall`, `EastArenaContainmentWall`, and `WestArenaContainmentWall` prevent the player, bot, and projectiles from leaving the arena.
- These colliders are invisible and do not change the visible hangar art or paintable ground.
- Current decision: keep the high containment walls as the stable boundary solution.

### Bot Route Stability

- The TeamB patrol route moves from spawn into center pressure, side coverage, and forward contest points.
- Bot paint targets include center, left lane, right lane, TeamA side, TeamA flank, and TeamA objective approach.
- Current decision: keep the route but validate Easy, Normal, and Hard difficulty during the next manual play session.

## Manual Playtest Checklist

Run this checklist from `Assets/Scenes/MainMenu.unity` in the Unity Editor.

- [ ] Main menu appears as a standalone UI screen, not as a gameplay overlay.
- [ ] Start Game opens character selection.
- [ ] Player and opponent character selections are visible and animated.
- [ ] AI difficulty can be cycled through Easy, Normal, and Hard.
- [ ] Confirming character selection loads `MVP_ShootingTest.unity`.
- [ ] The selected player model replaces the prototype cylinder.
- [ ] The selected opponent model replaces the prototype bot cylinder.
- [ ] Player and opponent ink colors match their selected characters.
- [ ] The match waits for Enter or Start Match before the countdown begins.
- [ ] Timer, score HUD, health, ink, special meter, and controls text remain readable.
- [ ] TeamB bot moves through the arena and paints territory.
- [ ] Easy bot behavior is visibly slower and less accurate than Normal.
- [ ] Hard bot behavior is visibly faster and more accurate than Normal.
- [ ] The player cannot jump or fall out of the arena.
- [ ] Center route, side platforms, and flank routes remain reachable.
- [ ] Camera movement does not frequently hide the player behind hangar ceiling or wall geometry.
- [ ] Pause, resume, restart, and reset match still work.
- [ ] No console error spam appears during normal play.

## Validation Performed In This Increment

- Repository status was checked before changes.
- Scene route anchors and containment wall anchors remain part of the MVP scene.
- AI difficulty logic was added without requiring a full scene rebuild.
- The menu exposes selected AI difficulty before the match starts.
- The TeamB bot reads the saved difficulty when the gameplay scene starts.

## Follow-Up Risks

- Manual Play Mode testing is still required before marking this item fully done.
- Imported hangar props may still create camera occlusion in some extreme angles.
- Hard difficulty should remain challenging without making the classroom demo look unfair or chaotic.
