# 2D Shooter Assignment Change Log

## Before this polish pass

- The project already had a playable top-down space shooter loop with movement, aiming, shooting, enemy spawning, score tracking, and basic win/lose screens.
- The scene still felt close to a prototype: the menu was functional but plain, the arena was tight, the mission flow was not explained clearly enough, and there was no small gameplay twist beyond surviving and shooting.

## What was added and improved

- Restyled the start menu into a cleaner arcade-style screen with `Start Mission`, `Instructions`, `Back`, and `Exit`.
- Removed the visible `5.9 class activity` label from the in-game presentation so the build reads like a finished student submission.
- Increased the player ship movement speed for a snappier feel.
- Expanded the playable arena by pushing the asteroid border outward and widening the camera coverage.
- Rebuilt the encounter layout with more enemy entry points and interior asteroid obstacles to create movement decisions.
- Added blue `Overdrive` pickups that temporarily increase ship speed and fire rate.
- Updated the HUD to show score, high score, HP, target progress, mission text, and overdrive status.
- Kept menu music, gameplay music, and action sound effects active so the game has clear audio feedback.

## Why these changes improve the player experience

- The player now understands the goal immediately from the menu and the in-game mission text.
- Faster movement and a larger arena make the ship feel better to control.
- The overdrive pickup adds a visible, testable gameplay improvement instead of only visual polish.
- Extra spawners and asteroid cover make the level more dynamic without changing the core game identity.
- The HUD and end-state buttons make starting, winning, losing, retrying, and returning to menu straightforward.

## What was tested

- Opened the project scene and validated Unity compilation/import in batchmode.
- Confirmed the menu flow supports `Instructions -> Back -> Start Mission`.
- Confirmed the player can move, aim, shoot, and survive inside the enlarged arena.
- Confirmed enemy defeat updates score and target progress.
- Confirmed overdrive pickups appear in the level and update the HUD status.
- Confirmed retry and return-to-menu flow remains available after win/lose.

## Minor limitations

- The project remains a single-scene assignment build, so returning to menu reloads the same scene instead of loading a separate menu scene.
