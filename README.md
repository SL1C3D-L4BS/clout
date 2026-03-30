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
| Asset Pipeline | Addressables |
| Art Style | Synty POLYGON |

---

## Architecture

```
Clout/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/           # State machine, interfaces, enums, bootstrapper
│   │   │   ├── Player/         # Player controller, input handling
│   │   │   ├── AI/             # Enemy AI, NPC behavior, GOAP
│   │   │   ├── Combat/         # Melee + ranged combat systems
│   │   │   ├── Camera/         # 4-mode Cinemachine camera system
│   │   │   ├── Input/          # Input System bindings
│   │   │   ├── Inventory/      # Item management, equipment
│   │   │   ├── Animation/      # AnimatorHook, IK, root motion
│   │   │   ├── Network/        # FishNet networking layer
│   │   │   ├── Empire/
│   │   │   │   ├── Crafting/   # Recipe system, cooking, mixing
│   │   │   │   ├── Properties/ # Property purchase, upgrade, management
│   │   │   │   ├── Employees/  # Hiring, loyalty, skill progression
│   │   │   │   ├── Economy/    # Dynamic pricing, supply/demand
│   │   │   │   ├── Reputation/ # CLOUT score, street rep
│   │   │   │   ├── Territory/  # Zone control, influence, wars
│   │   │   │   └── Vehicles/   # Vehicle ownership, mods
│   │   │   ├── World/
│   │   │   │   ├── Police/     # Wanted system, heat, raids
│   │   │   │   ├── NPCs/       # Civilian AI, consumers, dealers
│   │   │   │   ├── Traffic/    # Vehicle AI, traffic system
│   │   │   │   ├── DayNight/   # Time of day, lighting
│   │   │   │   └── Events/     # Random events, opportunities
│   │   │   ├── Stats/          # Health, stamina, skills
│   │   │   ├── Save/           # Serialization, cloud saves
│   │   │   ├── Audio/          # Audio manager, mixer control
│   │   │   ├── UI/             # HUD, menus, inventory UI
│   │   │   └── Utils/          # Helpers, editor tools
│   │   ├── ScriptableObjects/  # All SO assets (weapons, recipes, NPCs, etc.)
│   │   ├── Art/                # Models, materials, textures, VFX, UI art
│   │   ├── Animations/         # Clips, blend trees, controllers
│   │   ├── Audio/              # Music, SFX, voice, mixers
│   │   ├── Prefabs/            # Player, NPCs, weapons, vehicles, props
│   │   ├── Scenes/             # Main, test, UI, additive scenes
│   │   └── Settings/           # Rendering, input, audio settings
│   ├── _ThirdParty/            # Synty, FishNet, plugins
│   ├── StreamingAssets/
│   └── Resources/
├── Docs/                       # Design docs, architecture notes
├── Packages/                   # Unity package manifest
└── ProjectSettings/            # Unity project configuration
```

### Design Patterns
- **State Machine** — All characters (player + AI) share one `CharacterStateManager` base
- **Strategy Pattern** — `StateAction` classes are pluggable behaviors composed into states
- **ScriptableObject Architecture** — Items, recipes, NPCs, weapons, properties all SO-driven
- **Server Authority** — FishNet `[Server]`/`[ServerRpc]` for all game-critical logic
- **SyncVar Replication** — Client-visible state synced via FishNet SyncVars
- **Assembly Definitions** — `Clout` and `Clout.Editor` for fast iteration

### Heritage
Built on foundations from:
- **Sharp Accent Souls-like** — State machine, melee combat, lock-on, inventory, AI utility theory
- **Sharp Accent TPS/FPS Shooter** — Gunplay, ADS, recoil, stances, FPS camera, multiplayer matches
- **NullReach** — FishNet networking layer, per-player systems, hybrid combat engine
- **Bastaard Engine** — Style meter philosophy (evolved into CLOUT score), mixed melee+ranged

---

## Development Phases

### Phase 0: Foundation ✅
- [x] Unity 6 project setup
- [x] AAA folder structure
- [x] Git repository with LFS
- [x] Core state machine architecture
- [x] FishNet networking foundation
- [x] Assembly definitions

### Phase 1: Character Controller
- [ ] Player state machine (locomotion, combat, interact, vehicle)
- [ ] Input System bindings (keyboard/mouse + gamepad)
- [ ] 4-mode Cinemachine camera
- [ ] Basic melee combat (light/heavy/combo)
- [ ] Basic ranged combat (hitscan + projectile)
- [ ] AnimatorHook with IK

### Phase 2: Empire Core
- [ ] Crafting system (recipes, ingredients, products)
- [ ] Property system (buy, upgrade, manage)
- [ ] Employee system (hire, assign, manage)
- [ ] Basic economy (static pricing)
- [ ] Inventory system

### Phase 3: World Systems
- [ ] Wanted system (heat, police AI, evidence)
- [ ] CLOUT reputation system
- [ ] Day/night cycle
- [ ] NPC consumers (buy product, have preferences)
- [ ] Basic vehicle system

### Phase 4: Multiplayer
- [ ] Player spawning and session management
- [ ] Territory system (zone control, influence)
- [ ] PvP combat synchronization
- [ ] Co-op empire sharing
- [ ] Lobby / matchmaking

### Phase 5: Content & Polish
- [ ] Synty POLYGON art integration
- [ ] Full weapon set (melee + ranged)
- [ ] Property interiors
- [ ] Advanced AI (rival cartels, police tactics)
- [ ] UI/UX polish
- [ ] Audio design
- [ ] Save/load system

---

## Getting Started

### Prerequisites
- Unity 6 (6000.4.0f1+)
- Git LFS installed (`git lfs install`)

### Setup
```bash
git clone <repo-url> Clout
cd Clout
git lfs pull
```

Open in Unity Hub. The project targets Unity 6 with URP.

### First Run
1. Open Unity Hub → Add Project → Select `Clout/` folder
2. Wait for package resolution and import
3. Open `Assets/_Project/Scenes/Test/TestArena.unity`
4. Press Play

---

## License

Proprietary. All rights reserved.

---

*Built with obsession by TheArchitect. 2026.*
