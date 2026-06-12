# Splat Fighters Next Week Plan: Performance And 3D Asset Integration

Date: 2026-05-22
Planning window: 2026-05-25 to 2026-05-31

## Description

This plan defines the next development week for Splat Fighters. The focus is to improve the current playable prototype by reducing laptop performance cost, stabilizing the classroom demo experience, and preparing a controlled pipeline for importing public 3D scene assets.

The goal is not to add many new mechanics at once. The next week should make the existing Turf War-style loop smoother, clearer, and more presentable on MacBook Air hardware before adding heavier visual polish such as ink splatter particles.

## Current Context

The project already has a playable MVP scene with player movement, shooting, territory painting, score UI, a TeamB bot, restart and pause flow, ink resource behavior, swim movement on owned paint, and a larger graybox arena. Recent performance work disabled runtime ink splatter VFX because it created noticeable lag on the development laptop.

The next week should treat performance as a core acceptance requirement. Any imported 3D assets must be lightweight, properly licensed, documented, and tested in the MVP scene before they are kept.

## Weekly Goals

1. Improve runtime performance on MacBook Air hardware.
2. Keep the current MVP scene responsive during movement, shooting, bot behavior, and paint scoring.
3. Profile and reduce the most expensive systems before adding more visual detail.
4. Import a small set of public 3D scene assets to improve map readability and presentation.
5. Document every public asset source, license, imported path, and project use.
6. Preserve the ability to regenerate or rebuild the MVP scene without losing core gameplay objects.

## Scope

In scope:

- Performance profiling and frame-time observation.
- Paint grid and score update tuning.
- Projectile, bot, UI, and objective polling cost review.
- URP and quality profile review for classroom laptops.
- Low-poly 3D scene asset import.
- Asset scale, material, collider, and compression cleanup.
- Public asset usage documentation.
- MVP scene presentation improvements that do not harm performance.

Out of scope:

- Re-enabling ink splatter VFX.
- Adding online multiplayer.
- Adding large character customization systems.
- Importing copyrighted commercial game assets.
- Importing high-poly models without optimization.
- Adding multiple new bots before the current performance profile is stable.

## Planned Agile Tasks

### Task 1: Profile Current MVP Performance

User story:

As a developer, I want to identify the current runtime bottlenecks, so that optimization work targets real performance problems instead of guesses.

Description:

Run the MVP scene on the target laptop profile and record the main performance risks during normal play: movement, shooting, bot painting, score updates, swim movement, and match restart.

Acceptance criteria:

- Frame rate target and observed editor performance are recorded.
- The most expensive visible systems are listed.
- The test includes movement, shooting, bot behavior, and restart.
- Notes separate Unity Editor overhead from expected build behavior.

Definition of done:

- Profiling notes are added to the performance documentation.
- Any obvious errors or warning spam are recorded.
- Follow-up optimization tasks are updated if profiling changes priorities.

Priority: P1
Urgency: soon
Size: S
Suggested labels: `type:test`, `type:polish`, `priority:P1-high`, `urgency:soon`, `size:S`
Suggested Kanban column: Ready
Dependencies: Current MVP scene

### Task 2: Tune Paint And Score Update Cost

User story:

As a player on laptop hardware, I want painting and scoring to stay responsive, so that the core territory-control loop feels smooth.

Description:

Review paint grid size, score refresh frequency, objective polling, and overlay updates. Keep the current gameplay readable while reducing unnecessary per-frame or frequent work.

Acceptance criteria:

- Paint coverage scoring remains accurate enough for classroom demonstration.
- Score UI still updates clearly during active painting.
- Expensive polling intervals are documented.
- No visual regression makes territory ownership hard to understand.

Definition of done:

- Performance-sensitive settings are documented.
- Local scene validation confirms movement, shooting, painting, score updates, and restart still work.
- GitHub checks pass.

Priority: P1
Urgency: soon
Size: M
Suggested labels: `type:polish`, `type:refactor`, `priority:P1-high`, `urgency:soon`, `size:M`
Suggested Kanban column: Ready
Dependencies: Task 1

### Task 3: Review URP And Quality Settings For Laptop Demo

User story:

As a presenter, I want a lightweight render profile, so that the demo runs consistently on MacBook Air hardware.

Description:

Review the current URP performant asset and quality settings. Keep shadows, post-processing, render scale, anti-aliasing, and lighting choices aligned with the classroom laptop target.

Acceptance criteria:

- The project uses the intended performant quality profile.
- Costly effects remain disabled unless they are necessary for readability.
- Scene lighting remains clear enough to identify teams, paths, and objectives.
- Any Unity asset serialization changes are reviewed before committing.

Definition of done:

- Render settings changes are intentionally committed or intentionally left out.
- Performance notes document the final demo profile.
- The MVP scene remains visually readable.

Priority: P1
Urgency: soon
Size: S
Suggested labels: `type:polish`, `type:setup`, `priority:P1-high`, `urgency:soon`, `size:S`
Suggested Kanban column: Ready
Dependencies: Task 1

### Task 4: Build A Public 3D Asset Import Pipeline

User story:

