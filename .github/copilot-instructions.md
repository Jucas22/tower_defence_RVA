# AI Coding Instructions for Unity AR Monster Game

## Project Overview

**RVA Unity AR Game** - An Augmented Reality application built with **Unity 6.0.2** featuring Vuforia-based ground plane detection and marker-based tower spawning. The project is a monster-tower defense style game where AR monsters chase towers spawned via image markers.

### Core Architecture

The project uses an **event-driven architecture** connecting three main systems:

1. **Image Target System** ([img_target_tower.cs](Assets/Scripts/img_target_tower.cs)) - Detects image markers (via Vuforia `ObserverBehaviour`) and spawns tower prefabs. Fires `TowerSpawned` static event globally.

2. **Monster AI** ([monster_controller.cs](Assets/Scripts/monster_controller.cs)) - Listens to `TowerSpawned` event, autonomously pathfinds to towers using `Vector3.Lerp` linear movement, and manages animation states (`is_walking`, `is_staying` bool parameters).

3. **Ground Plane Placement** ([GroundPlacementController.cs](Assets/Scripts/GroundPlacementController.cs)) - Detects Vuforia ground planes via `PlaneFinderBehaviour`, allows tap-to-place avatars, and manages avatar movement with basic animation.

### Key Data Flows

- **Image Marker Detection** → `ObserverBehaviour.OnObserverStatusChanged` → `TowerSpawned` event → `monster_controller` listens and assigns `targetTower`
- **Ground Interaction** → `HitTest` (Vuforia) / `Physics.Raycast` fallback → Calculate 3D position → Move avatar via `transform.Translate()` / `Vector3.Lerp()`
- **Animation Binding** → `Animator.SetBool()` for `is_walking`/`is_staying`, `is_idle`/`isWalking` (note: naming varies by script)

## Critical Dependencies & Integration Points

### Vuforia Integration

