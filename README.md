# CLOUT

> **Build your empire. Earn your name. Rule the streets.**

A criminal ecosystem simulator with deep Souls-like melee combat and tactical shooter gunplay, built in Unity 6 with procedurally generated low-poly aesthetics. From street-level dealing to transnational syndicate operations.

---

## The Pitch

You're nobody. You roll into a procedurally generated low-poly city with $500 and a burner phone. Cook product, hire dealers, buy properties, launder money, fight rival cartels with skill-based combat — and scale from corner hustler to criminal empire operator.

**Schedule 1** proved the crime empire loop sells (8M+ copies). **GTA Online** proved multiplayer crime empires retain players for years. **EVE Online** proved player-driven economies create emergent stories. **Nobody has combined all three with deep combat.** Until now.

---

## Core Pillars

### 1. Empire Building
- **Crafting System** — Cook and mix products with ScriptableObject-driven recipes. Additives modify quality, effects, and street value. 6 station types with risk events (explosions, fume detection).
- **Property Management** — Buy safehouses, labs, growhouses, storefronts, warehouses, nightclubs, auto shops, restaurants. 8 property types with upgrade paths, stash storage, and employee slots.
- **Employee System** — Hire dealers, cooks, guards, drivers, accountants, lookouts, enforcers. Each has skills, loyalty, and betrayal probability.
- **Money Pipeline** — Dirty/clean cash separation. Deals earn dirty, laundering converts to clean, legal purchases require clean. Full transaction ledger with daily/weekly metrics.
- **Dynamic Economy** — Supply/demand pricing, market events, competitor pressure. Multi-layer market formula with elasticity, risk modifiers, and seasonal multipliers.

### 2. Deep Combat
- **Melee** — Souls-like parry, backstab, dodge, combo chains. Stamina-gated, animation-driven.
- **Ranged** — TPS/FPS gunplay with ADS, recoil curves, spread accumulation, ammo management.
- **Hybrid Weapons** — Gun-blades, staffs with projectile attacks, thrown weapons.
- **4 Camera Modes** — FreeLook, HipFire, ADS, LockOn via Cinemachine priority switching.

### 3. Territory Wars (Multiplayer — Phase 4)
- **Zone Control** — City divided into territories with economic value.
- **PvPvE** — Fight rival player empires AND AI cartels for corners, blocks, districts.
- **Influence System** — Control builds through dealing, property ownership, eliminating rivals.
- **Co-op Empire** — Build and manage empires together with friends.

### 4. Living World
- **Wanted System** — 6-tier heat with police AI, evidence, bribery, disguises.
- **CLOUT Score** — Your street reputation unlocks properties, suppliers, employees, respect.
- **NPC Consumers** — AI customers with preferences, addiction mechanics, and loyalty.
- **Procedural City** — Generated city blocks with street grids, 8 distinct building types, roads, sidewalks, and interaction zones.
- **Investigation AI** (planned) — Law enforcement that learns player patterns and builds evidence graphs.

---

## Tech Stack

| System | Technology |
|--------|-----------|
| Engine | Unity 6 (6000.4.0f1) |
| Networking | FishNet v4.7.0 (disabled — singleplayer Phase 2) |
| Render Pipeline | URP 17.4 |
| Input | Unity Input System 1.19 |
| Camera | Cinemachine 3.1.6 |
| AI Navigation | Unity AI Navigation 2.0.6 |
| Level Design | ProBuilder 6.0.9 + Procedural Generation |
| UI | OnGUI (prototyping) → UI Toolkit (production) |
| Art Style | Synty POLYGON + Procedural low-poly |

---

## Current Status: Phase 2 — Steps 1–5 Complete (101 scripts)

### What's Playable

```
Player spawns in procedural 160×160m city block →
  8 procedural buildings (safehouses, labs, growhouses, shops, etc.) →
  3 shop NPCs (ingredient supplier, fence, weapon dealer) →
  3 customer NPCs (seeking product, with addiction/loyalty) →
  3 enemy NPCs (melee thug, ranged shooter, hybrid enforcer) →
  Full Souls-like melee + ranged combat →
  Buy ingredients → Cook at crafting station → Deal to customers →
  Earn dirty cash → Buy/sell at shops → Purchase properties →
  Manage property stash → Upgrade properties → Track finances →
  6-tier heat/wanted system → Street grid with roads and sidewalks
```

### System Inventory

