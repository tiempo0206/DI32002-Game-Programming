# Splat Fighters Peer Feedback And Follow-Up Plan

Date: 2026-06-02

## Description

This document records feedback collected from other students after they reviewed the current playable version of Splat Fighters. The feedback focuses on making the game easier to understand, more personal for the player, and more complete as a classroom demonstration.

The current prototype already supports the core Turf War-style loop: moving through a 3D arena, shooting ink, painting territory, reading team coverage, competing against a TeamB bot, restarting matches, and using a standalone main menu. The next improvements should strengthen the player experience without weakening performance on laptop hardware.

## Peer Feedback Summary

### 1. Add Gameplay Guidance

Feedback:

New players need clearer instructions before entering a match. The current prototype contains several mechanics, but players may not immediately understand the objective, controls, ink resource, own-paint benefits, or enemy-paint movement penalty.

Recommended improvement:

Add a lightweight gameplay guide that is accessible from the main menu. The guide should explain the goal of Turf War, the essential controls, and the relationship between ink coverage and movement.

Suggested content:

- Goal: paint more of the arena than TeamB before the timer ends.
- Movement: use the movement keys to navigate the arena.
- Aim and shoot: use the mouse and fire input to paint territory.
- Swim form: hold the swim input while standing on TeamA paint to move faster and recover ink.
- Enemy ink: avoid TeamB paint because it slows movement.
- Match flow: pause, restart, and return to the menu when needed.

Acceptance criteria:

- The main menu includes a clear way to open gameplay instructions.
- The instructions explain the match objective and essential controls.
- The guide remains readable at common laptop resolutions.
- The guide can be closed without starting a match.
- The guide does not add noticeable runtime cost during gameplay.

### 2. Add Character Selection

Feedback:

Players would like a basic character selection step before starting a match. Even a small choice would make the game feel more intentional and improve the transition from menu to gameplay.

Recommended improvement:

Add a simple character selection screen or menu panel before gameplay begins. The first version should stay small: provide a few lightweight visual choices for the TeamA player while preserving the existing movement, shooting, painting, and camera behavior.

Suggested scope for the first version:

- Add two or three selectable TeamA character appearances.
- Use existing lightweight materials or public assets with documented licenses.
- Store the selected character choice before loading the gameplay scene.
- Apply the selected appearance to the TeamA player when the match starts.
- Keep the same gameplay statistics for every appearance.

Acceptance criteria:

- The player can choose a TeamA character appearance from the menu.
- The selected appearance is visible in the gameplay scene.
- Character selection does not change gameplay balance.
- Existing movement, shooting, swim form, camera, and restart behavior still work.
- Any public character assets are documented in `Public_Asset_Usage.md`.

### 3. Add Background Music

Feedback:

The prototype feels quiet during play. Background music would make the match feel more complete and improve the presentation quality.

Recommended improvement:

Add lightweight background music for the main menu and gameplay scene. Audio should support the game without distracting from movement, shooting, or UI feedback. Only original, royalty-free, or clearly licensed public audio should be used.

Suggested scope for the first version:

- Add one looping main menu track.
- Add one looping gameplay track.
- Add a simple music volume setting.
- Keep music volume below important gameplay sound effects.
- Document source URL, author, license, imported path, and project use for each public audio file.

Acceptance criteria:

- The main menu and gameplay scene each play an appropriate looping track.
- Music continues or transitions cleanly when entering a match.
- A user-accessible volume control is available.
- Music can be muted.
- Public audio usage is documented in `Public_Asset_Usage.md`.
- Audio does not introduce noticeable performance issues.

## Recommended Development Order

### Phase 1: Gameplay Guidance

Priority: P1

Reason:

Gameplay guidance provides the fastest improvement for first-time players and classroom reviewers. It has low technical risk and should be completed before adding more menu complexity.

Suggested Kanban labels:

- `type:feature`
- `type:docs`
- `priority:P1-high`
- `urgency:soon`
- `size:S`

### Phase 2: Character Selection V1

Priority: P2

Reason:

Character selection improves presentation and player ownership, but it should remain lightweight until the current menu flow and performance profile are stable.

Suggested Kanban labels:

- `type:feature`
- `type:design`
- `priority:P2-medium`
- `urgency:normal`
- `size:M`

### Phase 3: Background Music V1

Priority: P2

Reason:

Background music improves presentation quality and is suitable after the menu flow is stable. Public audio licensing and volume control must be included in the same increment.

Suggested Kanban labels:

- `type:feature`
- `type:polish`
- `priority:P2-medium`
- `urgency:normal`
- `size:S`

## Implementation Notes

- Keep menu UI readable and separate from the gameplay scene.
- Protect MacBook Air performance by avoiding heavy runtime effects.
- Do not use copyrighted Nintendo, Splatoon, or third-party assets without a compatible public license.
- Record all imported public assets in `Public_Asset_Usage.md`.
- Keep each feature as a small, reviewable Agile increment.
- Validate the existing Turf War loop after each change: movement, shooting, painting, scoring, TeamB bot behavior, pause, restart, swim form, and menu-to-gameplay navigation.

## Follow-Up User Stories

### Gameplay Guidance

As a new player, I want to read a short gameplay guide before entering a match, so that I understand the objective and controls without external explanation.

### Character Selection

As a player, I want to choose a character appearance before starting a match, so that the game feels more personal and the menu flow feels more complete.

### Background Music

As a player, I want background music in the menu and during gameplay, so that the game feels more polished and engaging.

