# CLOUT

> **Build your empire. Earn your name. Rule the streets.**

A multiplayer crime empire simulator with deep Souls-like melee combat and tactical shooter gunplay, built in Unity 6 with POLYGON low-poly aesthetics.

---

## The Pitch

You're nobody. You roll into a boxy low-poly city with nothing. Cook product, hire dealers, buy properties, launder money, fight rival cartels with skill-based combat — and do it all in multiplayer co-op or competitive territory wars.

**Schedule 1** proved the crime empire loop sells (8M+ copies). **GTA Online** proved multiplayer crime empires retain players for years. **Nobody has combined both with deep combat.** Until now.

---

## Core Pillars

### 1. Empire Building
- **Crafting System** — Cook and mix products with ScriptableObject-driven recipes. Additives modify quality, effects, and street value.
- **Property Management** — Buy safehouses, labs, growhouses, storefronts. Upgrade capacity and security.
- **Employee System** — Hire dealers, cooks, guards, drivers. Each has skills, loyalty, and risk of betrayal.
- **Money Laundering** — Run legit business fronts to clean your cash. Restaurants, auto shops, nightclubs.
- **Dynamic Economy** — Supply/demand pricing, market events, competitor pressure.

### 2. Deep Combat
- **Melee** — Souls-like parry, backstab, dodge, combo chains. Stamina-gated, animation-driven.
- **Ranged** — TPS/FPS gunplay with ADS, recoil curves, spread accumulation, ammo management.
- **Hybrid Weapons** — Gun-blades, staffs with projectile attacks, thrown weapons.
- **4 Camera Modes** — FreeLook, HipFire, ADS, LockOn via Cinemachine priority switching.

### 3. Territory Wars (Multiplayer)
- **Zone Control** — City divided into territories with economic value.
- **PvPvE** — Fight rival player empires AND AI cartels for corners, blocks, districts.
- **Influence System** — Control builds through dealing, property ownership, eliminating rivals.
- **Co-op Empire** — Build and manage empires together with friends.

### 4. Living World
- **Wanted System** — 6-tier heat with police AI, evidence, bribery, disguises.
- **CLOUT Score** — Your street reputation unlocks properties, suppliers, employees, respect.
- **Day/Night Cycle** — Different activities, dangers, and opportunities by time of day.
- **NPC Consumers** — AI customers with preferences, addiction, and loyalty.
- **Rival Cartels** — AI-driven empires that expand, fight, and react to your moves.

---

## Tech Stack

| System | Technology |
|--------|-----------|
| Engine | Unity 6 (6000.4.0f1) |
| Networking | FishNet v4.7.0 |
| Render Pipeline | URP 17.4 |
| Input | Unity Input System 1.19 |
| Camera | Cinemachine 3.1.6 |
| AI Navigation | Unity AI Navigation 2.0.6 |
| Level Design | ProBuilder 6.0.9 |
| UI | TextMeshPro |
| Art Style | Synty POLYGON |

---

## Current Status: Phase 2 In Progress 🔨

**80+ scripts** across all systems. Combat foundation + dealing system operational.

### What's Built

| System | Scripts | Status |
|--------|---------|--------|
| Core State Machine | 7 | ✅ Complete — StateManager, State, StateAction, CharacterStateManager, Interfaces, Enums, ComboInfo |
| Controller Actions | 7 | ✅ Complete — Movement, InputHandler, Rotation, Stats, Roll, Interaction, Combo |
| Combat System | 9 | ✅ Complete — Melee attacks, ranged hitscan, damage colliders, projectiles, recoil, ammo |
| Camera System | 2 | ✅ Complete — 4-mode Cinemachine (FreeLook/HipFire/ADS/LockOn), collision |
| Animation | 1 | ✅ Complete — AnimatorHook with IK, root motion, animation events |
| Player | 2 | ✅ Complete — PlayerStateManager, PlayerInputHandler |
| AI System | 7 | ✅ Complete — Detection, patrol, chase, ranged attack, combat selector, utility scoring |
| Network | 4 | ✅ Complete — FishNet bootstrapper, anim sync, damage handler, spawn manager |
| Empire Core | 7 | ✅ Complete — Crafting, properties, employees, economy, reputation, territory, vehicles |
| Empire Dealing | 7 | ✅ Complete — ProductInventory, DealManager, SupplierNPC, CustomerAI, DealUI, SupplierUI, DealingBootstrapper |
| World | 4 | ✅ Complete — Wanted system (6-tier heat), police response, DealInteraction, NPC customer AI |
| Inventory | 2 | ✅ Complete — Item management, equipment |
| Stats | 1 | ✅ Complete — RuntimeStats with FishNet SyncVar<T> |
| Editor Tools | 7 | ✅ Complete — TestArenaBuilder, PlayerPrefabBuilder, WeaponAssetFactory, DealingSystemFactory, AnimatorSetup, EditorShaderHelper, URPSetup |
| UI/HUD | 3 | ✅ Complete — CombatHUD (health, stamina, CLOUT, wanted, ammo, cash, interaction prompts), DealUI, SupplierUI |

### Dealing Pipeline (Wired)
```
Supplier → Player buys wholesale product → ProductInventory
Player approaches Customer NPC → DealManager negotiation
Quality tiers affect pricing: Trash(0.5x) → Street(1x) → Mid(1.5x) → Fire(2.5x) → Pure(4x)
Successful deal → Cash earned + CLOUT gained + Heat generated
Customer addiction/loyalty → repeat business
Snitch risk → police heat spikes
```

### Empire Event Pipeline (Wired)
```
Kill Enemy     → +25 CLOUT, +80 heat
Kill Police    → +50 CLOUT, +150 heat
Gunfire        → +30 heat per shot
Melee Assault  → +40 heat (civilian), +100 heat (police)
Ranged Assault → +40 heat (civilian), +100 heat (police)
Complete Deal  → +CLOUT (scales with value), +heat (scales with product type)
```

