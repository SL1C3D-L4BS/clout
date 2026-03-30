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

## From Sharp Accent TPS/FPS (Reference Only)

These systems are studied and reimplemented, not directly ported:

| System | SA Source | What We Take |
|--------|----------|-------------|
| FPS Camera Mode | `FPSHandler.cs` (lessons 1-17) | First-person camera logic, weapon sway |
| TPS Camera Mode | `TPSHandler.cs` (lessons 1-6) | Third-person shoulder cam, over-shoulder aim |
| Match System | `MatchManager.cs`, `DeathMatch.cs` | Session management patterns for territory wars |
| Leaderboards | `LeaderboardsManager.cs` | Ranking system patterns for CLOUT leaderboard |
| Inventory Manager | `InventoryManager.cs` (lessons 1-15) | Slot-based inventory UI patterns |
| Item System | `Item.cs`, `ItemAction.cs` | ScriptableObject item architecture |
| Network Sync | `NetworkManager.cs`, `NetworkPrint.cs` | FishNet patterns (same networking stack) |
| Ballistics | `Ballistics.cs` | Projectile physics, bullet drop |
| Crosshair | `Crosshair.cs` | Dynamic crosshair spread visualization |
| Level Manager | `LevelManager.cs` | Scene loading patterns |
| Cloth System | `ClothManager.cs`, `ClothItem.cs` | Character customization framework |
| Consumables | `ConsumableHolder.cs`, `Consumable.cs` | Item usage patterns (adapt for drug use) |
| Save System | `SaveableController.cs`, `Serialization.cs` | Save/load architecture |
| Object Pooling | `ObjectPooler.cs`, `ObjectPoolAsset.cs` | Bullet/VFX pooling |
| Door System | `DoorHook.cs` | Interactive world objects |
| Destructibles | `DestructiblePropObject.cs` | Breakable world props |
| Foot IK | `FootIK.cs` | Terrain adaptation |
| Screen Shake | `ScreenShakeHandler.cs` | Combat feedback |

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

## Transfer Score

- **Direct ports:** ~15 scripts (foundation)
- **Heavy adaptation:** ~15 scripts (combat + AI)
- **Reference patterns:** ~20 systems studied from SA-TPS/FPS
- **Built fresh:** ~15 new systems (empire + world)

**Estimated reuse: ~45% of total codebase** from existing projects.
The other 55% is the empire simulation layer — which is the actual game.
