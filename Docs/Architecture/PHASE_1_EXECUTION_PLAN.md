# Clout — Phase 1 Execution Plan

> Port the combat + character foundation from NullReach, then build the empire layer on top.

---

## Phase 1 Goal
A playable character that can walk around a test arena, fight enemies with melee + ranged combat, interact with objects, and see their CLOUT score change.

## Execution Order

### Step 1: Port Core Architecture ✅ COMPLETE
Port from NullReach with `NullReach` → `Clout` namespace rename:

| NullReach File | Clout Target | Status |
|------|------|--------|
| Core/StateManager.cs | ✅ Core/StateManager.cs | Done |
| Core/State.cs | ✅ Core/State.cs | Done |
| Core/StateAction.cs | ✅ Core/StateAction.cs | Done |
| Core/CharacterStateManager.cs | ✅ Core/CharacterStateManager.cs | Done — added combat fields, lock-on, combo, weapon references |
| Core/Interfaces.cs | ✅ Core/Interfaces.cs | Done — added AttackInputs, IParryable, Combo, crime WeaponTypes |
| Stats/RuntimeStats.cs | ✅ Stats/RuntimeStats.cs | Done — added HandleStamina, HandlePoise, ApplyPoiseDamage |
| Core/ComboInfo.cs | ✅ Core/ComboInfo.cs | Done |

### Step 2: Port Controller Actions ✅ COMPLETE

| File | System | Status |
|------|--------|--------|
| Actions/MovePlayerCharacter.cs | Movement with stance speed modifiers | ✅ Done — added overweight penalty |
| Actions/InputHandler.cs | Combat input routing (melee vs ranged context) | ✅ Done |
| Actions/HandleRotation.cs | Character rotation logic | ✅ Done |
| Actions/HandleStats.cs | Stamina regen, stat monitoring | ✅ Done |
| Actions/HandleRollVelocity.cs | Dodge/roll physics | ✅ Done |
| Actions/MonitorInteraction.cs | Animation-to-state transition monitor | ✅ Done |
| Actions/InputsForCombo.cs | Combo chain input buffering | ✅ Done |

### Step 3: Port Combat System ✅ COMPLETE

| File | System | Status |
|------|--------|--------|
| Combat/WeaponItem.cs | Weapon SOs + ItemAction + WeaponHook + WeaponHolderManager | ✅ Done — crime weapon types |
| Combat/AttackAction.cs | Melee attack execution | ✅ Done |
| Combat/RangedAttackAction.cs | Ranged attack with RangedWeaponHook | ✅ Done |
| Combat/DamageCollider.cs | Melee hit detection + ParryCollider | ✅ Done |
| Combat/Projectile.cs | Projectile physics + IParryable reflect | ✅ Done |
| Combat/RangedWeaponHook.cs | Gun behavior (ammo, spread, ADS) | ✅ Done |
| Combat/RecoilController.cs | Camera recoil | ✅ Done |
| Combat/AmmoCacheManager.cs | Ammo reserve management | ✅ Done — crime ammo types |
| Combat/AmmoDefinition.cs | Ammo type definitions | ✅ Done |

### Step 4: Port Camera System ✅ COMPLETE

| File | System | Status |
|------|--------|--------|
| Camera/CameraManager.cs | 4-mode Cinemachine camera | ✅ Done |
| Camera/CameraCollision.cs | SphereCast collision prevention | ✅ Done |

### Step 5: Port Animation System ✅ COMPLETE

| File | System | Status |
|------|--------|--------|
| Animation/AnimatorHook.cs | IK + animation events + root motion | ✅ Done |
| Player/PlayerStateManager.cs | Full combat wiring with all state actions | ✅ Done |
| Player/PlayerInputHandler.cs | New Input System handler | ✅ Done (existed) |

### Step 6: Port Network Layer ✅ COMPLETE

