# CLOUT — Phase 2 Masterclass Implementation Plan

> From combat prototype to playable vertical slice. This is where CLOUT becomes a game.

---

## Cross-Analysis: Where We Are vs Where We Need To Be

### Build Specification Phase Map

| Spec Phase | Status | Coverage |
|-----------|--------|----------|
| Phase 0: Foundation | ✅ DONE | Codebase merged, SO architecture, asmdef, URP configured, player controller, camera, test scene |
| Phase 1: Core Loop | 🟡 75% | Combat ✅, Inventory skeleton ✅, Dealing ✅, Production ❌, Economy basics ✅, Playable district ❌ |
| Phase 2: Empire Systems | 🟡 35% | Heat ✅, Reputation ✅, Dealing pipeline ✅, Properties ❌, Workers ❌, Laundering ❌ |
| Phase 3: World & AI | 🔴 15% | Combat AI ✅, Customer AI ✅, everything else ❌ |
| Phase 4: Multiplayer | 🟡 30% | FishNet foundation ✅, SyncVars ✅, spawning ✅, actual MP gameplay ❌ |
| Phase 5: Polish & Content | 🔴 0% | Not started |
| Phase 6: Ship | 🔴 0% | Not started |

### What Phase 1 Actually Built (62 scripts)

**STRONG:**
- Full state machine architecture (battle-tested pattern)
- Complete melee + ranged combat pipeline
- 4-mode camera system
- AI with utility theory scoring (detection → patrol → chase → combat)
- FishNet networking foundation with SyncVar<T>
- Empire event pipeline (kills → CLOUT, combat → heat)
- Server-authoritative damage

**MISSING for Phase 1 "Vertical Slice" milestone:**
- NPC dealing mechanic (approach, negotiate, exchange)
- Basic production (single mixing station)
- Simple economy (buy/sell with flat prices)
- One playable district with hand-built layout
- Player can actually walk/fight in test arena (needs ScriptableObject data wired)

### The Gap

Phase 1's code compiles. The architecture is solid. But there's no **gameplay loop** yet. You can't:
1. Walk up to an NPC and sell them product
2. Cook product at a station
3. Earn money from dealing
4. Buy anything with that money
5. See a real district (just a test arena)

**Phase 2's mission: Close the loop. Make it playable.**

---

## Phase 2: The Vertical Slice Sprint

**Goal:** A single playable district where you can cook product, deal to NPCs, earn money, buy a property, hire a worker, and get chased by police. The complete core loop in one district.

**Duration:** 4–6 weeks of focused implementation

---

## Step 1: ScriptableObject Data Foundation ✅ COMPLETE (Week 1, Days 1–2)

Before any gameplay works, we need the actual data assets that drive every system.

### 1A: Weapon ScriptableObjects

Create the SO assets that the combat system already expects:

```
ScriptableObjects/
├── Weapons/
│   ├── Melee/
│   │   ├── SO_Weapon_Fists.asset          (unarmed, fast, low damage)
│   │   ├── SO_Weapon_Bat.asset            (medium speed, high damage)
│   │   ├── SO_Weapon_Knife.asset          (fast, medium damage, bleed)
│   │   └── SO_Weapon_Machete.asset        (medium, very high damage)
│   └── Ranged/
│       ├── SO_Weapon_Pistol9mm.asset      (semi-auto, low recoil)
│       ├── SO_Weapon_Revolver.asset       (semi-auto, high damage, slow)
│       ├── SO_Weapon_SMG.asset            (full-auto, medium recoil)
│       └── SO_Weapon_Shotgun.asset        (pump, high damage, short range)
├── Ammo/
│   ├── SO_Ammo_9mm.asset
│   ├── SO_Ammo_45ACP.asset
│   ├── SO_Ammo_12Gauge.asset
│   └── SO_Ammo_556.asset
```

**Scripts needed:**
- `Editor/WeaponSOFactory.cs` — Editor tool to batch-create weapon SOs with sane defaults
- Verify: `WeaponItem.cs`, `RangedWeaponItem.cs`, `AmmoDefinition.cs` SO creation menus work

### 1B: State ScriptableObjects

Create the State + StateAction SOs that the state machine expects:

