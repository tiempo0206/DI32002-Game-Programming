# ADR-001: Input System Strategy

## Status

Accepted for the MVP.

## Context

The current Splat Fighters runtime reads movement and action input through Unity's legacy `Input` API:

- `PlayerInputHandler` reads `Horizontal`, `Vertical`, and `KeyCode` actions for movement, jump, fire, and swim.
- `ThirdPersonCameraFollow` reads mouse axes for camera rotation.
- `GameManager`, `SpecialPaintBurst`, `RollerPaintTool`, and `InkWeapon` use small `KeyCode` bindings for MVP controls and debug controls.
- `Splat Fighters/Packages/manifest.json` does not include `com.unity.inputsystem`.

The technical documentation previously stated that Splat Fighters used the New Input System. That no longer matched the implementation.

## Decision

Splat Fighters will keep the Legacy Unity Input Manager for the local classroom MVP.

This is an explicit MVP decision, not an accidental mismatch. The current priority is stable keyboard/mouse playtesting for movement, shooting, swimming, reset, pause, special activation, and editor-configured tool testing.

## Rationale

- The MVP already has stable keyboard and mouse controls through `PlayerInputHandler`.
- The project does not currently need rebinding UI, controller profiles, split-screen devices, or action asset authoring.
- Migrating now would touch movement, camera, weapon, match controls, special controls, and editor scene setup at the same time.
- Keeping input simple reduces demo risk while core Turf War mechanics are still changing.

## Consequences

- Runtime code and documentation should refer to the Legacy Unity Input Manager for the MVP.
- New gameplay controls should stay small and centralized where possible.
- `PlayerInputHandler` remains the adapter around movement and player action state.
- Direct `Input.GetKey` use is acceptable for temporary debug controls or narrowly scoped MVP systems, but future production controls should route through an input adapter.

## Future Migration Trigger

Create a separate migration task only when at least one of these becomes required:

- Controller/gamepad support.
- Rebindable controls.
- Multiple local players.
- Input action maps for menus versus gameplay.
- Platform-specific input profiles.

## Validation

- Documentation no longer claims New Input System runtime support.
- `manifest.json` remains free of `com.unity.inputsystem`.
- Existing keyboard and mouse controls remain unchanged for the MVP.
