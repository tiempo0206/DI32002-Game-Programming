---
title: "Unity Event-Driven Code Lab"
subtitle: "ShootingController.cs Code Reading Report"
author: "Tianbo Cao's Group"
date: "May 14, 2026"
geometry: margin=0.72in
fontsize: 10.5pt
colorlinks: true
linkcolor: blue
urlcolor: blue
---

# Submission Summary

**Selected script:** `Assets/Scripts/ShootingProjectiles/ShootingController.cs`

**New concept studied:** Unity's Input System `InputAction` lifecycle inside MonoBehaviour event functions.

**Main claim:** Shooting is not controlled by a traditional `main()` loop. Unity owns the loop. `ShootingController` reacts when Unity calls event functions such as `OnEnable()`, `Start()`, `Update()`, and `OnDisable()`. The player-visible shooting result comes from a chain of Unity events, input polling, helper methods, projectile instantiation, visual effects, and sound feedback.

# 1. Event Function Hunt

`ShootingController` inherits from `MonoBehaviour`, so Unity can call special event functions by name.

| Unity event function | Where it appears | What Unity is doing | What this script does |
|---|---|---|---|
| `OnEnable()` | `ShootingController.cs` | Called when the component becomes enabled and active. | Calls `fireAction.Enable()` so the Fire input can listen/respond. |
| `OnDisable()` | `ShootingController.cs` | Called when the component becomes disabled or inactive. | Calls `fireAction.Disable()` so the Fire input stops responding. |
| `Start()` | `ShootingController.cs` | Called before the first frame update if the script is enabled. | Checks whether the player-controlled Fire action has any bindings. |
| `Update()` | `ShootingController.cs` | Called once per frame while enabled. | Calls `ProcessInput()` every frame. |

The helper methods are **not** Unity event functions:

| Helper method | Who calls it | Purpose |
|---|---|---|
| `ProcessInput()` | `Update()` | Checks whether this controller belongs to the player and reads the Fire input value. |
| `Fire()` | `ProcessInput()` or another script | Applies fire-rate cooldown, spawns the projectile, plays feedback, and records the fire time. |
| `SpawnProjectile()` | `Fire()` | Instantiates the projectile prefab, adds random spread, and parents it under `ProjectileHolder`. |
| `PlayFireSound()` | `Fire()` | Plays a one-shot fire sound near the camera/player. |

# 2. Code Evidence

Short evidence from the selected script:

```csharp
private void OnEnable()
{
    fireAction.Enable();
}

private void OnDisable()
{
    fireAction.Disable();
}

private void Update()
{
    ProcessInput();
}
```

The script reads the input action during the frame update:

```csharp
if (fireAction.ReadValue<float>() >= 1)
{
    Fire();
}
```

The actual shot is protected by a cooldown:

```csharp
public void Fire()
{
    if ((Time.timeSinceLevelLoad - lastFired) <= fireRate)
    {
        return;
    }

    SpawnProjectile();
    PlayFireSound();
    lastFired = Time.timeSinceLevelLoad;
}
```

# 3. Event Chain: From Unity Event to Game Reaction

```text
Scene loads / Player object becomes active
-> Unity calls ShootingController.OnEnable()
-> fireAction.Enable() starts listening for the Fire binding

Before first frame
-> Unity calls ShootingController.Start()
-> script checks whether Fire has a binding

Every rendered frame
-> Unity calls ShootingController.Update()
-> Update() calls ProcessInput()
-> ProcessInput() reads fireAction.ReadValue<float>()
-> if Fire input is pressed, ProcessInput() calls Fire()

Fire()
-> checks Time.timeSinceLevelLoad against lastFired and fireRate
-> calls SpawnProjectile()
-> Instantiate(projectilePrefab, transform.position, transform.rotation)
-> applies random projectile spread
-> parents the projectile under ProjectileHolder
-> creates fireEffect if assigned
-> calls PlayFireSound()
-> records lastFired

Player sees and hears:
-> a projectile leaves the ship
-> muzzle/fire feedback appears
-> fire sound plays
```

This is event-driven because the player action does not start from our own global game loop. The chain starts when Unity calls `Update()` and the enabled `InputAction` reports the current Fire value.

# 4. New Concept: InputAction Lifecycle + Polling