```
ScriptableObjects/
├── States/
│   ├── Player/
│   │   ├── SO_State_Locomotion.asset      (Move, HandleRotation, HandleStats, InputHandler)
│   │   ├── SO_State_Combat.asset          (AttackAction, RangedAttackAction, InputsForCombo)
│   │   ├── SO_State_Interaction.asset     (MonitorInteraction)
│   │   └── SO_State_Death.asset
│   └── AI/
│       ├── SO_State_AIPatrol.asset        (AIPatrol, AIDetection)
│       ├── SO_State_AIChase.asset         (AIChaseTarget, AIDetection, AICombatSelector)
│       ├── SO_State_AIAttack.asset        (utility scoring → melee/ranged)
│       └── SO_State_AIDeath.asset
├── Actions/
│   ├── SO_Action_MovePlayer.asset
│   ├── SO_Action_HandleRotation.asset
│   ├── SO_Action_HandleStats.asset
│   ├── SO_Action_InputHandler.asset
│   ├── SO_Action_AttackAction.asset
│   ├── SO_Action_RangedAttackAction.asset
│   ├── SO_Action_AIDetection.asset
│   ├── SO_Action_AIPatrol.asset
│   └── ... (one per StateAction class)
```

**Scripts needed:**
- `Editor/StateSOFactory.cs` — Editor tool to create State SOs with pre-configured action lists

### 1C: Animator Controllers

The animation system needs actual controllers:

```
Animations/
├── Controllers/
│   ├── AC_Player.controller             (locomotion blend tree, combat layer, interaction layer)
│   └── AC_Enemy.controller              (patrol, chase, attack, death)
├── BlendTrees/
│   ├── BT_Locomotion.asset             (idle → walk → run → sprint, horizontal strafe)
│   └── BT_Combat_Locomotion.asset      (combat stance movement)
```

**Scripts needed:**
- `Editor/AnimatorControllerBuilder.cs` — Programmatically builds the animator controller with layers, parameters, and transitions. We won't have Synty anims yet, so use Unity's built-in humanoid clips as placeholders.

**Key parameters:** `vertical`, `horizontal`, `isInteracting`, `isGrounded`, `isCombatMode`, `attackTrigger`, `comboIndex`, `isDead`

---

## Step 2: The Dealing System ✅ COMPLETE (Week 1, Days 3–5)

The core money-making mechanic. This is Schedule 1's bread and butter.

### 2A: Product Data Model

```csharp
// Empire/Products/ProductDefinition.cs
[CreateAssetMenu(menuName = "CLOUT/Products/Product Definition")]
public class ProductDefinition : ScriptableObject
{
    public string productName;
    public ProductType productType;      // Weed, Crystal, Powder, Pills
    public float baseValue;              // Base street price
    public float basePotency;            // 0-1 effectiveness
    public QualityTier quality;          // Trash, Street, Premium, Pure, Legendary
    public List<ProductEffect> effects;  // What it does to consumers
    public Sprite icon;
    public GameObject worldPrefab;       // Baggie/brick/jar model
    public float weight;                 // Affects carry capacity
}

public enum ProductType { Weed, Crystal, Powder, Pills, Shroom }
public enum QualityTier { Trash, Street, Premium, Pure, Legendary }

[System.Serializable]
public class ProductEffect
{
    public EffectType type;              // Energy, Euphoria, Paranoia, Hallucination, etc.
    public float intensity;              // 0-1
}
```

### 2B: Product Inventory

```csharp
// Empire/Products/ProductInventory.cs
// Separate from weapon inventory — tracks product stacks with quantity + quality
public class ProductInventory : NetworkBehaviour
{
    public readonly SyncList<ProductStack> products = new SyncList<ProductStack>();

    // AddProduct, RemoveProduct, GetTotalValue, GetProductsByType
    // Weight system: total carried weight affects movement speed
}

[System.Serializable]
public struct ProductStack
{
    public string productId;         // References ProductDefinition name
    public int quantity;
    public QualityTier quality;
    public float potency;
}
```

### 2C: NPC Customer AI

