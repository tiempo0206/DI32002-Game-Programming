# Public Asset Usage

Last updated: 2026-05-20

## Current Status

Splat Fighters currently does not include imported third-party public assets.

The current prototype uses:

- Unity primitive meshes for expanded graybox level geometry, player placeholders, bot placeholders, objectives, elevated routes, cover, and route markers.
- Unity-generated materials created by editor setup scripts.
- Procedural ink splatter particle effects generated at runtime from original C# scripts.
- Original C# scripts written for this course project.
- Runtime UI built from Unity UI text and image components.

## Required Tracking For Future Public Assets

If a future increment imports public assets from the internet, add each asset to the table below before opening the pull request.

| Asset | Source URL | Author / Publisher | License | Imported Path | Project Use | Modifications |
| --- | --- | --- | --- | --- | --- | --- |
| None currently imported | N/A | N/A | N/A | N/A | N/A | N/A |

## Review Rules

- Use only assets whose license allows the intended course-project use.
- Keep source URLs specific enough for a reviewer to verify the original asset.
- Do not import Nintendo, Splatoon, or other copyrighted game assets.
- Prefer simple placeholder assets until the gameplay loop is stable.
- Keep each imported asset tied to a GitHub issue or pull request.
