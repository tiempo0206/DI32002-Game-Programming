# Audio Asset Notes

Last updated: 2026-06-07

## Source

The current Splat Fighters music and sound effects are project-original procedural audio assets created locally for this course project. No third-party audio or public sound-library downloads were copied into this increment.

## Asset List

| Asset | Path | Use |
| --- | --- | --- |
| MenuLoop.wav | `Assets/Resources/Audio/Music/MenuLoop.wav` | Upbeat menu loop for the main menu and setup flow |
| GameplayLoop.wav | `Assets/Resources/Audio/Music/GameplayLoop.wav` | Faster match loop for the gameplay scene |
| UiClick.wav | `Assets/Resources/Audio/SFX/UiClick.wav` | General menu click feedback |
| UiConfirm.wav | `Assets/Resources/Audio/SFX/UiConfirm.wav` | Confirm, start, and enter-arena feedback |
| UiBack.wav | `Assets/Resources/Audio/SFX/UiBack.wav` | Back, cancel, and quit-style feedback |
| SelectionMove.wav | `Assets/Resources/Audio/SFX/SelectionMove.wav` | Mode, difficulty, graphics preset, and character-switch feedback |
| WeaponFire.wav | `Assets/Resources/Audio/SFX/WeaponFire.wav` | Successful ink weapon fire feedback |
| InkImpact.wav | `Assets/Resources/Audio/SFX/InkImpact.wav` | Projectile impact and paint splat feedback |
| MatchStart.wav | `Assets/Resources/Audio/SFX/MatchStart.wav` | Match start stinger |
| MatchEnd.wav | `Assets/Resources/Audio/SFX/MatchEnd.wav` | Match finish stinger |
| SpecialBurst.wav | `Assets/Resources/Audio/SFX/SpecialBurst.wav` | Special paint burst feedback |

## Implementation Notes

- `Assets/Resources/Audio/Prefabs/SplatAudioManager.prefab` stores the reusable audio manager and clip references.
- `SplatAudioManager` persists across scene loads, switches between menu and gameplay music, and saves master/music/SFX volume preferences through `PlayerPrefs`.
- The main menu settings prefab includes Master Volume, Music Volume, and SFX Volume sliders so audio settings remain editable through the authored UI.

## Attribution

Attribution for these audio files should be listed as: "Original procedural audio created for Splat Fighters by the project team." No external source URL is required because the files are not public or third-party imports.
