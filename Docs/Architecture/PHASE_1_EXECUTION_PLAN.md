# Clout — Phase 1 Execution Plan

> Port the combat + character foundation from NullReach, then build the empire layer on top.

---

## Phase 1 Goal
A playable character that can walk around a test arena, fight enemies with melee + ranged combat, interact with objects, and see their CLOUT score change.

## Execution Order

### Step 1: Port Core Architecture (Day 1)
Port from NullReach with `NullReach` → `Clout` namespace rename:

| NullReach File | Clout Target | Action |
|------|------|--------|
| Core/StateManager.cs | ✅ Already created | Done |
| Core/State.cs | ✅ Already created | Done |
| Core/StateAction.cs | ✅ Already created | Done |
| Core/CharacterStateManager.cs | ✅ Already created | Done |
| Core/Interfaces.cs | ✅ Already created | Done |
| Stats/RuntimeStats.cs | ✅ Already created | Done |
| Core/ComboInfo.cs | Core/ComboInfo.cs | Port |

### Step 2: Port Controller Actions (Day 1-2)
These are the StateAction implementations that make the character work:

| File | System |
|------|--------|
| Actions/MovePlayerCharacter.cs | Movement with stance speed modifiers |
| Actions/InputHandler.cs | Combat input routing (melee vs ranged context) |
| Actions/HandleRotation.cs | Character rotation logic |
| Actions/HandleStats.cs | Stamina regen, stat monitoring |
| Actions/HandleRollVelocity.cs | Dodge/roll physics |
| Actions/MonitorInteraction.cs | Animation-to-state transition monitor |
| Actions/InputsForCombo.cs | Combo chain input buffering |

### Step 3: Port Combat System (Day 2-3)
| File | System |
|------|--------|
| Combat/WeaponItem.cs + RangedWeaponItem.cs | Weapon ScriptableObjects |
| Combat/AttackAction.cs | Melee attack execution |
| Combat/RangedAttackAction.cs | Ranged attack with RangedWeaponHook |
| Combat/DamageCollider.cs | Melee hit detection |
| Combat/Projectile.cs | Projectile physics |
| Combat/RangedWeaponHook.cs | Gun behavior (ammo, spread, ADS) |
| Combat/RecoilController.cs | Camera recoil |
| Combat/AmmoCacheManager.cs | Ammo reserve management |
| Combat/AmmoDefinition.cs | Ammo type definitions |
| Combat/WeaponHolderManager.cs | Weapon model management |
| Combat/WeaponHolderHook.cs | Hand attachment points |

### Step 4: Port Camera System (Day 3)
| File | System |
|------|--------|
| Camera/CameraManager.cs | 4-mode Cinemachine camera |
| Camera/CameraCollision.cs | SphereCast collision prevention |

### Step 5: Port Animation System (Day 3-4)
| File | System |
|------|--------|
| Animation/AnimatorHook.cs | IK + animation events |
| Player/PlayerStateManager.cs | ✅ Already created (needs combat wiring) |
| Player/PlayerInputHandler.cs | ✅ Already created |

### Step 6: Port AI System (Day 4-5)
| File | System |
|------|--------|
| AI/AIStateManager.cs | Base enemy AI |
| AI/Actions/AIDetection.cs | Detection + FOV |
| AI/Actions/AIChaseTarget.cs | NavMesh pursuit |
| AI/Actions/AICombatSelector.cs | Melee vs ranged decision |
| AI/Actions/AIRangedAttack.cs | Ranged AI behavior |
| AI/Actions/AIActionScoring.cs | Utility theory scoring |
| AI/Actions/AIPatrol.cs | Patrol behavior |

### Step 7: Port Network Layer (Day 5-6)
| File | System |
|------|--------|
| Network/NetworkBootstrapper.cs | FishNet initialization |
| Network/NetworkAnimatorSync.cs | Animation replication |
| Network/NetworkDamageHandler.cs | Server damage validation |
| Network/PlayerSpawnManager.cs | Player spawning |

### Step 8: Build Test Arena (Day 6-7)
- Editor script to create test scene
- Player prefab with all components
- 3 enemy NPCs (melee, ranged, hybrid)
- Ground plane with NavMesh
- Cinemachine camera rig
- Basic HUD (health, CLOUT, ammo)

### Step 9: Wire Empire Systems (Day 7-8)
- Connect ReputationManager to combat events
- Connect WantedSystem to combat actions (gunfire = heat)
- Add CLOUT rank display to HUD
- Add wanted level indicator to HUD

---

## Port Checklist

Total files to port: ~35 scripts from NullReach
Already created: 22 scripts (foundation + empire)
Remaining: ~13 scripts (combat + AI + network)
New for Clout: ~15 scripts (empire systems — already created)

**Estimated total codebase at Phase 1 complete: ~50 scripts**

---

## Validation Criteria

Phase 1 is DONE when:
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