| System | Scripts | Status |
|--------|---------|--------|
| Core State Machine | 7 | ✅ Complete |
| Controller Actions | 7 | ✅ Complete |
| Combat System | 9 | ✅ Complete |
| Camera System | 2 | ✅ Complete |
| Animation | 1 | ✅ Complete |
| Player | 2 | ✅ Complete |
| AI System | 7 | ✅ Complete |
| Network (offline stub) | 1 | ✅ Stub |
| Empire — Crafting | 8 | ✅ Complete |
| Empire — Dealing | 10 | ✅ Complete |
| Empire — Economy | 4 | ✅ Complete |
| Empire — Properties | 5 | ✅ Complete |
| Empire — Employees | 1 | 🟡 Template only |
| World — Police/Heat | 1 | ✅ Basic |
| World — NPCs | 3 | ✅ Complete |
| World — Procedural | 2 | ✅ Complete |
| UI / HUD | 6 | ✅ Complete |
| Editor Tools | 9 | ✅ Complete |
| Utils + Stats + Save | 6 | ✅ Complete |

### Core Gameplay Pipeline

```
Supplier → Buy wholesale ingredients → IngredientInventory
Ingredients → CraftingStation (6 types) → ProductInventory (quality tiers)
Product → Deal to CustomerAI → DealManager negotiation
Deal success → CashManager.EarnDirty() → TransactionLedger
Cash → ShopKeeper (buy more ingredients) OR PropertyManager (buy property)
Property → Stash storage, upgrade slots, employee capacity
Heat accumulates → WantedSystem (6 tiers) → Police response
```

### Next Up: Step 6 — Worker Hiring System

Autonomous dealer AI, cook AI, guard AI. Recruitment, wages, betrayal mechanics. The automation layer that transforms the game from "do everything yourself" to "run an organization."

---

## Architecture

```
Clout/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/           # State machine, interfaces, enums, event bus
│   │   │   ├── Player/         # PlayerStateManager, PlayerInputHandler
│   │   │   ├── AI/             # Enemy AI — detection, patrol, chase, combat, utility scoring
│   │   │   │   └── Actions/    # Pluggable AI state actions
│   │   │   ├── Combat/         # Melee + ranged combat, weapons, damage, ammo
│   │   │   ├── Camera/         # 4-mode Cinemachine camera system
│   │   │   ├── Input/          # Input System bindings
│   │   │   ├── Inventory/      # Item management, equipment
│   │   │   ├── Animation/      # AnimatorHook, IK, root motion
│   │   │   ├── Network/        # Network bootstrapper (offline stub for Phase 2)
│   │   │   ├── Empire/
│   │   │   │   ├── Crafting/   # CraftingStation (6 types), ProductionManager, recipes, ingredients
│   │   │   │   ├── Dealing/    # DealManager, ProductInventory, SupplierNPC, CustomerAI, DealingBootstrapper
│   │   │   │   ├── Properties/ # PropertyManager, PropertyDefinition, Property, stash, upgrades
│   │   │   │   ├── Employees/  # EmployeeDefinition (SO template — workers in Step 6)
│   │   │   │   ├── Economy/    # CashManager (dirty/clean), EconomyManager, TransactionLedger
│   │   │   │   ├── Reputation/ # ReputationManager (CLOUT score)
│   │   │   │   ├── Territory/  # TerritoryManager (zone control)
│   │   │   │   └── Vehicles/   # VehicleManager (placeholder)
│   │   │   ├── World/
│   │   │   │   ├── Police/     # WantedSystem (6-tier heat)
│   │   │   │   ├── NPCs/       # CustomerAI, ShopKeeper, DealInteraction
│   │   │   │   └── *.cs        # ProceduralPropertyBuilder, ProceduralCityBlock
│   │   │   ├── Stats/          # RuntimeStats (health, stamina, poise)
│   │   │   ├── Save/           # SaveManager
│   │   │   ├── UI/
│   │   │   │   ├── HUD/        # CombatHUD
│   │   │   │   ├── Dealing/    # DealUI, SupplierUI
│   │   │   │   ├── Production/ # CraftingUI
│   │   │   │   ├── Economy/    # ShopUI
│   │   │   │   └── Properties/ # PropertyUI
│   │   │   ├── Editor/         # TestArenaBuilder, WeaponAssetFactory, AnimatorSetup, + 6 more
│   │   │   └── Utils/          # EventBus, ObjectPooler, ResourceDatabase, extensions
│   │   ├── Models/Weapons/     # BaseballBat_Low FBX + PBR textures
│   │   ├── ScriptableObjects/  # Weapons, recipes, products, ingredients, employees, properties
│   │   ├── Prefabs/            # Player, NPCs, weapons, props
│   │   └── Scenes/             # Bootstrap, Main, Test
│   └── _Placeholder/           # boxMan FBX (Humanoid), placeholder models
├── Docs/
│   ├── Architecture/           # Phase plans, build spec, roadmap, port maps
│   ├── Design/                 # Criminal Ecosystem 2026 vision, game design doc
│   └── Art/                    # Art direction reference
├── Packages/                   # Unity package manifest
└── ProjectSettings/            # Unity project configuration
```