As a developer, I want a repeatable public asset import process, so that the project can improve presentation without adding licensing or performance risk.

Description:

Define a lightweight workflow for downloading, importing, scaling, naming, optimizing, and documenting public 3D assets. Imported assets should support the arena theme without replacing the original gameplay systems.

Acceptance criteria:

- Each imported asset has a source URL, author or publisher, license, imported path, project use, and modification notes.
- Assets are stored in a clear project folder.
- Assets use reasonable scale, pivots, materials, and colliders.
- The imported assets do not introduce copyrighted commercial game content.

Definition of done:

- `Public_Asset_Usage.md` is updated for every imported asset.
- Asset import notes are included in the pull request description.
- The MVP scene opens without missing materials or references.

Priority: P1
Urgency: soon
Size: M
Suggested labels: `type:setup`, `type:docs`, `priority:P1-high`, `urgency:soon`, `size:M`
Suggested Kanban column: Ready
Dependencies: None

### Task 5: Import Lightweight Arena Decoration Assets

User story:

As a player, I want the arena to look more like a designed 3D space, so that navigation and team conflict areas are easier to understand.

Description:

Import a small set of low-poly public 3D assets for environmental decoration, such as barriers, ramps, crates, signage, railings, or urban arena props. Use them to improve spatial readability without blocking the core paintable ground.

Acceptance criteria:

- Imported assets improve route readability or presentation.
- Assets do not block critical movement routes unexpectedly.
- Colliders are simple and intentional.
- The scene remains playable on the target laptop profile.
- All public asset usage is documented.

Definition of done:

- MVP scene contains the selected optimized assets.
- Playtest confirms player, bot, projectiles, paint scoring, pause, restart, and swim movement still work.
- GitHub checks pass.

Priority: P2
Urgency: normal
Size: L
Suggested labels: `type:feature`, `type:design`, `type:polish`, `priority:P2-medium`, `urgency:normal`, `size:L`
Suggested Kanban column: Backlog
Dependencies: Task 4

### Task 6: Add Lightweight Background Presentation

User story:

As a viewer, I want the arena to have a more complete background, so that the classroom demo feels less like an empty prototype.

Description:

Add a simple background treatment such as a skybox, distant low-poly geometry, or non-interactive arena backdrop. The background should support the game mood without increasing gameplay cost.

Acceptance criteria:

- The background improves presentation without reducing gameplay readability.
- The background does not use expensive particle or post-processing effects.
- The camera view remains clear during movement and combat.
- Public asset usage is documented if third-party assets are used.

Definition of done:

- Background assets or settings are committed intentionally.
- Performance notes are updated if the background changes runtime cost.
- MVP playtest confirms no camera or readability regression.

Priority: P2
Urgency: normal
Size: M
Suggested labels: `type:polish`, `type:design`, `priority:P2-medium`, `urgency:normal`, `size:M`
Suggested Kanban column: Backlog
Dependencies: Task 4 if public assets are used

### Task 7: Prepare Audio Import Plan

User story:

As a player, I want lightweight sound feedback later, so that shooting, painting, UI actions, and match flow feel more responsive.

Description:

Prepare the plan for importing or creating audio assets without adding them immediately to the current performance-focused week. This keeps the next week focused on stability while making future polish easier.

Acceptance criteria:

- Required sound categories are listed.
- Public audio license requirements are documented.
- Audio import settings are planned for small file size and low runtime cost.
- No heavy music or sound system is added before performance work is stable.

Definition of done:

- Audio plan is documented.
- Future Kanban tasks can be created from the plan.

Priority: P3
Urgency: later
Size: XS
Suggested labels: `type:design`, `type:docs`, `priority:P3-low`, `urgency:later`, `size:XS`
Suggested Kanban column: Backlog
Dependencies: Performance stabilization

## Weekly Order

Recommended order:

1. Profile the current MVP scene.
2. Tune paint, scoring, and polling costs.
3. Review URP and quality settings.
4. Define the public asset import pipeline.
5. Import a small number of optimized 3D arena assets.
6. Add a lightweight background only after the MVP remains responsive.
7. Keep ink splatter VFX disabled until the final visual polish phase.

## Validation Plan

Run these checks before merging next week's implementation PRs:

- Confirm `MVP_ShootingTest` opens without missing references.
- Play one full match from start to finish.
- Confirm player movement, shooting, painting, swimming, bot painting, score UI, pause, and restart still work.
- Confirm the game remains responsive on the MacBook Air target profile.
- Confirm imported public assets are documented in `Public_Asset_Usage.md`.
- Run repository hygiene checks.
- Run GitHub Actions checks before merging.

## Risks

- Public assets may have unclear license terms.
- High-poly models or large textures may make the current laptop performance problem worse.
- Unity asset serialization changes may create noisy diffs.
- Scene decoration could accidentally block movement, projectiles, or paint scoring.
- Optimizing only in the Unity Editor may not match standalone build performance.

## Success Criteria For The Week

The week is successful if the current Splat Fighters prototype feels smoother on MacBook Air hardware, the MVP scene gains controlled 3D presentation improvements, all third-party public assets are documented, and the core Turf War-style loop remains stable and easy to demonstrate.