---

## Architecture

```
Clout/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/           # State machine, interfaces, enums, bootstrapper
│   │   │   ├── Player/         # Player controller, input handling
│   │   │   ├── AI/             # Enemy AI — detection, patrol, chase, combat, scoring
│   │   │   │   └── Actions/    # Pluggable AI state actions
│   │   │   ├── Combat/         # Melee + ranged combat, weapons, damage, ammo
│   │   │   ├── Camera/         # 4-mode Cinemachine camera system
│   │   │   ├── Input/          # Input System bindings
│   │   │   ├── Inventory/      # Item management, equipment
│   │   │   ├── Animation/      # AnimatorHook, IK, root motion
│   │   │   ├── Network/        # FishNet networking layer
│   │   │   ├── Empire/
│   │   │   │   ├── Crafting/   # Recipe system, cooking, mixing
│   │   │   │   ├── Dealing/    # Product inventory, deal manager, supplier/customer systems
│   │   │   │   ├── Properties/ # Property purchase, upgrade, management
│   │   │   │   ├── Employees/  # Hiring, loyalty, skill progression
│   │   │   │   ├── Economy/    # Dynamic pricing, supply/demand
│   │   │   │   ├── Reputation/ # CLOUT score, street rep
│   │   │   │   ├── Territory/  # Zone control, influence, wars
│   │   │   │   └── Vehicles/   # Vehicle ownership, mods
│   │   │   ├── World/
│   │   │   │   ├── Police/     # Wanted system, heat, raids
│   │   │   │   └── NPCs/       # CustomerAI, SupplierNPC, DealInteraction
│   │   │   ├── Stats/          # Health, stamina, skills (SyncVar<T>)
│   │   │   ├── UI/
│   │   │   │   ├── HUD/        # CombatHUD (health, stamina, CLOUT, cash, prompts)
│   │   │   │   └── Dealing/    # DealUI, SupplierUI
│   │   │   ├── Editor/         # TestArenaBuilder, PlayerPrefabBuilder, WeaponAssetFactory, DealingSystemFactory, URPSetup
│   │   │   └── Utils/          # Helpers, extensions
│   │   ├── ScriptableObjects/  # Weapons, recipes, NPCs, items
│   │   ├── Prefabs/            # Player, NPCs, weapons, props
│   │   └── Scenes/             # Bootstrap, Main, Test
│   └── _ThirdParty/            # Synty, FishNet, plugins
├── Docs/
│   └── Architecture/           # Design docs, build spec, asset list, phase plans
├── Packages/                   # Unity package manifest
└── ProjectSettings/            # Unity project configuration
```

### Design Patterns
- **State Machine** — All characters (player + AI) share `CharacterStateManager` base
- **Strategy Pattern** — `StateAction` classes are pluggable behaviors composed into states
- **ScriptableObject Architecture** — Items, recipes, NPCs, weapons, properties all SO-driven
- **Server Authority** — FishNet `[Server]`/`[ServerRpc]` for all game-critical logic
- **SyncVar<T> Replication** — FishNet 4.7 generic SyncVars for client-visible state
- **Utility Theory AI** — Weighted scoring for AI combat decisions (distance, angle, health, ammo, aggression)
- **Assembly Definitions** — `Clout` (runtime) and `Clout.Editor` (editor-only) for fast iteration

### Heritage
Built on foundations from:
- **Sharp Accent Souls-like** — State machine, melee combat, lock-on, inventory, AI utility theory
- **Sharp Accent TPS/FPS Shooter** — Gunplay, ADS, recoil, stances, FPS camera, multiplayer
- **NullReach** — FishNet networking layer, per-player systems, hybrid combat engine
- **Bastaard Engine** — Style meter philosophy (evolved into CLOUT score), mixed melee+ranged

---

## Getting Started

### Prerequisites
- Unity 6 (6000.4.0f1+)
- Git LFS installed (`git lfs install`)

### Setup
```bash
git clone git@github.com:SL1C3D-L4BS/clout.git Clout
cd Clout
git lfs pull
```

Open in Unity Hub. The project targets Unity 6 with URP.

### First Run
1. Open Unity Hub → Add Project → select `Clout/` folder
2. Wait for package resolution and import
3. Use menu: **Clout > Build Test Arena** to generate the test scene
4. Press Play

### Build Test Arena
The editor tool (`Clout > Build Test Arena`) programmatically creates:
- Ground plane with baked NavMesh
- Cover objects and boundary walls
- Player with full component stack (StateManager, Combat, Camera, Network)
- 3 enemy types: melee thug, ranged shooter, hybrid enforcer
- Cinemachine 4-mode camera rig
- Combat HUD (health, stamina, CLOUT rank, wanted level, ammo, crosshair)

---

## Documentation

| Document | Description |
|----------|-------------|
| `Docs/Architecture/BUILD_SPECIFICATION.md` | Full 30-section game design document |
| `Docs/Architecture/SYNTY_ASSET_LIST.md` | Synty POLYGON asset requirements & integration guide |
| `Docs/Architecture/PHASE_1_EXECUTION_PLAN.md` | Phase 1 step-by-step completion log |
| `Docs/Architecture/PHASE_2_MASTERCLASS_PLAN.md` | Phase 2 vertical slice sprint — 10 steps to playable |
| `Docs/Architecture/SYSTEM_PORT_MAP.md` | Sharp Accent → CLOUT system port analysis |

---

## License

Proprietary. All rights reserved.

---

*Built with obsession by TheArchitect × Claude. SlicedLabs 2026.*
