# CLOUT -- Technical Architecture

> Version 1.0 | April 2026
> Engine: Unity 6 (6000.4.0f1) | URP 17.4
> Assemblies: Clout (runtime), Clout.Editor (editor-only)

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Engine | Unity 6 | 6000.4.0f1 |
| Render Pipeline | Universal Render Pipeline (URP) | 17.4 |
| Input | Unity Input System | 1.19 |
| Camera | Cinemachine | 3.1.6 |
| AI Navigation | Unity AI Navigation | 2.0.6 |
| Level Design | ProBuilder | 6.0.9 |
| Networking | FishNet | 4.7.0 (disabled Phase 2) |
| UI | OnGUI (prototyping) | Migrating to UI Toolkit in Phase 5 |
| Art Assets | Synty POLYGON | Multiple packs via SyntyPass |

---

## Assembly Structure

```
Clout.asmdef (Runtime)
  Namespaces:
    Clout.Core          -- State machine, interfaces, enums, event bus, game flow
    Clout.Player        -- PlayerStateManager, PlayerInputHandler
    Clout.AI            -- Enemy AI state management
    Clout.AI.Actions    -- Pluggable AI behaviors
    Clout.Combat        -- Melee + ranged combat, weapons, damage
    Clout.Camera        -- 4-mode Cinemachine camera
    Clout.Animation     -- AnimatorHook, IK, root motion
    Clout.Network       -- FishNet bootstrapper (offline stubs)
    Clout.Inventory     -- Item definitions, equipment
    Clout.Stats         -- RuntimeStats (health, stamina, poise)
    Clout.Save          -- SaveManager, CloutSaveData
    Clout.Utils         -- EventBus, ObjectPooler, ResourceDatabase
    Clout.Empire.Crafting    -- CraftingStation, ProductionManager, recipes
    Clout.Empire.Dealing     -- DealManager, ProductInventory, suppliers
    Clout.Empire.Properties  -- PropertyManager, Property, definitions
    Clout.Empire.Employees   -- WorkerManager, DealerAI, CookAI, GuardAI
    Clout.Empire.Economy     -- CashManager, EconomyManager, TransactionLedger
    Clout.Empire.Reputation  -- ReputationManager, 4D vector
    Clout.Empire.Territory   -- TerritoryManager
    Clout.World.Police       -- WantedSystem, HeatResponseManager, PolicePatrolAI
    Clout.World.NPCs         -- CustomerAI, ShopKeeper, DealInteraction
    Clout.World.Districts    -- DistrictManager, ProceduralDistrictGenerator
    Clout.UI.HUD             -- CombatHUD
    Clout.UI.Phone           -- PhoneController, 5 tab controllers
    Clout.UI.Dealing         -- DealUI, SupplierUI
    Clout.UI.Economy         -- ShopUI
    Clout.UI.Production      -- CraftingUI

Clout.Editor.asmdef (Editor Only)
  References: Clout.asmdef
  Namespace: Clout.Editor
    -- TestArenaBuilder, WeaponAssetFactory, AnimatorSetup
    -- DealingSystemFactory, ProductionSystemFactory
    -- PropertySystemFactory, EconomySystemFactory
    -- SceneBootstrapBuilder, URPSetup, EditorShaderHelper
```

---

## Design Patterns

### State Machine Architecture
All characters (player + AI) inherit from `CharacterStateManager`, which manages a stack of `State` ScriptableObjects. Each State contains a list of `StateAction` ScriptableObjects that execute per-frame logic. This enables data-driven character behavior where designers compose states from reusable actions without code changes.

```
CharacterStateManager (MonoBehaviour)
  -> State (ScriptableObject)
    -> StateAction[] (ScriptableObjects)
      e.g., MovePlayerCharacter, HandleRotation, HandleStats, InputHandler
```

### Event Bus
Type-safe generic pub/sub system for cross-system communication. Any struct can be an event. Systems subscribe to specific event types without direct references to publishers.

```csharp
// Publishing
EventBus.Publish(new DealCompletedEvent { productId = "meth", revenue = 500f });

// Subscribing
EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
```

Currently 20+ event types: DealCompleted, ProductCooked, PropertyPurchased, PropertyRaided, PropertyUpgraded, WorkerHired, WorkerFired, WorkerArrested, WorkerBetrayed, MoneyChanged, HeatChanged, WantedLevelChanged, DistrictEntered, EnemyKilled, ReputationChanged, and more.

### Singleton Managers
Central access pattern for gameplay managers. Each implements Instance with Awake-time registration and OnDestroy cleanup.

Active singletons: CashManager, PropertyManager, ProductionManager, TransactionLedger, EconomyManager, WorkerManager, RecruitmentManager, HeatResponseManager, DistrictManager, GameFlowManager, PerformanceMonitor, WantedSystem, ReputationManager.

### ScriptableObject Architecture
All game data is defined as ScriptableObjects: weapons, ammo, recipes, ingredients, products, property definitions, employee definitions, district definitions, states, and actions. Runtime instances reference SO data, enabling designer-editable tuning without code changes.

`GameBalanceConfig` is a special SO that centralizes 50+ tuning values across 12 categories with 3 difficulty presets (Easy, Normal, Hardcore). Accessed via `GameBalanceConfig.Active` static property.

### Utility Theory AI
AI combat decisions use weighted scoring across multiple factors (distance, health, ammo, cover availability, aggression). Each potential action receives a utility score; the highest-scoring action executes. This produces varied, context-appropriate AI behavior without complex behavior trees.