### Design Patterns
- **State Machine** — All characters (player + AI) share `CharacterStateManager` base
- **Strategy Pattern** — `StateAction` classes are pluggable behaviors composed into states
- **ScriptableObject Architecture** — Items, recipes, NPCs, weapons, properties, employees all SO-driven
- **Singleton Managers** — CashManager, PropertyManager, ProductionManager, TransactionLedger
- **Event Bus** — Type-safe pub/sub for cross-system communication (12+ event types)
- **Utility Theory AI** — Weighted scoring for AI combat decisions
- **Procedural Generation** — City blocks, buildings, streets, interiors
- **Assembly Definitions** — `Clout` (runtime) and `Clout.Editor` (editor-only)

### Heritage
Built on foundations from:
- **Sharp Accent Souls-like** — State machine, melee combat, lock-on, inventory, AI utility theory
- **Sharp Accent TPS/FPS Shooter** — Gunplay, ADS, recoil, stances, FPS camera
- **NullReach** — FishNet networking layer, per-player systems, hybrid combat engine
- **Bastaard Engine** — Style meter philosophy (evolved into CLOUT score)

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
- Procedural 160×160m city block with street grid, roads, sidewalks
- 8 procedural buildings (safehouses, labs, growhouses, shops, warehouse, nightclub, auto shop, restaurant)
- Player with full component stack (StateManager, Combat, Camera, AnimatorHook)
- 3 enemy types: melee thug, ranged shooter, hybrid enforcer
- 3 shop NPCs: ingredient supplier, fence, weapon dealer
- 3 customer NPCs with addiction/loyalty mechanics
- Economy managers: CashManager, TransactionLedger, EconomyManager
- Property system: PropertyManager with 8 purchasable buildings
- Crafting station with production pipeline
- Cinemachine 4-mode camera rig
- Full HUD: health, stamina, CLOUT rank, wanted level, ammo, cash, interaction prompts

---

## Documentation

| Document | Description |
|----------|-------------|
| `Docs/Architecture/BUILD_SPECIFICATION.md` | **Canonical spec v2.0** — 70-section full-stack masterclass specification |
| `Docs/Architecture/GAP_ANALYSIS.md` | Spec v2.0 vs codebase — every section mapped to implementation status |
| `Docs/Architecture/NEXT_STEPS_ROADMAP.md` | Complete development roadmap (Phase 2–6) with catch-up tasks |
| `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md` | Vision doc — criminal universe operating system |
| `Docs/Design/GAME_DESIGN_DOCUMENT.md` | Core game design document |
| `Docs/Design/VIRAL_MECHANICS_2026.md` | Viral mechanics and retention analysis |
| `Docs/Architecture/PHASE_2_MASTERCLASS_PLAN.md` | Phase 2 vertical slice — 10 steps to playable |
| `Docs/Architecture/PHASE_1_EXECUTION_PLAN.md` | Phase 1 completion log |
| `Docs/Architecture/SYNTY_ASSET_LIST.md` | Synty POLYGON asset requirements & integration |
| `Docs/Architecture/SYSTEM_PORT_MAP.md` | Sharp Accent → CLOUT system port analysis |

---

## Development Phases

| Phase | Focus | Status |
|-------|-------|--------|
| Phase 0: Foundation | Codebase merge, architecture, URP | ✅ Complete |
| Phase 1: Core Loop | Combat, inventory, AI, camera, editor tools | ✅ Complete |
| Phase 2: Empire Systems | Dealing, production, economy, properties, workers | 🟡 70% (Steps 1–5 done) |
| Phase 3: Advanced Empire | Laundering pipeline, forensics, rival factions, investigation AI | 🔴 Not started |
| Phase 4: World & Multiplayer | Multi-district city, global supply chain, FishNet re-enable | 🔴 Not started |
| Phase 5: Content & Polish | UI/UX migration, procedural music, accessibility, content pipeline | 🔴 Not started |
| Phase 6: Ship | Scale testing, live ops, Early Access Q3 2026, Full Launch Q4 2027 | 🔴 Not started |

---

## License

Proprietary. All rights reserved.

---

*Built with obsession by TheArchitect × Claude. SlicedLabs 2026.*
