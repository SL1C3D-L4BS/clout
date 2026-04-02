# CLOUT

**Build an empire. Control the streets. Own the game.**

A criminal ecosystem simulator built in Unity 6 where every deal, betrayal, and territory war emerges from interconnected systems -- not scripted events.

---

## The Pitch

CLOUT drops players into a living open world where they build a criminal empire from nothing. Cook product, hire workers, acquire properties, manage supply chains, and defend territory -- all while evading an escalating law enforcement response. Every system feeds into every other: your economy drives your reputation, your reputation attracts better workers, your workers expand your territory, and your territory grows your economy. One bad decision cascades through all of it.

---

## Core Pillars

| Pillar | Description |
|---|---|
| **Empire Building** | Acquire 8 property types, recruit autonomous workers (dealers, cooks, guards), establish supply chains with 6-type crafting, and scale operations across procedurally generated districts. Full transaction ledger with dirty/clean cash separation. |
| **Deep Combat** | Souls-like melee with combo chains, parry, backstab, and dodge rolls. Ranged combat with ADS, recoil curves, and ammo management. 4-mode Cinemachine camera (Exploration, Combat Lock-On, Aim, Cinematic). |
| **Territory Wars** | Compete for district control through influence, dealing presence, and force. Workers autonomously defend and expand turf. Rival factions contest your operations. |
| **Living World** | Autonomous NPC customers with preferences and loyalty. Dynamic supply/demand pricing. 6-tier wanted system with police patrols, investigations, pursuits, and property raids. Witness-driven evidence that degrades over time. 7-phase procedural district generation. |

---

## Current Status

| Phase | Name | Steps | Status |
|---|---|---|---|
| 0 | Foundation | -- | COMPLETE |
| 1 | Core Loop | -- | COMPLETE (62 scripts) |
| 2 | Empire Systems | 10/10 | COMPLETE (+77 scripts) |
| 3 | Advanced Empire | 2/5 | IN PROGRESS |

**Total: ~151 scripts** across 19 system modules. Phase 3 Steps 11-12 complete.

Phase 2 delivered: crafting pipeline, property management, employee AI with betrayal mechanics, dynamic economy, dealing systems, 6-tier wanted/police systems, procedural districts, phone UI empire hub, game flow management with milestones and tutorials, and GameBalanceConfig with 50+ tunable values across 3 difficulty presets.

Phase 3 Step 11 delivered: Full 5-stage money laundering pipeline (Placement -> Layering -> Integration -> Cooling -> Complete), 5 front business types with simulated revenue and suspicion tracking, 5 laundering methods (Structuring, Smurfing, RealEstate, CashIntensive, Crypto), 4-stage IRS investigation system (Flag -> Investigation -> Audit -> Seizure), laundering dashboard UI (L key), and Accountant worker role activation.

Phase 3 Step 12 delivered: 512-dimensional forensic batch signatures with cosine similarity clustering. Every crafted batch carries a unique forensic fingerprint traceable to its origin facility. ForensicLabAI processes seized evidence through a queue-based pipeline. SignatureDatabase clusters signatures by facility origin with 60-day degradation. SignatureScrubber equipment (3 levels) lets players trade yield for anonymity. Full integration: CraftingStation -> ProductInventory -> DealManager -> ForensicLabAI evidence chain. Undercover buy mechanic captures signatures from street deals. Forensics dashboard (F key).

---

## System Inventory