```csharp
// AI/NPCs/CustomerNPC.cs
// NPCs that want to buy product. Schedule 1-style approach.
public class CustomerNPC : MonoBehaviour
{
    public CustomerProfile profile;       // SO with preferences
    public CustomerState state;           // Browsing, Interested, Negotiating, Buying, Satisfied, Fleeing

    // Detection: customer sees player holding product → approaches
    // OR player approaches customer with product equipped
    // Negotiation: price vs customer willingness (affected by quality, reputation, demand)
    // Exchange: product leaves player inventory, cash enters
    // Satisfaction: quality affects addiction, loyalty, snitch risk
    // Repeat: satisfied customers return, addicted ones seek player out
}

// AI/NPCs/CustomerProfile.cs
[CreateAssetMenu(menuName = "CLOUT/AI/Customer Profile")]
public class CustomerProfile : ScriptableObject
{
    public ProductType preferredProduct;
    public float maxPrice;                // Won't pay more than this
    public float minQuality;              // Won't accept below this
    public float addictionLevel;          // 0-1, affects desperation
    public float snitchRisk;              // 0-1, chance of calling police
    public float wealthLevel;             // Affects purchase quantity
}
```

### 2D: Deal UI

```csharp
// UI/Dealing/DealUI.cs
// In-world deal interface — appears when player initiates deal with customer
// Shows: product selection, quantity slider, price negotiation, customer mood, risk meter
// Quick-deal option for repeat customers
// Cancel/Walk away option
```

**Total new scripts for dealing:** ~8 files
- `Empire/Products/ProductDefinition.cs`
- `Empire/Products/ProductInventory.cs`
- `Empire/Products/ProductStack.cs`
- `AI/NPCs/CustomerNPC.cs`
- `AI/NPCs/CustomerProfile.cs`
- `AI/NPCs/CustomerSpawner.cs`
- `UI/Dealing/DealUI.cs`
- `UI/Dealing/DealManager.cs`

> **✅ IMPLEMENTED:** Full dealing pipeline operational. Created: ProductDefinition (SO), ProductInventory (quality-aware stacking), DealManager (singleton orchestrator), SupplierNPC (wholesale buying with bust risk), CustomerAI (seeking/negotiating/satisfied states), DealUI (OnGUI negotiation panel), SupplierUI (catalog browsing), DealInteraction (IInteractable), DealingBootstrapper (testing helper), DealingSystemFactory (editor tool). 5 products (Weed, Crystal, Powder, Pills, Shroom), 2 suppliers (Lil D, Mr Kim), 3 test customers wired into test arena. Quality tiers with price multipliers (0.5x–4x). Addiction, loyalty, snitch mechanics integrated with WantedSystem and ReputationManager.

---

## Step 3: Basic Production (Week 2, Days 1–3)

One mixing station. Put ingredients in, wait, get product out.

### 3A: Production Station

```csharp
// Empire/Production/MixingStation.cs
// Interactable world object. Player approaches, opens mixing UI.
// Select base product + up to 3 ingredients → timer → output product
// Quality determined by: ingredient combo, player skill, station level
// Ingredients modify effects: adding baking soda = cut product (lower quality, more quantity)
// Adding premium ingredient = higher quality, less quantity
```

### 3B: Recipe System

```csharp
// Empire/Production/RecipeDefinition.cs
[CreateAssetMenu(menuName = "CLOUT/Production/Recipe")]
public class RecipeDefinition : ScriptableObject
{
    public ProductDefinition baseProduct;
    public List<IngredientSlot> ingredients;    // Up to 4 slots
    public ProductDefinition outputProduct;
    public float productionTime;                 // Seconds
    public int outputQuantity;
    public float qualityMultiplier;
}

// Empire/Production/IngredientDefinition.cs
[CreateAssetMenu(menuName = "CLOUT/Production/Ingredient")]
public class IngredientDefinition : ScriptableObject
{
    public string ingredientName;
    public float cost;                          // Buy from supplier
    public List<EffectModifier> effectModifiers; // How it changes the product
    public float potencyModifier;               // Multiplier on potency
    public float quantityModifier;              // More or less output
    public Sprite icon;
}
```

### 3C: Mixing UI

```csharp
// UI/Production/MixingUI.cs
// Shows station slots, available ingredients, timer, output preview
// Drag ingredients into slots, press Cook, wait for timer, collect output
```

**Total new scripts for production:** ~6 files
- `Empire/Production/MixingStation.cs`
- `Empire/Production/RecipeDefinition.cs`
- `Empire/Production/IngredientDefinition.cs`
- `Empire/Production/ProductionManager.cs`
- `UI/Production/MixingUI.cs`
- `Editor/ProductSOFactory.cs`

---

## Step 4: Money & Basic Economy (Week 2, Days 4–5)

