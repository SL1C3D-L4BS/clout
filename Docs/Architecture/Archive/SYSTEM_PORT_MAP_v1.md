# Clout — System Port Map

> What transfers from NullReach / Sharp Accent and what needs to be built fresh.

---

## Direct Ports (NullReach → Clout)

These systems transfer almost 1:1 with namespace changes:

| System | NullReach Source | Clout Target | Changes Needed |
|--------|-----------------|-------------|----------------|
| State Machine | `Core/StateManager.cs` | `Core/StateManager.cs` | Namespace rename |
| State + StateAction | `Core/State.cs`, `Core/StateAction.cs` | Same | Namespace rename |
| CharacterStateManager | `Core/CharacterStateManager.cs` | Same | Add empire fields |
| Damage System | `Core/Interfaces.cs` | Same | Add Ballistic/Explosive types |
| RuntimeStats | `Stats/RuntimeStats.cs` | Same | Add crime-relevant stats |
| FishNet Networking | `Network/*.cs` | Same | Direct port |
| NetworkAnimatorSync | `Network/NetworkAnimatorSync.cs` | Same | Direct port |
| NetworkDamageHandler | `Network/NetworkDamageHandler.cs` | Same | Direct port |
| PlayerSpawnManager | `Network/PlayerSpawnManager.cs` | Same | Direct port |
| AnimatorHook | `Animation/AnimatorHook.cs` | Same | Port IK + events |
| CameraManager | `Controller/CameraManager.cs` | `Camera/CameraManager.cs` | Port 4-mode system |
| CameraCollision | `Controller/CameraCollision.cs` | Same | Direct port |
| MovePlayerCharacter | `Controller/Actions/MovePlayerCharacter.cs` | Same | Add vehicle check |
| InputHandler | `Controller/Actions/InputHandler.cs` | Same | Port shooter inputs |
| PlayerInputHandler | `Controller/PlayerInputHandler.cs` | Same | Direct port |

## Heavy Adaptation (NullReach → Clout)

These need significant modification but the foundation ports:

| System | Source | Changes |
|--------|--------|---------|
| WeaponItem / RangedWeaponItem | `Combat/WeaponItem.cs` | Add crime weapons (bat, pipe, machete, etc.) |
| RangedWeaponHook | `Combat/RangedWeaponHook.cs` | Direct port, adjust for crime weapons |
| RangedAttackAction | `Combat/RangedAttackAction.cs` | Port with RangedWeaponHook integration |
| AttackAction | `Combat/AttackAction.cs` | Add melee crime weapon animations |
| DamageCollider | `Combat/DamageCollider.cs` | Direct port |
| Projectile | `Combat/Projectile.cs` | Direct port |
| AmmoDefinition | `Combat/AmmoDefinition.cs` | Direct port |
| AmmoCacheManager | `Combat/AmmoCacheManager.cs` | Direct port |
| RecoilController | `Combat/RecoilController.cs` | Direct port |
| AIStateManager | `AI/AIStateManager.cs` | Fork into PoliceAI, RivalAI, CivilianAI |
| AIDetection | `AI/Actions/AIDetection.cs` | Port + add wanted-level awareness |
| AIChaseTarget | `AI/Actions/AIChaseTarget.cs` | Port + adapt for police pursuit |
| AIActionScoring | `AI/Actions/AIActionScoring.cs` | Port utility theory, adapt weights |
| AICombatSelector | `AI/Actions/AICombatSelector.cs` | Port + add flee behavior |
| AIRangedAttack | `AI/Actions/AIRangedAttack.cs` | Direct port |
| PlayerStateManager | `Controller/PlayerStateManager.cs` | Add empire interaction, vehicle state |

## From Sharp Accent TPS/FPS (Reference + Adaptation)

The TPS/FPS project contains 600+ versioned files across lessons 150-169.
These systems are studied, adapted, and reimplemented:

### HIGH Priority — Port with Adaptation
| System | SA Source | What We Take | Crime Game Use |
|--------|----------|-------------|----------------|
| FPS Camera Mode | `FPSHandler.cs` (17 versions) | First-person camera, weapon sway | Optional FPS toggle |
| TPS Camera Mode | `TPSHandler.cs` (6 versions) | Shoulder cam, over-shoulder aim | Primary camera mode |
| Cloth System | `ClothManager.cs` + `ClothItem.cs` | Mesh swapping, material assignment | Gang colors, disguises, outfits |
| Consumables | `Consumable.cs` + `ConsumableHolder.cs` + `ConsumablesHook.cs` | Usage tracking, animation hooks | Health items, buffs, drugs |
| Save System | `SaveableController.cs` + `SaveableMonobehavior.cs` + `Serialization.cs` | Position/state persistence | Empire state, character progress |
| Object Pooling | `ObjectPooler.cs` + `ObjectPoolAsset.cs` | Static pool with SO config | Bullets, VFX, NPC spawning |
| Player Profile | `PlayerProfile.cs` (7 versions) | Persistent player data | CLOUT score, empire state, stats |
| Destructibles | `DestructiblePropObject.cs` (2 versions) | IDamageable props | Breakable windows, doors, crates |
| Interactions | `DoorHook.cs`, `PickableHook.cs`, `BonefireHook.cs` | World object interaction | Doors, loot, safe houses |