The new concept we focused on is **how an `InputAction` becomes active and how it is read inside Unity's event loop**.

In this project, `fireAction` is not useful just because it exists as a field. It must be enabled:

```csharp
private void OnEnable()
{
    fireAction.Enable();
}
```

It should also be disabled with the component:

```csharp
private void OnDisable()
{
    fireAction.Disable();
}
```

That pattern matters because it matches the lifetime of the `ShootingController`. When the player object, enemy gun, or menu state disables the component, the input action should stop listening too. This avoids stale input behavior and makes the script's active state easier to reason about.

This script uses **polling** instead of callback subscription. It checks the current input value each frame:

```csharp
fireAction.ReadValue<float>()
```

That makes sense here because holding the fire button should continue firing as long as the cooldown allows it. A single callback such as `performed` might be better for one-shot actions like opening a menu, but continuous firing is easier to understand with `Update()` + `ReadValue<float>()` + fire-rate cooldown.

# 5. Why the Cooldown Uses Time

The fire-rate check uses `Time.timeSinceLevelLoad`:

```csharp
if ((Time.timeSinceLevelLoad - lastFired) <= fireRate)
{
    return;
}
```

This means the script compares the current level time against the last successful shot. If the difference is smaller than `fireRate`, the method exits early. This prevents a projectile from being spawned every single frame while the fire button is held.

Player-facing result:

- Low `fireRate` = faster shooting.
- High `fireRate` = slower shooting.
- Holding fire feels responsive, but still controlled.

# 6. Misconceptions We Corrected

| Misconception | Correct understanding |
|---|---|
| `Fire()` is a Unity event. | `Fire()` is a helper method. Unity does not call it by name. `Update()` eventually calls it through `ProcessInput()`. |
| `OnEnable()` means the player fired. | `OnEnable()` only prepares the input action to listen/respond. |
| `Update()` is manually called by our code. | Unity calls `Update()` once per frame while the enabled component exists. |
| Input and shooting are the same thing. | Input is read first; shooting only happens if the controller is player-controlled, the input value is high enough, and the cooldown has passed. |
| Audio feedback is separate from gameplay. | In this script, audio feedback is part of the same event chain after a valid shot. |

# 7. Improvement Idea: Add Inspector Events for Shooting Feedback

The current script directly handles projectile spawning, fire effect, sound, and cooldown in one class. A small event-driven improvement would be to add `UnityEvent` hooks for shooting results.

Suggested pseudocode:

```csharp
using UnityEngine.Events;

[Header("Shooting Events")]
public UnityEvent onFired = new UnityEvent();
public UnityEvent onFireBlockedByCooldown = new UnityEvent();

public void Fire()
{
    if ((Time.timeSinceLevelLoad - lastFired) <= fireRate)
    {
        onFireBlockedByCooldown.Invoke();
        return;
    }

    SpawnProjectile();
    onFired.Invoke();
    lastFired = Time.timeSinceLevelLoad;
}
```

Why this improves the design:

- Designers can connect muzzle flash, UI recoil, controller vibration, sound, or ammo feedback in the Inspector.
- `ShootingController` can focus on the shooting rule instead of knowing every feedback system.
- The project becomes more event-driven: one script announces "a shot happened," and other components react.
- It is small enough to implement safely without changing the core shooter idea.

# 8. GitHub Post Ready to Paste

````markdown
# What we learned: ShootingController reacts to Unity events

Group: Tianbo Cao's group
Scripts studied: ShootingController.cs, Projectile.cs

## New concept

We learned that player shooting in Unity is event-driven. The script does not run from a
traditional `main()` loop. Unity calls `OnEnable()`, `Start()`, `Update()`, and
`OnDisable()` at specific points in the component lifecycle.

Our selected concept is the `InputAction` lifecycle. In `ShootingController`, the Fire
input action is enabled when the component becomes active and disabled when the component
is disabled.

## Code evidence

```csharp
private void OnEnable()
{
    fireAction.Enable();
}

private void OnDisable()
{
    fireAction.Disable();
}

private void Update()
{
    ProcessInput();
}
```

The script uses polling inside `Update()`:

```csharp
if (fireAction.ReadValue<float>() >= 1)
{
    Fire();
}
```

## Event chain