---

## Data Flow

### Core Game Loop
```
IngredientInventory -> CraftingStation -> ProductInventory -> DealManager -> CashManager
                                                                              |
                                                                    TransactionLedger
                                                                              |
                                                                    EconomyManager (pricing)
```

### Worker Automation Loop
```
WorkerManager -> DealerAI: Load product -> Patrol route -> Deal to customers -> Deposit cash
              -> CookAI: Load ingredients -> Start batch -> Store output -> Rest
              -> GuardAI: Patrol perimeter -> Engage hostiles -> Defend raids
```

### Heat Escalation Loop
```
Player Actions -> WantedSystem (heat accumulation)
                   -> HeatResponseManager (police spawning)
                     -> PolicePatrolAI (patrol/investigate/pursue/arrest)
                       -> PropertyRaidSystem (stash confiscation)
WitnessSystem -> WantedSystem (crime reporting)
```

### Save/Load Pipeline
```
GameFlowManager.CaptureGameState()
  -> RuntimeStats, CashManager, ReputationManager, PropertyManager,
     WorkerManager, WantedSystem, MilestoneTracker, DistrictManager
  -> CloutSaveData (serializable struct)
  -> SaveManager.Save() -> JSON -> Application.persistentDataPath
```

---

## Directory Structure

```
Assets/_Project/
  Scripts/
    Core/           State machine, interfaces, enums, event bus
                    CloutGameFlowManager.cs, GameBalanceConfig.cs, PerformanceMonitor.cs
    Player/         PlayerStateManager, PlayerInputHandler
    AI/             AIStateManager
      Actions/      AIDetection, AIPatrol, AIChaseTarget, AICombatSelector, AIRangedAttack, AIActionScoring
    Combat/         AttackAction, RangedAttackAction, DamageCollider, Projectile, weapons, ammo
    Camera/         CameraManager, CameraCollision
    Animation/      AnimatorHook
    Input/          Input System bindings
    Network/        NetworkBootstrapper (offline stub)
    Inventory/      ItemDefinition, ConsumableItem, InventoryManager
    Stats/          RuntimeStats
    Save/           SaveManager
    Empire/
      Crafting/     CraftingStation, ProductionManager, RecipeDefinition, IngredientDefinition, ProductDefinition
      Dealing/      DealManager, ProductInventory, SupplierDefinition, DealingBootstrapper
      Properties/   PropertyManager, Property, PropertyDefinition
      Employees/    WorkerManager, WorkerInstance, DealerAI, CookAI, GuardAI, EmployeeDefinition, RecruitmentManager
      Economy/      CashManager, EconomyManager, TransactionLedger
      Reputation/   ReputationManager
      Territory/    TerritoryManager
    World/
      Police/       WantedSystem, HeatResponseManager, PolicePatrolAI, WitnessSystem, PropertyRaidSystem
      NPCs/         CustomerAI, ShopKeeper, DealInteraction, SupplierNPC
      Districts/    DistrictManager, DistrictDefinition, ProceduralDistrictGenerator, DistrictTriggerZone
    UI/
      HUD/          CombatHUD
      Phone/        PhoneController, PhoneMapTab, PhoneContactsTab, PhoneFinanceTab, PhoneProductsTab, PhoneMessagesTab
      Dealing/      DealUI, SupplierUI
      Economy/      ShopUI
      Production/   CraftingUI
    Editor/         TestArenaBuilder, WeaponAssetFactory, AnimatorSetup, + 7 more factory/builder tools
    Utils/          EventBus, ObjectPooler, ObjectPoolConfig, ResourceDatabase
  Resources/
    GameBalanceConfig.asset
  ScriptableObjects/
    Weapons/, Ammo/, States/, Recipes/, Products/, Ingredients/, Employees/, Properties/
  Prefabs/
    Player, NPCs, weapons, props
  Scenes/
    Bootstrap, Main, Test
```

---

## Performance Targets

| Metric | Target | Minimum | Current |
|--------|--------|---------|---------|
| Frame Rate | 60 FPS | 30 FPS | Monitored via PerformanceMonitor |
| NavMesh Agents | <50 | Budget warning at 50 | Tracked |
| GameObjects | <2000 | Budget warning at 2000 | Tracked |
| Managed Memory | <512 MB | Warning at 512 MB | Tracked |
| Frame Spike | <16.7 ms | Warning at 50 ms | Tracked |
| Game Day Duration | 600s (10 min) | Configurable | GameBalanceConfig |

---

## Known Technical Constraints

1. **Unity 6 Asset DB Cache Bug:** File named `GameFlowManager.cs` caused corrupted cache entry. Workaround: file renamed to `CloutGameFlowManager.cs` (class name unchanged).

2. **C# Namespace Resolution:** From `Clout.Core`, sibling namespaces like `Clout.Stats` require full qualification or explicit `using` directives. C# does not resolve through parent namespaces.

3. **FindObjectsByType API:** Unity 6 simplified `FindObjectsByType<T>(FindObjectsSortMode.None)` to `FindObjectsByType<T>()`. Both compile; the linter auto-simplifies.

4. **OnGUI Performance:** Current Phase 2 UI uses OnGUI for rapid prototyping. Known to be inefficient at scale. Migration to UI Toolkit planned for Phase 5 Step 22.

---

*CLOUT Technical Architecture v1.0 -- SlicedLabs -- April 2026*