| System | Scripts | Key Components |
|---|---|---|
| Core | 13 | GameBootstrapper, CloutGameFlowManager, GameBalanceConfig, StateManager, CharacterStateManager, PerformanceMonitor |
| Actions | 7 | InputHandler, MovePlayerCharacter, HandleRotation, InputsForCombo, MonitorInteraction, HandleStats, HandleRollVelocity |
| Combat | 12 | AttackAction, RangedAttackAction, WeaponHolderManager, DamageCollider, Projectile, RecoilController, AmmoCacheManager |
| AI | 7 | AIStateManager, AIDetection, AIPatrol, AIChaseTarget, AICombatSelector, AIRangedAttack, AIActionScoring |
| Player | 3 | PlayerStateManager, PlayerInputHandler, AppearanceManager |
| Camera | 2 | CameraManager, CameraCollision |
| Animation | 1 | AnimatorHook |
| Network | 4 | NetworkBootstrapper, PlayerSpawnManager, NetworkDamageHandler, NetworkAnimatorSync |
| Inventory | 3 | InventoryManager, ItemDefinition, ConsumableItem |
| Empire / Crafting | 7 | ProductionManager, CraftingStation, CraftingBootstrapper, RecipeDefinition, ProductDefinition, IngredientDefinition, IngredientInventory |
| Empire / Dealing | 4 | DealManager, DealingBootstrapper, SupplierDefinition, ProductInventory |
| Empire / Properties | 4 | PropertyManager, Property, PropertyDefinition |
| Empire / Employees | 10 | WorkerManager, RecruitmentManager, DealerAI, CookAI, GuardAI, WorkerInstance, EmployeeDefinition, HireUI, WorkerManagementUI |
| Empire / Economy | 3 | EconomyManager, CashManager, TransactionLedger |
| Empire / Laundering | 4 | LaunderingManager, FrontBusiness, LaunderingMethod (SO), IRSInvestigation |
| Forensics | 5 | BatchSignature, SignatureDatabase, ForensicLabAI, SignatureScrubber, ForensicsUI |
| Empire / Reputation | 1 | ReputationManager |
| Empire / Territory | 1 | TerritoryManager |
| World / Police | 6 | WantedSystem, WitnessSystem, PoliceOfficerAI, HeatResponseManager, PropertyRaidSystem, PoliceStation |
| World / Districts | 4 | DistrictManager, ProceduralDistrictGenerator, DistrictDefinition, DistrictTriggerZone |
| World / NPCs | 4 | CustomerAI, SupplierNPC, ShopKeeper, DealInteraction |
| World / Interactables | 3 | DoorInteractable, PickupInteractable, DestructibleProp |
| World (root) | 1 | ProceduralPropertyBuilder |
| UI / HUD | 1 | CombatHUD |
| UI / Phone | 6 | PhoneController, PhoneMapTab, PhoneContactsTab, PhoneProductsTab, PhoneFinanceTab, PhoneMessagesTab |
| UI / Dealing | 2 | DealUI, SupplierUI |
| UI / Economy | 1 | ShopUI |
| UI / Laundering | 1 | LaunderingUI (L key toggle dashboard) |
| UI / Forensics | 1 | ForensicsUI (F key toggle dashboard) |
| UI / Production | 1 | CraftingUI |
| Stats | 1 | RuntimeStats |
| Save | 1 | SaveManager (V2 -- JSON, auto-save, slot management) |
| Utils | 4 | EventBus, ObjectPooler, ObjectPoolConfig, ResourceDatabase |
| Editor | 12 | TestArenaBuilder, SceneBootstrapBuilder, WeaponAssetFactory, PlayerPrefabBuilder, AnimatorSetup, URPSetup, EditorShaderHelper, DealingSystemFactory, ProductionSystemFactory, EconomySystemFactory, PropertySystemFactory, TMPWarningFix |

---

## Tech Stack

| Component | Version / Detail |
|---|---|
| Engine | Unity 6 (6000.4.0f1) |
| Render Pipeline | Universal Render Pipeline (URP) |
| Networking | FishNet v4.7.0 (disabled for singleplayer Phase 2) |
| Camera | Cinemachine 3.1.6 |
| Input | New Input System |
| Level Design | ProBuilder |
| Art Style | Synty POLYGON |
| Assembly Definitions | `Clout` (runtime) + `Clout.Editor` (editor tooling) |
| Source Control | Git + LFS (GitHub: SL1C3D-L4BS/clout) |

---

## Architecture

```
Assets/_Project/
  Scripts/
    Core/                   # Bootstrap, state machines, game flow, balance config, perf monitor
    Player/                 # Player state, input handling, appearance
    AI/                     # AI state manager, detection, patrol, chase, combat
      Actions/              # Modular AI action behaviors with utility scoring
    Combat/                 # Melee + ranged attacks, weapons, projectiles, damage, ammo
    Camera/                 # 4-mode Cinemachine camera, collision avoidance
    Animation/              # Animator hook, root motion
    Actions/                # Player movement, rotation, combos, interaction, dodge
    Input/                  # New Input System bindings
    Network/                # FishNet bootstrapper, spawn, sync, damage (offline stub)
    Inventory/              # Item definitions, consumables, inventory manager
    Empire/
      Crafting/             # 6-type crafting stations, recipes, ingredients, production manager
      Dealing/              # Suppliers, product inventory, deal manager
      Properties/           # 8 property types, upgrades, stash, manager
      Employees/            # Autonomous worker AI (dealers, cooks, guards), recruitment, betrayal
      Economy/              # Dynamic pricing, cash flow, transaction ledger
        Laundering/         # Step 11: Money laundering pipeline, front businesses, IRS investigation
      Reputation/           # Street rep, faction standing
      Territory/            # District control, influence mechanics
    World/
      Police/               # 6-tier wanted system, witness/evidence, patrols, raids, pursuit AI
      NPCs/                 # Customers, suppliers, shopkeepers, deal interactions
      Districts/            # 7-phase procedural generation, district definitions, trigger zones
      Interactables/        # Doors, pickups, destructibles
    Stats/                  # Runtime stat tracking
    Save/                   # Save system V2 (JSON, auto-save, slot management)
    UI/
      HUD/                  # Combat HUD (health, stamina, ammo, wanted level, cash)
      Phone/                # Empire hub (Map, Contacts, Products, Finances, Messages)
      Dealing/              # Deal negotiation, supplier browsing
      Economy/              # Shop interface
      Production/           # Crafting interface
      Laundering/           # Money laundering dashboard
    Editor/                 # Scene builders, asset factories, prefab generators
    Utils/                  # EventBus, object pooling, resource database
```