Unity enables the object  
-> `ShootingController.OnEnable()`  
-> `fireAction.Enable()` starts listening  
-> Unity calls `Update()` every frame  
-> `ProcessInput()` reads the Fire action  
-> `Fire()` checks cooldown with `Time.timeSinceLevelLoad`  
-> `SpawnProjectile()` instantiates a projectile  
-> fire effect and fire sound play  
-> the player sees and hears the ship shoot.

## Why this matters

In Java or simple console programs, we often look for `main()` and a loop we control.
In Unity, the engine owns the loop. Our scripts react to named event functions. This
changes how we read code: `Update()` is a Unity event, but `Fire()` and
`SpawnProjectile()` are helper methods called by the event chain.

## Improvement idea

Add `UnityEvent` hooks such as `onFired` and `onFireBlockedByCooldown`. Then sound,
visual effects, ammo UI, and screen feedback can listen to shooting events without all
that feedback being hard-coded inside `ShootingController`.

## Sources

- Unity Manual: Event Functions
- Unity Manual: Event function execution order
- Unity Input System API: InputAction
- Unity Scripting API: Time.timeSinceLevelLoad
- Unity Scripting API: AudioSource.PlayClipAtPoint
- Unity Manual: UnityEvents / Inspector-configurable custom events

## Reflection

The clearest takeaway is that event-driven code is easier to trace when we separate
Unity event functions from helper methods. `Update()` starts the frame-by-frame input
check, but the actual gameplay reaction is built by smaller methods that spawn the
projectile, play feedback, and enforce cooldown.
````

# 9. Three-Minute Presentation Plan

## Minute 1: Concept

Our script is `ShootingController.cs`. The key concept is that Unity scripts react to engine events. `ShootingController` uses `OnEnable()`, `Start()`, `Update()`, and `OnDisable()`. We focused on how `InputAction` is enabled, disabled, and read during `Update()`.

## Minute 2: Event chain

Show the chain:

```text
Unity Update event
-> ProcessInput()
-> fireAction.ReadValue<float>()
-> Fire()
-> cooldown check
-> SpawnProjectile()
-> fire effect and sound
-> player sees the ship shoot
```

Emphasize that `Fire()` is not a Unity event. It is a helper method reached from Unity's frame update event.

## Minute 3: Improvement and reflection

Our improvement is to add `UnityEvent` hooks like `onFired`. That would let designers attach sound, UI, and visual feedback in the Inspector. It also makes the code less tightly coupled because `ShootingController` announces a shooting event instead of directly owning every reaction.

Final reflection: the most important reading skill is to ask, "Who calls this method?" If Unity calls it by name, it is an event function. If another method calls it, it is part of the event chain.

# 10. Sources

- Unity Manual, Event Functions: <https://docs.unity3d.com/Manual/EventFunctions.html>
- Unity Manual, Event function execution order: <https://docs.unity3d.com/Manual/execution-order.html>
- Unity Input System API, `InputAction`: <https://docs.unity3d.com/Packages/com.unity.inputsystem@1.10/api/UnityEngine.InputSystem.InputAction.html>
- Unity Scripting API, `Time.timeSinceLevelLoad`: <https://docs.unity3d.com/ScriptReference/Time-timeSinceLevelLoad.html>
- Unity Scripting API, `AudioSource.PlayClipAtPoint`: <https://docs.unity3d.com/ScriptReference/AudioSource.PlayClipAtPoint.html>
- Unity Manual, Inspector-configurable custom events / UnityEvents: <https://docs.unity3d.com/Manual/UnityEvents.html>
- Project source inspected: `Assets/Scripts/ShootingProjectiles/ShootingController.cs`

# 11. Final Checklist

| Required item | Included |
|---|---|
| Selected script | Yes: `ShootingController.cs` |
| Unity event functions identified | Yes: `OnEnable`, `OnDisable`, `Start`, `Update` |
| Event chain | Yes: input to projectile/audio feedback |
| New concept researched | Yes: `InputAction` lifecycle and polling |
| Code evidence | Yes: short focused snippets |
| Improvement idea | Yes: `UnityEvent` hooks for shooting feedback |
| GitHub post | Yes: ready to paste |
| 3-minute presentation | Yes: minute-by-minute plan |
| Sources | Yes: official Unity docs plus project source |
