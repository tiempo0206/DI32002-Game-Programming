# MVP Baseline Regression Checklist

Date: 2026-04-29

## Baseline Scope

This checklist freezes the current playable MVP baseline before the project moves into graybox map and TeamB bot development.

The baseline includes:

- Third-person player movement and jumping.
- Center-screen reticle aiming.
- Ink weapon firing.
- Projectile visual feedback.
- Crosshair-authoritative ground painting.
- Grid-based TeamA / TeamB paint scoring.
- Runtime paint overlay display.
- Match timer and score HUD.
- MVP shooting test scene setup.

## Required Manual Playtest

Run these checks in `Assets/Scenes/MVP_ShootingTest.unity`.

1. Open the scene and wait for Unity compilation to finish.
2. Press Play.
3. Move the player with WASD.
4. Rotate the camera with the mouse.
5. Confirm the center reticle remains visible.
6. Aim at nearby ground and fire.
7. Confirm paint appears at the reticle target.
8. Aim at distant ground and fire.
9. Confirm paint still appears at the reticle target.
10. Fire repeatedly and confirm previous projectiles do not steal the aim ray.
11. Confirm the player does not appear embedded in the floor.
12. Confirm the score HUD updates after painting.
13. Confirm the match timer counts down.
14. Stop Play mode and confirm there are no new console errors.

## Code Checks

The baseline should pass:

- `git diff --check`
- no Chinese characters in `Assets/Scripts` or `Assets/Editor`
- no new C# compiler errors in the Unity Editor log

## Known Limitations

- The current level is still a flat MVP test arena.
- TeamB bot behavior is not implemented yet.
- Match-end result UI is not implemented yet.
- New Input System migration is intentionally deferred until core gameplay is stable.
- Wall painting and arbitrary curved-surface painting remain out of scope for the MVP baseline.

## Next Development Step

Proceed to graybox 3D map v1 after this baseline has been committed, pushed, checked in PR, and merged back into `main`.