### 4A: Cash System

```csharp
// Empire/Economy/CashManager.cs
// Tracks: cash on hand (dirty), bank balance (clean), total net worth
// All FishNet SyncVar<T> for multiplayer
// Cash earned from: dealing, robberies, property income
// Cash spent on: ingredients, properties, workers, weapons, vehicles, bribes
public class CashManager : NetworkBehaviour
{
    public readonly SyncVar<float> dirtyMoney = new SyncVar<float>(0f);
    public readonly SyncVar<float> cleanMoney = new SyncVar<float>(0f);

    [Server] public void EarnDirty(float amount, string source);
    [Server] public void Spend(float amount, string reason);
    [Server] public bool CanAfford(float amount);
}
```

### 4B: Shop System

```csharp
// Empire/Economy/ShopKeeper.cs
// NPC that sells ingredients, weapons, ammo
// Static pricing for Phase 2 (dynamic in Phase 3)
// Player approaches → shop UI opens → browse catalog → buy with cash
```

### 4C: Price Manager

```csharp
// Empire/Economy/PriceManager.cs
// Singleton that tracks product prices per district
// Phase 2: flat prices based on quality tier
// Phase 3: supply/demand curves
public static float GetPrice(ProductDefinition product, string districtId)
```

**Total new scripts for economy:** ~5 files
- `Empire/Economy/CashManager.cs`
- `Empire/Economy/ShopKeeper.cs`
- `Empire/Economy/PriceManager.cs`
- `UI/Economy/ShopUI.cs`
- `UI/Economy/CashDisplay.cs`

---

## Step 5: Property System — First Property (Week 3, Days 1–3)

### 5A: Property Purchase

```csharp
// Empire/Properties/PropertyDefinition.cs — already exists as skeleton
// Flesh out: PropertyManager becomes the player's property portfolio
// PropertyInteractable — world object the player approaches to buy/enter

// New: PropertyDefinition.cs enhancement
[CreateAssetMenu(menuName = "CLOUT/Properties/Property")]
public class PropertyDefinition : ScriptableObject
{
    public string propertyName;
    public PropertyType type;            // Safehouse, Lab, GrowHouse, Storefront, Warehouse
    public float purchasePrice;
    public float weeklyUpkeep;
    public int productionSlots;          // How many mixing stations fit
    public int storageCapacity;          // Product storage
    public int workerSlots;              // Max employees
    public List<UpgradeDefinition> availableUpgrades;
    public Vector3 interiorSpawnPoint;   // Where player spawns inside
}
```

### 5B: Property Interior Loading

```csharp
// Empire/Properties/PropertyInterior.cs
// Additive scene loading — each property type has an interior scene
// Player enters property → load interior scene additively → place player at spawn point
// Exit → unload interior, return to city position
// Interior contains: mixing stations, storage, worker stations
```

### 5C: Stash System

```csharp
// Empire/Properties/StashManager.cs
// Storage at properties — product, cash, weapons
// Limited by storage capacity (upgradeable)
// Visible in-world (shelves fill up as you store more)
// Police raids can seize stash contents
```

**Total new scripts for properties:** ~6 files
- `Empire/Properties/PropertyInteractable.cs`
- `Empire/Properties/PropertyInterior.cs`
- `Empire/Properties/StashManager.cs`
- `Empire/Properties/PropertyUpgrade.cs`
- `UI/Properties/PropertyPurchaseUI.cs`
- `UI/Properties/PropertyManagementUI.cs`

---

## Step 6: Worker Hiring (Week 3, Days 4–5)

### 6A: Hire First Dealer

```csharp
// Empire/Employees/WorkerDefinition.cs — flesh out existing skeleton
// Worker stats: skill, loyalty, stealth, speed, combat
// Worker types: Dealer, Cook, Guard (Phase 2 = Dealer only)
// Workers as NetworkObjects with their own AI

// Empire/Employees/DealerAI.cs
// Autonomous NPC that follows a route and deals to customers
// Route: set of waypoints in controlled territory
// Behavior: walk route → find customer → deal → return profit to stash → repeat
// Risk: can be robbed, arrested, killed, or flip to rival
// Income: takes a cut (loyalty-dependent)
```

### 6B: Recruitment