---

## Design Patterns

| Pattern | Usage |
|---|---|
| **State Machine** | Core gameplay loop, player states, AI behavior states, game flow phases. `CharacterStateManager` base shared by player and AI. |
| **Strategy Pattern** | Modular `StateAction` components composed into states. Swap behaviors without modifying state logic. |
| **ScriptableObject Architecture** | All data definitions (weapons, recipes, products, properties, employees, districts) are SO-driven for designer-friendly tuning. |
| **Singleton Managers** | Service locator pattern for core managers (Economy, Property, Territory, Wanted, Save, GameFlow). |
| **Event Bus** | Type-safe publish/subscribe decoupling between systems. Zero direct references between Empire, World, and UI layers. |
| **Utility Theory AI** | `AIActionScoring` evaluates weighted utility functions to select NPC behaviors dynamically. Used by police, workers, and enemy AI. |
| **Procedural Generation** | 7-phase district generator producing layouts, POI placement, road networks, and building interiors. |

---

## Getting Started

### Prerequisites

- Unity 6 (6000.4.0f1) via Unity Hub
- Git with LFS enabled (`git lfs install`)
- 16 GB RAM minimum recommended

### Setup

```bash
git clone git@github.com:SL1C3D-L4BS/clout.git
cd clout
git lfs pull
```

Open the project in Unity Hub. Allow the initial import to complete (first launch may take several minutes).

### First Run

1. Open `Assets/_Project/Scenes/Bootstrap.unity`.
2. Press Play. The bootstrap scene loads all managers and transitions to the game scene.
3. Press **F3** to toggle the performance monitor overlay.

### Build Test Arena

Use the editor tooling to generate a test environment:

```
Unity Menu > SlicedLabs > Build Test Arena
```

This invokes `TestArenaBuilder` to scaffold a playable arena with enemies, NPCs, properties, crafting stations, and the full economy pipeline for rapid iteration.

---

## Controls

| Input | Action |
|---|---|
| WASD | Movement |
| Mouse | Camera |
| Left Click | Light attack |
| Right Click | Heavy attack / Aim (ranged) |
| Left Shift | Sprint |
| Space | Dodge roll |
| Tab | Lock-on toggle |
| E | Interact |
| P | Phone (empire hub) |
| I | Inventory |
| L | Laundering dashboard |
| F | Forensics intelligence dashboard |
| F3 | Performance monitor |
| Esc | Pause menu |

---

## Documentation

| Document | Path | Description |
|---|---|---|
| Game Design Document | `Docs/Design/GAME_DESIGN_DOCUMENT.md` | Full GDD: vision, mechanics, content plan |
| Criminal Ecosystem | `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md` | Interconnected criminal systems deep dive |
| Viral Mechanics | `Docs/Design/VIRAL_MECHANICS_2026.md` | Engagement and retention loop design |
| Build Specification | `Docs/Architecture/BUILD_SPECIFICATION.md` | Canonical build spec (70-section masterclass) |
| Phase 1 Plan | `Docs/Architecture/PHASE_1_EXECUTION_PLAN.md` | Core loop implementation spec |
| Phase 2 Plan | `Docs/Architecture/PHASE_2_MASTERCLASS_PLAN.md` | Empire systems spec (10 steps) |
| System Port Map | `Docs/Architecture/SYSTEM_PORT_MAP.md` | System dependency and communication map |
| Gap Analysis | `Docs/Architecture/GAP_ANALYSIS.md` | Feature gap tracking and prioritization |
| Synty Asset List | `Docs/Architecture/SYNTY_ASSET_LIST.md` | Synty POLYGON asset inventory and integration |
| Next Steps Roadmap | `Docs/Architecture/NEXT_STEPS_ROADMAP.md` | Forward-looking roadmap and priorities |

---

## Development Phases

| Phase | Name | Scope | Status |
|---|---|---|---|
| 0 | Foundation | Project setup, URP, input system, bootstrap, assembly definitions | COMPLETE |
| 1 | Core Loop | Movement, combat, camera, AI, inventory, networking scaffold, world basics (62 scripts) | COMPLETE |
| 2 | Empire Systems | Crafting, dealing, properties, employees, economy, police, districts, phone UI, game flow, balance (10 steps, +77 scripts) | COMPLETE |
| 3 | Advanced Empire | Money laundering, forensic signatures, rival factions, investigation AI, advanced territory warfare | IN PROGRESS (Steps 11-12 complete) |
| 4 | World and Multiplayer | FishNet re-enable, co-op, persistent world, expanded districts, dynamic world events | PLANNED |
| 5 | Content and Polish | Story missions, side content, audio, VFX, UI polish, localization, QA | PLANNED |
| 6 | Ship | Platform certification, optimization, launch prep, post-launch roadmap | PLANNED |

---

## License

This project is proprietary software owned by SlicedLabs. All rights reserved. Unauthorized copying, distribution, or modification is strictly prohibited.

---

SlicedLabs -- SL1C3D-L4BS/clout
