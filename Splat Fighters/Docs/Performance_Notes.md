# Splat Fighters Performance Notes

## Target Hardware

The current demo profile is tuned for classroom laptops, including MacBook Air hardware without a dedicated GPU.

## Runtime Profile

- The MVP scene adds `PerformanceProfile` to the `GameManager` object.
- The default target frame rate is 30 FPS.
- VSync is disabled at runtime so the frame cap is controlled by `Application.targetFrameRate`.
- `Time.fixedDeltaTime` stays at 0.02 seconds to keep physics predictable.
- The project quality setting defaults to the existing `Performant` level.

## Paint System Cost

- The main map remains 32 by 36 world units.
- The paint grid is 80 by 90 cells.
- The previous 96 by 108 grid had 10,368 raw cells before blockers.
- The new 80 by 90 grid has 7,200 raw cells before blockers, reducing score and overlay iteration cost while preserving full-map paint coverage.
- Score refresh runs every 0.2 seconds instead of every 0.1 seconds.
- Splat Zone and Tower objective polling runs every 0.25 seconds.

## VFX Cost

- Ink splatter feedback is still enabled.
- The splatter burst has a lower particle count.
- Runtime splatter objects are capped at 14 active instances.
- Splatter cleanup is shortened to 0.9 seconds.

## Editor Cost

- Paintable cell gizmo drawing is disabled by default in the generated MVP scene.
- This prevents the Unity Scene view from drawing thousands of grid cell cubes while the game is running in the editor.

## Remaining Risks

- Public 3D models or audio added later can increase load time and memory usage if they are not compressed or scaled down.
- Additional bots will multiply weapon, projectile, paint, health, and VFX cost.
- Higher-resolution paint grids may make coverage smoother, but they should be tested on target laptops before being merged.
- Unity Editor overhead is higher than a standalone build, so classroom demos should use a build when possible.

## Demo Recommendations

- Use the `MVP_ShootingTest` scene for playtesting.
- Keep the Game view at a moderate resolution while testing in the Unity Editor.
- Close unrelated apps before presenting on MacBook Air hardware.
- Prefer a standalone build for the final demo if time allows.