```csharp
// Empire/Employees/RecruitmentManager.cs
// Find potential workers at specific locations (bars, street corners)
// Reputation-gated: better workers at higher CLOUT
// Hire cost + weekly salary
// Worker appears in world, follows assigned behavior
```

**Total new scripts for workers:** ~5 files
- `Empire/Employees/DealerAI.cs`
- `Empire/Employees/WorkerRoute.cs`
- `Empire/Employees/RecruitmentManager.cs`
- `UI/Employees/HireUI.cs`
- `UI/Employees/WorkerManagementUI.cs`

---

## Step 7: Police AI — Chase & Investigate (Week 4, Days 1–3)

### 7A: Police Patrol AI

```csharp
// World/Police/PolicePatrolAI.cs
// Extends AIStateManager with police-specific behavior
// States: Patrol → Investigate → Pursue → Arrest → Combat
// Patrol: follow waypoint routes through district
// Investigate: respond to heat sources (gunfire, reported deals)
// Pursue: chase player on foot (NavMesh)
// Arrest: non-lethal takedown attempt at low wanted levels
// Combat: lethal force at high wanted levels
```

### 7B: Heat Response System

```csharp
// World/Police/HeatResponseManager.cs
// Global manager that spawns police based on heat level
// Low heat: regular patrols, occasional investigation
// Medium heat: increased patrols, checkpoints
// High heat: active pursuit, property surveillance
// Extreme heat: SWAT response (Phase 3+)
```

### 7C: Evidence & Witnesses

```csharp
// World/Police/WitnessSystem.cs
// Civilians who see crimes generate heat
// Evidence degrades over time
// Player can intimidate witnesses to reduce evidence
// Destroying evidence at crime scenes reduces heat gain
```

**Total new scripts for police:** ~5 files
- `World/Police/PolicePatrolAI.cs`
- `World/Police/HeatResponseManager.cs`
- `World/Police/WitnessSystem.cs`
- `World/Police/PoliceSpawner.cs`
- `AI/NPCs/CivilianNPC.cs`

---

## Step 8: First District — The Slums (Week 4, Days 4–5 + Week 5)

### 8A: District Layout

Build one complete district using ProBuilder (Synty assets come later):

```
THE SLUMS — Starter District
├── 4 city blocks with streets
├── 10-15 buildings (greyboxed)
│   ├── 2 purchasable properties (apartment lab, corner store front)
│   ├── 1 supplier location (alley meeting spot)
│   ├── 1 shop (weapons/ingredients)
│   ├── 5+ residential (NPC homes, decorative)
│   └── 2 abandoned (future expansion)
├── Street corners (dealing hotspots)
├── Alleyways (escape routes, stash spots)
├── Park area (customer meeting zone)
├── Police patrol route
└── 3 spawn points (player, customers, police)
```

### 8B: District Manager

```csharp
// World/Districts/DistrictManager.cs
// Manages one district: NPCs, spawn points, demand, heat, control level
// Tracks: who controls this district, current demand per product, active NPCs
// Spawns: customers, police, ambient civilians on timer

// World/Districts/DistrictDefinition.cs
[CreateAssetMenu(menuName = "CLOUT/World/District")]
public class DistrictDefinition : ScriptableObject
{
    public string districtName;
    public float baseDemand;              // How much product this district wants
    public float wealthLevel;             // Affects pricing
    public float policePresence;          // Base patrol frequency
    public ProductType[] preferredProducts;
    public int maxCustomers;
    public int maxPolice;
}
```

### 8C: Scene Architecture

```
Scenes/
├── Bootstrap/Bootstrap.unity          (exists — loading screen, NetworkManager)
├── Main/Main.unity                    (exists — persistent managers)
├── Districts/
│   └── Slums/Slums.unity             (first playable district)
├── Interiors/
│   ├── INT_ApartmentLab.unity        (first production property)
│   └── INT_CornerStore.unity         (first storefront)
└── Test/
    └── TestArena.unity               (existing combat test)
```

**Total new scripts for district:** ~6 files
- `World/Districts/DistrictManager.cs`
- `World/Districts/DistrictDefinition.cs`
- `World/Districts/SpawnZone.cs`
- `World/Districts/DistrictLoader.cs`
- `Core/SceneTransitionManager.cs`
- `Editor/DistrictBuilder.cs`

---

## Step 9: The Phone — Empire Management UI (Week 5, Days 3–5)

### 9A: Phone System