- **Library Path**: `Packages/com.ptc.vuforia.engine` (v11.4.4)
- **Key Classes**: `ObserverBehaviour`, `PlaneFinderBehaviour`, `HitTest`, `ContentPositioningBehaviour`
- **Pattern**: Use `ObserverBehaviour.OnTargetStatusChanged` event for marker detection (see [img_target_tower.cs#L39](Assets/Scripts/img_target_tower.cs#L39))
- **AR Camera Setup**: Always cache `Camera.main` at startup; many scripts require it for raycasting/UI interaction

### Input System

- Uses **New Input System** (`com.unity.inputsystem` v1.14.2)
- [SimpleJoystick.cs](Assets/Scripts/SimpleJoystick.cs) implements `IDragHandler`, `IPointerDownHandler`, `IPointerUpHandler` for touch input
- [mover.cs](Assets/mover.cs) uses legacy `Input.GetKey()` - **keep legacy input available for editor testing**

### Physics & Movement

- Monsters use **kinematic Rigidbody** (see [monster_controller.cs#L41](Assets/Scripts/monster_controller.cs#L41)): `rb.isKinematic = true`
- Movement via `Vector3.Lerp()` for smooth interpolation, NOT physics forces
- Stopping distance: `stoppingDistance` field (varies: 0.1m for avatars, 0.6m for monsters)

## Project-Specific Conventions & Patterns

### Naming Conventions

- **MonoBehaviour classes**: PascalCase (`GroundPlacementController`, `ARMonsterPlacer`)
- **Animation bool parameters**: Inconsistent! Check comments:
  - `monster_controller`: `is_staying`, `is_walking` (see [monster_controller.cs#L24-L25](Assets/Scripts/monster_controller.cs#L24-L25))
  - `GroundPlacementController`: `isIdle`, `isWalking` (configurable via Inspector fields)
  - Always parameterize these as `[SerializeField] string` for flexibility

### Event Patterns

- **Static Action Events** for global communication (see [img_target_tower.cs#L20](Assets/Scripts/img_target_tower.cs#L20)):
  ```csharp
  public static Action<Transform> TowerSpawned;
  ```
- **Usage**: `img_target_tower.TowerSpawned += OnTowerSpawned;` in [monster_controller.cs#L66](Assets/Scripts/monster_controller.cs#L66)
- Unsubscribe in `OnDisable()` to prevent memory leaks

### Animator State Management

Always follow this pattern when managing animation states:

```csharp
animator.SetBool(stayBoolName, staying);
animator.SetBool(walkBoolName, walking);
```

See [monster_controller.cs#L177](Assets/Scripts/monster_controller.cs#L177) for `SetAnimatorState()` helper method.

### Vuforia Target Status Checking

When detecting targets, always compare as uppercase string:

```csharp
string statusName = statusObj.ToString().ToUpperInvariant();
bool isFound = statusName == "DETECTED" || statusName == "TRACKED";
```

See [img_target_tower.cs#L45-L47](Assets/Scripts/img_target_tower.cs#L45-L47)

## Build & Development Workflows

### Project Setup

1. **Vuforia Configuration**: Ensure Vuforia is initialized in `ProjectSettings` (camera permissions, app license)
2. **Scene Preparation**:
   - Add `ARCamera` with `ObserverBehaviour` components for image targets
   - Add `PlaneFinderBehaviour` for ground plane detection (optional for some features)
   - Assign prefabs in Inspector fields

### Editor vs. Device Testing

- **Editor**: Use mouse clicks to simulate touches (raycast to ground plane)
- **Device**: Requires actual image targets and ground plane detection
- [ARMonsterPlacer.cs](Assets/Scripts/ARMonsterPlacer.cs#L28) handles both via `Camera.main` and fallback raycasting

### Common Issues & Patterns

| Issue                            | Pattern                                                                        | Reference                                                                                |
| -------------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Animator parameter name mismatch | Use `[SerializeField] string` for parameter names, configurable in Inspector   | [monster_controller.cs#L24-L25](Assets/Scripts/monster_controller.cs#L24-L25)            |
| Missing Rigidbody on prefabs     | Add auto-creation in `Awake()` with `gameObject.AddComponent<Rigidbody>()`     | [monster_controller.cs#L38-L41](Assets/Scripts/monster_controller.cs#L38-L41)            |
| Ground plane not detected        | Fallback to `Physics.Raycast` with `layerMask`                                 | [GroundPlacementController.cs](Assets/Scripts/GroundPlacementController.cs) (lines ~200) |
| Placement race condition         | Use `.spawnOnce = true` flag in `img_target_tower` to prevent duplicate spawns | [img_target_tower.cs#L16](Assets/Scripts/img_target_tower.cs#L16)                        |

## File Organization

```
Assets/
├── Scripts/              # All gameplay logic
│   ├── ARMonsterPlacer.cs (UI mode toggle for placement)
│   ├── GroundPlacementController.cs (Avatar placement & movement)
│   ├── monster_controller.cs (AI pathfinding to towers)
│   ├── img_target_tower.cs (Marker detection & spawning)
│   ├── MonsterManualController.cs
│   ├── SimpleJoystick.cs (Touch input UI)
│   └── GroundPlacementController_README.md (detailed usage guide)
├── Prefabs/             # Reusable GameObjects
├── Materials/           # Shaders & visual assets
├── Animations/          # Animator clips & parameters
└── Scenes/              # Unity scenes
```

## Important Notes for AI Agents

1. **Test Cases Focus**: Always test on device with actual image targets; editor testing is limited.
2. **Performance**: Monsters use kinematic movement to avoid physics overhead.
3. **Prefab Dependencies**: Always ensure prefabs have `Animator` components and valid animation parameter names.
4. **Tag Usage**: [monster_controller.cs#L8](Assets/Scripts/monster_controller.cs#L8) searches towers by tag—ensure "Tower" tag exists and is assigned.
5. **Documentation**: [GroundPlacementController_README.md](Assets/Scripts/GroundPlacementController_README.md) provides detailed step-by-step configuration guide for placement system.

---

**Last Updated**: January 17, 2026  
**Unity Version**: 6.0.2  
**Key Dependencies**: Vuforia Engine 11.4.4, New Input System 1.14.2