### MEDIUM Priority — Reference Patterns
| System | SA Source | What We Take | Crime Game Use |
|--------|----------|-------------|----------------|
| Match System | `MatchManager.cs` (11 versions) + `DeathMatch.cs` + `DuelMatch.cs` | Session management, scoring | Territory war sessions |
| Leaderboards | `LeaderboardsManager.cs` (3 versions) + `LeaderboardEntry.cs` | Ranking display | CLOUT leaderboard |
| Inventory UI | `UIInventoryManager.cs` (4 versions) + `UISlot.cs` (4 versions) | Slot-based UI patterns | Inventory screen |
| UI Navigation | `UIManager.cs` (9 versions) + `NavigatableGroupManager.cs` | Menu input navigation | All menu screens |
| Ballistics | `Ballistics.cs` (7 versions) + `BulletProjectile.cs` + `BulletLine.cs` | Bullet physics, tracers | Realistic gunplay |
| Crosshair | `Crosshair.cs` (2 versions) | Dynamic spread visualization | Hip-fire/ADS crosshair |
| Level Manager | `LevelManager.cs` (5 versions) + `LevelTemplate.cs` | Scene loading patterns | City district loading |
| Foot IK | `FootIK.cs` | Terrain foot adaptation | Character polish |
| Screen Shake | `ScreenShakeHandler.cs` + `ShakeObject.cs` | Camera/object shake | Combat feedback, explosions |
| Icon Maker | `IconMakerActual.cs` + `IconMakerAsset.cs` | 3D model → UI icon | Item icon generation |

### LOW Priority — Study Only
| System | SA Source | Notes |
|--------|----------|-------|
| Network Manager | `NetworkManager.cs` (2 versions) | We use FishNet instead |
| Master Light Control | `MasterLightControl.cs` | Lighting reference |
| Copy Rotation | `CopyRotation.cs` | IK utility |
| Sway Object | `SwayObject.cs` | Weapon sway reference |

## Built Fresh for Clout

These are new systems with no NullReach/SA equivalent:

| System | Priority | Complexity |
|--------|----------|-----------|
| Crafting/Cooking System | P0 | High |
| Property Management | P0 | High |
| Employee System | P0 | Medium |
| Dynamic Economy | P1 | High |
| Territory Control | P1 | High |
| CLOUT/Reputation | P0 | Medium |
| Wanted/Police System | P0 | High |
| Vehicle System | P2 | Very High |
| Money Laundering | P1 | Medium |
| NPC Consumer AI | P1 | Medium |
| Rival Cartel AI | P2 | High |
| Day/Night Cycle | P2 | Low |
| Traffic System | P3 | Medium |
| Disguise System | P3 | Low |
| Phone/Communication UI | P1 | Medium |

## Transfer Score (Validated by Deep Analysis)

Three research agents analyzed all codebases in detail:
- **NullReach**: 46 scripts, 8 core systems — confirmed 60-75% architectural reuse
- **Sharp Accent TPS/FPS**: 600+ versioned files across lessons 150-169 — 18+ reusable systems cataloged
- **Sharp Accent Souls-like**: State machine foundation verified as combat-agnostic (weapon→tool swap needs zero arch changes)

| Category | Scripts | Reuse Level |
|----------|---------|-------------|
| Direct ports (NullReach) | ~15 | 100% architecture, namespace rename |
| Heavy adaptation (NullReach) | ~15 | 70-85% code, crime-specific tuning |
| Reference ports (SA-TPS/FPS) | ~20 | Patterns studied, reimplemented clean |
| Already built fresh (Clout) | 22 | Empire + world systems |
| Still needed fresh | ~10 | Vehicle, disguise, phone UI, dialogue |

**Confirmed reuse: 60-75% of combat/character/AI/network codebase.**
**The 25-40% that's new IS the game** — empire simulation, territory wars, economy.
This is the ideal ratio: proven foundation + original gameplay.