```csharp
// UI/Phone/PhoneController.cs
// In-game management device. Press Tab/DPad-Up to open.
// Tabs: Map, Contacts, Products, Finances, Messages
// Map: shows district with territory control overlay
// Contacts: list of workers, suppliers, customers
// Products: inventory, pricing, quality
// Finances: income, expenses, net profit
// Messages: tips, warnings, orders from workers
```

### 9B: Minimap

```csharp
// UI/HUD/Minimap.cs
// Render texture from top-down camera
// Shows: player position, nearby NPCs (color-coded), property locations, police, customers
// Expand to full map on phone
```

**Total new scripts for UI:** ~5 files
- `UI/Phone/PhoneController.cs`
- `UI/Phone/PhoneTab.cs` (base class)
- `UI/Phone/MapTab.cs`
- `UI/Phone/FinanceTab.cs`
- `UI/HUD/Minimap.cs`

---

## Step 10: Integration & Polish Pass (Week 6)

### 10A: Wire Everything Together

- PlayerStateManager gets `CashManager`, `ProductInventory` component references
- Dealing completes → cash earned → CashManager.EarnDirty()
- Products cooked → added to ProductInventory
- Properties purchased → removed from CashManager, added to PropertyManager
- Workers hired → salary deducted weekly from CashManager
- All UI elements read from authoritative SyncVars

### 10B: Game Flow

```
NEW GAME:
1. Player spawns in Slums with $500 dirty cash, no product
2. Find supplier (marked on map) → buy raw materials
3. Go to abandoned building → find mixing station
4. Cook first product batch (tutorial prompt)
5. Find customers on street corners (marked on minimap)
6. Deal to earn cash
7. Earn enough to buy first property ($5,000)
8. Set up lab in property with mixing station
9. Cook more product, deal more
10. Hire first dealer ($1,000 + $200/week salary)
11. Dealer auto-deals in territory
12. Heat builds → police investigate → player evades or fights
13. LOOP: Cook → Deal → Earn → Expand → Manage Heat
```

### 10C: Save System Skeleton

```csharp
// Save/SaveManager.cs
// Serialize: player position, cash, product inventory, owned properties, hired workers
// JSON format, single save slot for Phase 2
// Auto-save on property exit and deal completion
```

---

## Script Count Summary

| Category | New Scripts | Running Total |
|----------|-------------|---------------|
| Phase 1 (existing) | — | 62 |
| SO Data & Editors | 4 | 66 |
| Dealing System | 8 | 74 |
| Production System | 6 | 80 |
| Economy System | 5 | 85 |
| Property System | 6 | 91 |
| Worker System | 5 | 96 |
| Police AI | 5 | 101 |
| District & World | 6 | 107 |
| Phone & UI | 5 | 112 |
| Integration & Save | 3 | 115 |
| **TOTAL** | **~53 new** | **~115 scripts** |

---

## Synty Asset Integration Points

When SyntyPass is active, these Phase 2 systems map directly to asset packs:

| System | Synty Pack | Assets Used |
|--------|-----------|-------------|
| Slums District | Town Pack + Apocalypse | Buildings, streets, debris |
| Mixing Station | Gang Warfare | Lab equipment, tables, chemistry props |
| Customers | City Characters | Civilian NPCs (19 types) |
| Police | Police Station | Officers, vehicles, equipment |
| Properties | Town + City + Shops | House exteriors, shop interiors |
| Weapons | Gang Warfare + Battle Royale | Melee + ranged weapons |
| Products | Gang Warfare | Baggies, money, scales, packaging |
| Vehicles (visual only) | City Pack | Parked cars for atmosphere |

**Priority imports for Phase 2:**
1. Town Pack → Slums district buildings
2. Gang Warfare → Lab props, gang NPCs, product props
3. City Characters → Customer NPC variety
4. Police Station → Police NPCs and vehicles
5. Prototype Pack (FREE) → Greybox anything Synty doesn't cover

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| SO data wiring takes too long | Medium | High | EditorTools that batch-create SOs with defaults |
| Animation controller complexity | High | Medium | Programmatic builder, placeholder anims, layer-by-layer |
| Additive scene loading bugs | Medium | Medium | Test early with Bootstrap→Main→District flow |
| FishNet SyncList<T> complexity | Medium | High | Start singleplayer, add network sync after gameplay works |
| District greyboxing is tedious | High | Low | ProBuilder for speed, replace with Synty later |
| Dealing AI is too simple/broken | Medium | High | Start with fixed-position customers, add roaming later |