| File | System | Status |
|------|--------|--------|
| Network/NetworkBootstrapper.cs | FishNet initialization | ✅ Done |
| Network/NetworkAnimatorSync.cs | Animation replication | ✅ Done |
| Network/NetworkDamageHandler.cs | Server damage validation | ✅ Done |
| Network/PlayerSpawnManager.cs | Player spawning | ✅ Done |

### Step 7: Build Test Arena ✅ COMPLETE
- [x] Editor script to create test scene (Editor/TestArenaBuilder.cs)
- [x] Player prefab with all components (PlayerStateManager, RuntimeStats, CameraManager, etc.)
- [x] 3 enemy NPCs (melee thug, ranged shooter, hybrid enforcer)
- [x] Ground plane with NavMesh + cover objects + boundary walls
- [x] Cinemachine camera rig (4-mode system built programmatically)
- [x] Basic HUD (health, stamina, CLOUT, ammo, wanted level, crosshair, state debug)

### Step 8: Wire Empire Systems (NEXT)
- [ ] Connect ReputationManager to combat events (damage dealt, kills)
- [ ] Connect WantedSystem to combat actions (gunfire = heat)
- [x] Add CLOUT rank display to HUD (CombatHUD.cs)
- [x] Add wanted level indicator to HUD (CombatHUD.cs — 5-star system)

### Step 9: Port AI System ✅ COMPLETE
| File | System | Status |
|------|--------|--------|
| AI/AIStateManager.cs | Base enemy AI | ✅ Done — aggression system |
| AI/Actions/AIDetection.cs | Detection + FOV | ✅ Done |
| AI/Actions/AIChaseTarget.cs | NavMesh pursuit | ✅ Done |
| AI/Actions/AICombatSelector.cs | Melee vs ranged decision | ✅ Done |
| AI/Actions/AIRangedAttack.cs | Ranged AI behavior | ✅ Done |
| AI/Actions/AIActionScoring.cs | Utility theory scoring | ✅ Done |
| AI/Actions/AIPatrol.cs | Patrol behavior | ✅ Done |

---

## Script Count

| Category | Count | Status |
|----------|-------|--------|
| Core (StateManager, State, Interfaces, etc.) | 7 | ✅ Complete |
| Controller Actions | 7 | ✅ Complete |
| Combat System | 9 | ✅ Complete |
| Camera System | 2 | ✅ Complete |
| Animation System | 1 | ✅ Complete |
| Network Layer | 4 | ✅ Complete |
| Player | 2 | ✅ Complete |
| Stats | 1 | ✅ Complete |
| Empire Systems | 7 | ✅ Complete (existed) |
| World Systems | 2 | ✅ Complete (existed) |
| Inventory | 2 | ✅ Complete (existed) |
| AI System | 7 | ✅ Complete |
| Editor Tools | 1 | ✅ Complete (TestArenaBuilder) |
| UI / HUD | 1 | ✅ Complete (CombatHUD) |
| **TOTAL** | **55** | **All scripts complete** |

---

## Validation Criteria

Phase 1 is DONE when:
- [x] Core state machine compiles clean
- [x] All combat scripts ported with Clout namespaces
- [x] Controller actions wired into PlayerStateManager states
- [x] Camera system ported with 4-mode switching
- [x] Network layer ported with server-authoritative damage
- [x] Animation system with IK + root motion + events
- [ ] Player can walk, sprint, crouch in test arena
- [ ] Player can light attack, heavy attack, combo chain
- [ ] Player can dodge/roll with i-frames
- [ ] Player can equip and fire a gun (hitscan)
- [ ] Player can ADS with FOV zoom
- [ ] Camera switches between FreeLook / HipFire / ADS / LockOn
- [ ] 3 enemy types: melee, ranged, hybrid
- [ ] AI detects, chases, attacks, strafes
- [ ] Damage system works (health, stagger, death)
- [ ] CLOUT score increases on kills
- [ ] Wanted level increases on combat
- [ ] FishNet networking compiles (multiplayer not required for Phase 1)
- [ ] All 50+ scripts compile clean