---

## Validation Criteria

Phase 2 is DONE when:

- [ ] Player spawns in Slums district with starting cash
- [ ] Player can find and buy ingredients from shop NPC
- [ ] Player can use mixing station to cook product
- [ ] Player can approach customer NPC and initiate deal
- [ ] Deal UI shows product selection, price negotiation
- [ ] Successful deals add cash to player's CashManager
- [ ] Player can purchase a property (apartment lab)
- [ ] Player can enter/exit property interior (additive scene)
- [ ] Property interior has functional mixing station
- [ ] Player can hire one dealer NPC
- [ ] Dealer NPC follows route and auto-deals
- [ ] Dealer income appears in player's finances
- [ ] Police patrol the district on NavMesh routes
- [ ] Police respond to heat (investigate dealing, chase player)
- [ ] Wanted level escalates with criminal activity
- [ ] Phone UI shows map, finances, contacts
- [ ] Minimap shows player, NPCs, properties
- [ ] All combat from Phase 1 still works in the district
- [ ] Basic save/load preserves player state
- [ ] FishNet compiles (multiplayer not required to function)

**Milestone: Playable vertical slice — walk around one district, fight, deal, cook, earn, buy, hire, evade police.**

---

## Execution Priority

If time is short, implement in this order (each step is playable):

1. **SO Data + Animator** — Nothing works without these
2. **Dealing** — Core money loop, makes the game "feel" like a game
3. **Production** — Adds depth to dealing (cook → deal → earn)
4. **Money + Shop** — Closes the economic loop (earn → buy ingredients → cook → deal)
5. **Property** — First expansion milestone (save up → buy → set up lab)
6. **District** — Real environment (replace test arena with actual level)
7. **Police** — Stakes and tension (dealing has consequences)
8. **Workers** — Automation (hire dealer → passive income)
9. **Phone UI** — Management layer (overview of empire)
10. **Save System** — Persistence (progress carries between sessions)

---

---

## Appendix A: Sharp Accent Cross-Analysis & Port Map

### What We Extracted (701 files analyzed → 11 systems ported)

The Sharp Accent codebase (`sharpaccent-full/`) contains 701 C# scripts across the root and 16 tutorial folders (Parts 151–169). It's a **combat-focused action game engine** — no business simulation, economy, or property systems exist. Everything useful is about character systems, inventory, save/load, and utility infrastructure.

### Systems Ported to CLOUT (SA → Clout namespace)

| SA System | SA File(s) | CLOUT File | What Changed |
|-----------|-----------|------------|-------------|
| Object Pooling | ObjectPooler.cs, ObjectPoolAsset.cs | Utils/ObjectPooler.cs, ObjectPoolConfig.cs | Auto-expand pools, delayed return, DontDestroyOnLoad, typed API |
| Item Database | ResourcesManager.cs | Utils/ResourceDatabase.cs | Generic typed lookup, auto-init singleton, multi-type support |
| Event System | EventsManager.cs | Utils/EventBus.cs | String-based → type-safe generics, game event structs defined |
| Save System | Serialization.cs, SaveFile.cs, SaveableMonobehavior.cs | Save/SaveManager.cs | BinaryFormatter → JSON, 3 slots, versioning, full empire data model |
| Clothing | ClothManager.cs, ClothItem.cs, ClothItemHook.cs | Player/AppearanceManager.cs | Synty modular character support, palette variants, disguise system |
| Consumables | Consumable.cs, ConsumableHolder.cs, AddStatOverTime.cs | Inventory/ConsumableItem.cs | SO-driven effects, tick-based healing, multiple effect types |
| Doors | DoorHook.cs | World/Interactables/DoorInteractable.cs | Scene transitions, lock system, auto-close, rotation override |
| Pickups | PickableHook.cs | World/Interactables/PickupInteractable.cs | Multi-type items, respawn, animation, quantity support |
| Destructibles | DestructiblePropObject.cs, ShootableHook.cs | World/Interactables/DestructibleProp.cs | Combined into one, health pool, pooled VFX, loot drops, reset |
| Interactions | IInteractable, IShootable, IDamageable | Core/Interactables.cs | InteractionType enum, IDestructible, IPickupable added |
| Stats Over Time | AddStatOverTime.cs (LogicHook) | Inventory/ConsumableItem.cs (StatOverTimeEffect) | Self-destroying MonoBehaviour, supports health + stamina |

### Systems NOT in Sharp Accent (Must Build from Scratch)

These systems are **100% original CLOUT code** — no Sharp Accent foundation exists:

| System | Why It's New | Phase |
|--------|-------------|-------|
| Dealing / Trading | SA has no NPC commerce | Phase 2 |
| Production / Cooking | SA has no crafting | Phase 2 |
| Property Management | SA has no building ownership | Phase 2 |
| Workforce / Employees | SA has no NPC management | Phase 2 |
| Economy / Pricing | SA has no dynamic economy | Phase 2 |
| Police / Wanted AI | SA has no law enforcement | Phase 2 |
| Territory Control | SA has no zone system | Phase 3 |
| Vehicle System | SA has no vehicles | Phase 3 |
| Day/Night Cycle | SA has no time system | Phase 3 |
| Money Laundering | SA has no financial sim | Phase 3 |
| Command Mode | SA has no RTS layer | Phase 3 |

### Sharp Accent Systems Already Ported in Phase 1

These were ported in the initial Phase 1 sprint and form the combat backbone:

| SA System | Lesson Refs | CLOUT Location |
|-----------|------------|----------------|
| State Machine | L9-10, L18-19 | Core/StateManager.cs, State.cs, StateAction.cs |
| Character Controller | L26-28 | Core/CharacterStateManager.cs, Player/PlayerStateManager.cs |
| Melee Combat | L4, L9-15, L20-21, L43-44, L90, L95-96, L100 | Combat/AttackAction.cs, DamageCollider.cs |
| Ranged Combat | COD-like all seasons | Combat/RangedAttackAction.cs, RangedWeaponHook.cs |
| Weapon System | L157-162 | Combat/WeaponItem.cs, WeaponHolderManager.cs |
| Camera System | L27, L37, L75-76 | Camera/CameraManager.cs, CameraCollision.cs |
| Animation | L13, L109 | Animation/AnimatorHook.cs |
| AI Utility Theory | L30-34, L70, L80-81 | AI/AIStateManager.cs, AI/Actions/* |
| Inventory Base | L60-67 | Inventory/InventoryManager.cs |
| Lock-On | L5-6, L16, L75-76, L88 | Core/Interfaces.cs (ILockable) |
| Combo System | L14, L20, L114, L121 | Core/Interfaces.cs (Combo), Actions/InputsForCombo.cs |
| Input System | L35 | Player/PlayerInputHandler.cs |

### What's Left Unported (Low Priority for Phase 2)

| SA System | SA Files | Why Deferred |
|-----------|----------|-------------|
| Icon Maker | IconMakerAsset.cs, IconMakerActual.cs | Cosmetic — need when building full inventory UI |
| Match System | MatchManager.cs (11 versions) | PvP multiplayer — Phase 4 |
| Leaderboard | LeaderboardsManager.cs | Multiplayer feature — Phase 4 |
| Level Generator | LevelManager.cs, LevelTemplate.cs | Procedural levels — Phase 5 |
| Bonfire/Checkpoint | BonefireHook.cs | Checkpoint saves — Phase 3 (adapt for safehouses) |
| UI Navigation | NavigateManager.cs, NavigatableGroupManager.cs | Gamepad menu nav — Phase 5 polish |
| FPS Handler | FPSHandler.cs, TPSHandler.cs | FPS camera mode — Phase 3 (optional) |
| Network (SA) | NetworkManager.cs, NetworkPrint.cs | Already replaced by FishNet |

### Total Script Inventory

| Category | Scripts |
|----------|---------|
| Phase 1 (combat foundation) | 62 |
| Phase 1.5 (Sharp Accent ports + fixes) | 11 |
| Phase 2 Step 1 (SO data, weapons, animator, editor tools) | 5 |
| Phase 2 Step 2 (dealing system) | 10 |
| Phase 2 infrastructure (URP setup, shader helper) | 2 |
| **Current Total** | **~90** |
| Phase 2 remaining (Steps 3–10) | ~43 |
| **Projected After Phase 2** | **~133** |

*CLOUT Phase 2 Masterclass Plan v1.1 — SlicedLabs — March 2026*
