# CLOUT — Build Specification v1.0

## Low-Poly Crime Empire Simulator | AAA-Grade Design Document

**Project:** CLOUT (Working Title)
**Studio:** SlicedLabs
**Engine:** Unity 6 (6000.x) | URP Pipeline
**Target Platforms:** PC (Steam), Console (Future)
**Multiplayer:** 1–4 Players (Co-op + Competitive PvPvE)
**Art Style:** Synty POLYGON Low-Poly
**Document Version:** 1.0 — March 2026
**Author:** the_architect × Claude (AI-Assisted Design)

---

## Table of Contents

1. Executive Vision
2. Core Game Identity
3. Game Loop Architecture
4. Player Controller System
5. Combat Systems
6. Empire Management System
7. Production & Cooking System
8. Territory Control System
9. Crime & Heist System
10. Law Enforcement & Heat System
11. AI Architecture
12. Economy Simulation
13. NPC & Workforce System
14. Open World Design
15. Vehicle System
16. Property & Base System
17. Reputation & Influence System
18. Corruption & Political System
19. Multiplayer Architecture
20. Save & Serialization System
21. UI/UX Design Specification
22. Audio Design
23. Visual Effects & Post-Processing
24. Procedural Systems
25. Progression & Skill System
26. Next-Level Features
27. Technical Architecture
28. Development Phases
29. Performance Targets
30. Asset Pipeline & Art Direction

---

## 1. Executive Vision

CLOUT is a systemic crime sandbox that merges the cinematic freedom of Grand Theft Auto with the empire-building depth of Schedule I, elevated by souls-like combat precision and tactical FPS gunplay — all wrapped in a distinctive POLYGON low-poly art style.

The player starts as a nobody arriving in a grungy coastal city. Through street-level hustling, strategic empire building, territory warfare, and political corruption, they rise to become the undisputed kingpin — or fall spectacularly trying.

What separates CLOUT from every other crime game on the market:

- **Deep combat** — Melee combat with parry, backstab, and combo systems inherited from a battle-tested souls-like codebase. FPS gunplay with recoil, ADS, stances, and weapon sway from a COD-like codebase. No other crime sim has this level of combat depth.
- **Empire simulation on steroids** — Production chains, supply logistics, workforce management, territory control, automated distribution, money laundering businesses, and a living economy that reacts to player actions.
- **Multiplayer territory wars** — Not just co-op empire building, but competitive PvPvE where rival player empires fight for territory, customers, and supply routes.
- **The POLYGON aesthetic** — A consistent, moddable, performant visual identity using Synty Studios' ecosystem. Stylized violence meets neon-lit low-poly cityscapes with sharp lighting and dark atmosphere.

**Market Validation:** Schedule I generated $125M+ revenue from 8M+ copies as a solo-dev project with shallow combat. GTA V continues to generate billions. The intersection of deep combat, empire simulation, and multiplayer in a stylized low-poly package is an unoccupied market position.

---

## 2. Core Game Identity

### Pillars

1. **Player Agency** — Every system feeds into player choice. There is no single path to power.
2. **Systemic Depth** — Systems interact with each other organically. Flooding the market crashes prices. Bribing cops reduces heat. Betraying allies triggers gang wars.
3. **Emergent Stories** — The game generates narratives through system interactions, not scripted sequences. Betrayals, police raids, rival ambushes, market crashes — these emerge from gameplay.
4. **Skill Expression** — Combat rewards mechanical skill. Empire management rewards strategic thinking. Both paths lead to power.
5. **Stylized Brutality** — The POLYGON art style enables stylized violence that's visually distinctive without being gratuitous. Neon-lit streets, sharp shadows, rain-slicked asphalt.

### Tone & Atmosphere

- Dark comedy meets gritty crime drama
- Neon-lit low-poly city at night
- Sharp, high-contrast lighting with volumetric fog
- Stylized violence with impactful hit feedback (screen shake, freeze frames, particle bursts)
- Ambient city sounds: distant sirens, bass from nightclubs, street chatter
- Dynamic weather: rain, fog, clear nights, overcast days

### Genre Fusion

| Genre | Contribution |
|-------|-------------|
| Open World (GTA) | Freedom, traversal, emergent chaos |
| Business Sim (Schedule I) | Production chains, workforce, empire management |
| Action RPG (Souls-like) | Melee combat depth, skill-based encounters |
| Tactical Shooter (COD) | FPS gunplay, ADS, weapon handling |
| Strategy (RTS) | Command mode, territory control, resource management |
| Roguelike | Procedural events, permadeath mode, unpredictability |

---

## 3. Game Loop Architecture

### Primary Loop (Session-Level)

```
HUSTLE → EXPAND → CONTROL → DOMINATE → (HEAT CRISIS) → ADAPT
```

### Phase 1: Hustle (Early Game — Hours 0–5)

- Street-level dealing: buy low, sell high
- Small robberies: convenience stores, muggings
- Scouting locations for future operations
- Meeting initial contacts and suppliers
- Learning the city layout and district dynamics
- First stash house acquisition
- Building initial reputation

**Key Systems Active:** Player controller, basic combat, inventory, basic economy, NPC dealing

### Phase 2: Expand (Mid Game — Hours 5–20)

- Hire first dealers and enforcers
- Set up production (grow ops, basic labs)
- Claim first territory through combat or influence
- Establish supply routes
- Purchase first properties
- Begin money laundering through legitimate businesses
- Rival gang encounters begin

**Key Systems Active:** All Phase 1 + workforce management, production chains, territory system, property system

### Phase 3: Control (Late Game — Hours 20–50)

- Defend territory from rivals and police
- Optimize supply chains for maximum efficiency
- Automate production and distribution
- Manage heat across multiple operations
- Bribe cops and politicians
- Expand into premium products
- Large-scale territory wars

**Key Systems Active:** All Phase 2 + corruption system, advanced AI, full economy simulation, command mode

### Phase 4: Dominate (Endgame — Hours 50+)

- Plan and execute large-scale heists
- Political influence and manipulation
- Full map control attempts
- Rival empire elimination
- Endgame crisis events (federal investigations, cartel invasions)
- Legacy systems and prestige mechanics

**Key Systems Active:** All systems at full capacity

### Secondary Loops (Moment-to-Moment)

**Combat Loop:** Encounter → Assess → Engage (melee/ranged) → Loot → Heal → Move on
**Dealing Loop:** Acquire product → Mix/process → Price → Find buyer → Deal → Evade heat
**Management Loop:** Check operations → Assign workers → Optimize routes → Handle problems → Collect profits

---

## 4. Player Controller System

### Architecture

The player controller is a **unified hybrid system** merging two Sharp Accent codebases:

- **Souls-like codebase** → 3rd person controller, camera, melee combat, inventory, state machine
- **COD-like codebase** → FPS controller, shooting, ADS, recoil, stances

Both use **ScriptableObject-driven state machines** with the **strategy pattern**, making them architecturally compatible.

### Controller Modes

| Mode | Camera | Movement | Combat | Source |
|------|--------|----------|--------|--------|
| On Foot (3rd Person) | Over-shoulder, homebrew camera | Root motion + NavMesh hybrid | Melee + ranged | Souls-like base |
| On Foot (1st Person) | FPS camera with sway | Physics-based | Ranged primary | COD-like base |
| Driving | Follow cam / hood cam / chase cam | Vehicle physics | Drive-by shooting | New system |
| Command Mode | Top-down tactical | Point-and-click | None (strategic) | New system |

### Movement Specification

- **Walk/Run/Sprint** with stamina system
- **Crouch/Prone** stances (from COD-like)
- **Parkour lite** — vault low cover, climb fences, slide under obstacles
- **NavMesh grounding** with root motion (Sharp Accent lesson 18–19, 26, 28)
- **Foot IK** for terrain adaptation (Sharp Accent lesson 109)
- **Roll/Dodge** with i-frames (Sharp Accent lesson 17, 100)

### Camera System

- **Homebrew 3rd person camera** (Sharp Accent lesson 27) — more control than Cinemachine
- **Camera collisions** (Sharp Accent lesson 37)
- **Lock-on system** for melee encounters (Sharp Accent lessons 5–6, 16, 75–76, 88)
- **FPS camera** with procedural sway and recoil (COD-like lessons 8–9)
- **Smooth perspective switching** — 3rd person to 1st person via animation blend
- **Driving camera** — multiple presets (chase, hood, cinematic)

### Input System

- **Unity Input System** (New) — Sharp Accent lesson 35 migrated
- **Gamepad support** — full controller mapping with radial menus
- **Mouse + keyboard** — standard PC FPS/TPS bindings
- **Context-sensitive inputs** — same button does different things based on state (interact, deal, enter vehicle, pick lock)

### State Machine Architecture

```
PlayerStateManager (ScriptableObject-driven)
├── LocomotionState
│   ├── IdleAction
│   ├── MoveAction
│   ├── SprintAction
│   └── CrouchAction
├── CombatState
│   ├── MeleeAttackAction (combo-driven)
│   ├── RangedAttackAction (ADS/hip-fire)
│   ├── BlockAction
│   ├── ParryAction
│   ├── RollAction
│   └── BackstabAction
├── InteractionState
│   ├── DealAction
│   ├── PickupAction
│   ├── DoorAction
│   └── VehicleEnterAction
├── DrivingState
│   ├── DriveAction
│   ├── DriveByAction
│   └── ExitVehicleAction
└── CommandState
    ├── TacticalViewAction
    ├── AssignWorkerAction
    └── PlanRouteAction
```

Each action is a **ScriptableObject** (Sharp Accent's modular action pattern from lessons 9–10), allowing runtime swapping and data-driven behavior.

---

## 5. Combat Systems

### 5A. Melee Combat (Souls-like Foundation)

**Source:** Sharp Accent Souls-like codebase (lessons 4, 9–15, 20–21, 43–44, 68, 90, 95–96, 100, 114–115, 121)

#### Weapons

| Category | Examples | Speed | Damage | Reach |
|----------|----------|-------|--------|-------|
| Fists | Unarmed | Very Fast | Low | Short |
| Knives | Switchblade, Kitchen knife | Fast | Medium | Short |
| Bats | Baseball bat, Pipe | Medium | High | Medium |
| Machetes | Machete, Cleaver | Medium | Very High | Medium |
| Heavy | Sledgehammer, Fire axe | Slow | Extreme | Long |

#### Combo System

- **Dynamic combos** (Sharp Accent lesson 14, 20) — input-driven branching
- **Animation events** for hit detection (Sharp Accent lesson 13)
- **Root motion attacks** for weighted, impactful swings (Sharp Accent lesson 4)
- **Rotation on events** — players can redirect mid-combo (Sharp Accent lesson 15)
- **Left-hand combos** for dual wielding (Sharp Accent lesson 121)
- **Stacking attacks** for extended sequences (Sharp Accent lesson 114)

#### Defensive Mechanics

- **Parry** — timed block that opens enemy for riposte (Sharp Accent lessons 43–44, 90, 95)
- **Backstab** — positional instant-kill on unaware enemies (Sharp Accent lessons 44, 90, 95–96)
- **Block** — damage reduction with stamina cost (Sharp Accent lesson 68)
- **Roll** — i-frame dodge with directional control (Sharp Accent lessons 17, 100)
- **Step back** — quick retreat to create distance (Sharp Accent lesson 21)

#### Hit Response System

- **Actions on damage** (Sharp Accent lesson 39) — stagger, knockback, knockdown
- **Roll touch damage** — rolling through destructibles (Sharp Accent lesson 111)
- **Damage types** — blunt (stagger), sharp (bleed), impact (knockdown)

### 5B. Ranged Combat (COD-like Foundation)

**Source:** Sharp Accent COD-like codebase (all seasons)

#### Weapons

| Category | Examples | Fire Mode | Recoil | Range |
|----------|----------|-----------|--------|-------|
| Pistols | Revolver, 9mm, .45 | Semi-auto | Low | Short |
| SMGs | MAC-10, Uzi | Full-auto | Medium | Medium |
| Shotguns | Pump, Sawed-off | Pump/Semi | High | Short |
| Rifles | AK-47, AR-15 | Full/Semi | High | Long |
| Sniper | Hunting rifle | Bolt | Very Low | Very Long |
| Special | RPG, Molotov, Grenades | Single | — | Varies |

#### Gunplay Mechanics

- **ADS (Aim Down Sights)** with weapon-specific zoom (COD-like lesson 7)
- **Recoil patterns** — learnable spray patterns per weapon (COD-like lesson 5)
- **Weapon sway** — non-input-based procedural sway (COD-like lesson 6)
- **Stances affect accuracy** — standing < crouching < prone (COD-like lesson 6)
- **Shootable objects** — destructible cover, explosive barrels (COD-like lesson 5)
- **Bullet penetration** — material-based wall penetration
- **Hip-fire spread** — cone-based accuracy degradation

#### Cover System

- **Soft cover** — crouch behind objects, peek and shoot
- **Destructible cover** — low-poly objects fragment under fire
- **Blind fire** — reduced accuracy but safe from return fire

### 5C. Combat Integration

Both melee and ranged combat share:

- **Unified damage system** — ScriptableObject damage types with effects
- **Stats handler** (Sharp Accent lesson 57) — health, stamina, armor calculations
- **Effects over time** (Sharp Accent lesson 58) — bleed, burn, poison
- **Attributes framework** (Sharp Accent lesson 108) — stat-based damage scaling
- **Object pooler** for projectiles and effects (Sharp Accent lesson 40)

---

## 6. Empire Management System

The empire system is the strategic backbone of CLOUT. It transforms Schedule I's management loop into a fully realized criminal business simulator.

### Empire Hierarchy

```
PLAYER (Kingpin)
├── Properties
│   ├── Safehouses (personal storage, respawn)
│   ├── Production Facilities (labs, grow houses, kitchens)
│   ├── Warehouses (bulk storage, distribution hubs)
│   ├── Storefronts (money laundering fronts)
│   └── Luxury Properties (status, passive income)
├── Workforce
│   ├── Dealers (street distribution)
│   ├── Enforcers (territory defense, intimidation)
│   ├── Chemists (production, mixing)
│   ├── Botanists (cultivation)
│   ├── Drivers (logistics, smuggling)
│   ├── Janitors (cleanup, maintenance)
│   └── Accountants (money laundering efficiency)
├── Supply Chains
│   ├── Raw Material Sources (suppliers, imports)
│   ├── Production Pipelines (multi-stage processing)
│   ├── Distribution Routes (dealer networks)
│   └── Money Laundering Channels (business fronts)
└── Territory
    ├── Controlled Zones (income generating)
    ├── Contested Zones (active conflict)
    └── Uncontrolled Zones (expansion targets)
```

### Management Interface

**The Clipboard** — In-world management tool (inspired by Schedule I's phone system)

- Real-time empire overview
- Worker assignment and routing
- Financial reports
- Heat level monitoring
- Territory status map
- Supply chain visualization

**Command Mode** — The game's unique tactical layer

- Top-down camera over the city
- Assign routes, workers, and operations
- Plan territory expansion
- Monitor rival movements
- Set defensive positions
- This is where CLOUT separates from every competitor

---

## 7. Production & Cooking System

### Architecture

Production uses a **ScriptableObject-driven recipe system** built on Sharp Accent's inventory framework.

### Data Model

```csharp
[CreateAssetMenu(menuName = "CLOUT/Production/Product")]
public class ProductSO : ScriptableObject
{
    public string productName;
    public ProductType baseType;        // Weed, Crystal, Powder, Shroom
    public float baseValue;
    public float basePotency;
    public float baseAddictiveness;
    public List<EffectSO> inherentEffects;
    public Sprite icon;
    public GameObject worldModel;
}

[CreateAssetMenu(menuName = "CLOUT/Production/Ingredient")]
public class IngredientSO : ScriptableObject
{
    public string ingredientName;
    public float cost;
    public List<EffectModifier> effectModifiers;
    public float potencyModifier;
    public float addictionModifier;
}

[CreateAssetMenu(menuName = "CLOUT/Production/Recipe")]
public class RecipeSO : ScriptableObject
{
    public ProductSO baseProduct;
    public List<IngredientSO> ingredients;
    public ProductSO resultProduct;
    public float qualityMultiplier;
    public float productionTime;
    public int requiredSkillLevel;
}
```

### Production Chain

```
RAW MATERIALS → PROCESSING → MIXING → PACKAGING → DISTRIBUTION
     ↓              ↓           ↓          ↓            ↓
  Suppliers    Equipment   Ingredients   Supplies    Dealers
  Seeds        Labs        Additives     Bags        Routes
  Chemicals    Grow Ops    Stations      Scales      Stashes
```

### Mixing Mechanics

- **Base products:** 4+ drug types with distinct production processes
- **Ingredients:** 20+ additives, each modifying effects, value, and risk
- **Chained mixing:** Output of one station becomes input of the next (daisy-chaining up to 4 stations)
- **Effect system:** Each ingredient adds/removes/transforms effects on the product
- **Quality tiers:** Trash → Street → Premium → Pure → Legendary
- **Discovery system:** New recipes are discovered through experimentation
- **Automation:** Chemists and handlers automate production chains once configured
- **Comedic effects:** Products can cause visual effects on NPCs (color changes, size shifts, behavior changes)

### Production Facilities

| Facility | Function | Capacity | Upgrade Path |
|----------|----------|----------|-------------|
| Grow House | Cultivation | 4–16 plants | Lighting, irrigation, climate |
| Basement Lab | Basic mixing | 1–2 stations | Ventilation, equipment |
| Industrial Lab | Advanced production | 4–8 stations | Automation, security |
| Kitchen | Food-front production | 2–4 stations | Quality equipment |
| Warehouse | Storage + distribution | 100–1000 units | Shelving, security, loading dock |

---

## 8. Territory Control System

### Zone Architecture

The city is divided into **districts**, each containing **zones**.

```
CITY
├── Downtown (4 zones) — High value, high police presence
├── Industrial (4 zones) — Low police, warehouse space
├── Slums (4 zones) — Low cost, high demand, gang activity
├── Suburbs (4 zones) — Medium value, residential customers
├── Waterfront (4 zones) — Smuggling routes, nightlife
├── Arts District (2 zones) — Premium customers, low demand
└── Outskirts (2 zones) — Supplier access, low oversight
```

### Zone Properties

Each zone tracks:

- **Control Level** (0–100%) — Determines income and influence
- **Heat Level** (0–100%) — Police attention on this zone
- **Demand Profile** — What products sell well here
- **Wealth Level** — Affects pricing and customer types
- **Rival Presence** — Gang activity and competition
- **Infrastructure** — Available properties and businesses

### Conquest Mechanics

Territories can be claimed through:

1. **Combat** — Defeat rival gang members in the zone
2. **Influence** — Outcompete rivals through better product, lower prices, more dealers
3. **Corruption** — Bribe local police to look the other way
4. **Intimidation** — Send enforcers to drive out competitors
5. **Economic warfare** — Flood market to crash rival profits

### Defense Mechanics

- **Enforcers** patrol controlled zones
- **Security cameras** on properties provide early warning
- **Stash diversification** — don't put all product in one location
- **Alliance system** — temporary truces with rival gangs
- **Ambush detection** — high-loyalty workers warn of incoming threats

---

## 9. Crime & Heist System

### Crime Types

#### Street Crime (Available from start)

- **Mugging** — Quick cash, low risk, builds combat skill
- **Store robbery** — Moderate cash, triggers police response
- **Car theft** — Acquire vehicles, sell to chop shops
- **Pickpocketing** — Stealth-based, no combat required
- **Drug deals** — Core income loop

#### Organized Crime (Mid–Late game)

- **Convoy hijacking** — Steal rival shipments
- **Bank robbery** — High reward, extreme heat
- **Assassinations** — Contract kills for cash or territory
- **Smuggling runs** — Move product across district borders
- **Cartel raids** — Attack rival production facilities

#### Heist System (Endgame)

Each heist has three phases:

**Planning Phase:**
- Choose target (bank, casino, warehouse, armored transport)
- Select crew members (each has specialties)
- Choose approach: Stealth, Loud, or Deception
- Acquire equipment (masks, tools, vehicles, weapons)
- Study target schedules and security

**Execution Phase:**
- Real-time gameplay shifts based on approach
- Dynamic complications (alarms, witnesses, reinforcements)
- Crew AI follows assigned roles
- Optional objectives for bonus rewards
- Player choices affect outcome

**Escape Phase:**
- Police response scales with heist profile
- Multiple escape routes
- Getaway vehicle management
- Heat distribution across crew
- Loot splitting and fence operations

---

## 10. Law Enforcement & Heat System

### Heat Architecture

Unlike GTA's binary wanted stars, CLOUT uses a **multi-layered heat system**:

```
HEAT SYSTEM
├── Local Heat (per zone)
│   ├── Police patrol frequency
│   ├── Random search probability
│   └── Checkpoint activation
├── City Heat (aggregate)
│   ├── Investigation progress
│   ├── Special units deployment
│   └── Curfew enforcement
└── Federal Heat (endgame)
    ├── Surveillance operations
    ├── Undercover agents
    ├── Asset seizure warrants
    └── RICO investigation progress
```

### Heat Mechanics

**Heat Generation:**
- Dealing in public (+small)
- Police witness crime (+medium)
- High-profile crimes (+large)
- Unsatisfied customers snitching (+variable)
- Rival gang tip-offs (+variable)

**Heat Reduction:**
- Time passage (slow natural decay)
- Bribing officers (immediate reduction)
- Laying low (no criminal activity)
- Using fronts for laundering (reduces financial heat)
- Eliminating witnesses (high risk, high reward)

**Heat Consequences:**

| Heat Level | Effect |
|-----------|--------|
| 0–20% | Normal operations |
| 20–40% | Increased patrols, random searches |
| 40–60% | Checkpoints, surveillance vans |
| 60–80% | Raids on properties, undercover NPCs |
| 80–100% | Federal investigation, asset seizure, SWAT response |

### Police AI Behavior

- **Patrol routes** — Semi-random with weighted zones
- **Investigation system** — Cops follow evidence chains
- **Raid system** — Coordinated assaults on known stash houses
- **Undercover agents** — NPCs that infiltrate your operation (look like regular customers)
- **K-9 units** — Drug detection at checkpoints
- **SWAT response** — Triggered by extreme heat or violent crimes

---

## 11. AI Architecture

### AI Framework

Built on Sharp Accent's **utility theory AI** (lessons 30–34, 70, 80–81) extended with behavior trees.

### NPC Layers

#### Civilians

- **Schedule system** — Wake, commute, work, shop, home, sleep
- **Reaction system** — Flee from gunfire, call police on witnessed crimes, gossip about player reputation
- **Customer potential** — Some civilians can become customers based on approach
- **Witness system** — Civilians report crimes to police

#### Police

- **Patrol AI** — Route-based with investigation detours
- **Pursuit AI** — Chase player on foot and in vehicles
- **Investigation AI** — Follow evidence, conduct stakeouts
- **Raid AI** — Coordinated team tactics for property raids
- **Corruption AI** — Bribed cops have modified behavior trees

#### Rival Gangs

- **Territory AI** — Expand, defend, retreat based on strength
- **Economy AI** — Run their own drug operations
- **Combat AI** — Squad-based tactics with role assignment
- **Diplomacy AI** — Offer truces, demand tribute, declare war
- **Adaptation AI** — Change strategies based on player actions

#### Workforce (Player's Crew)

- **Dealer AI** — Follow routes, make deals, evade police, return profits
- **Enforcer AI** — Patrol territory, engage threats, intimidate rivals
- **Production AI** — Operate stations, follow recipes, manage inventory
- **Driver AI** — Follow delivery routes, evade checkpoints
- **Loyalty system** — Workers can betray based on pay, respect, and fear

### AI Personality System

Each NPC has personality traits stored as ScriptableObjects:

```csharp
[CreateAssetMenu(menuName = "CLOUT/AI/Personality")]
public class PersonalitySO : ScriptableObject
{
    [Range(0,1)] public float aggression;
    [Range(0,1)] public float loyalty;
    [Range(0,1)] public float greed;
    [Range(0,1)] public float courage;
    [Range(0,1)] public float intelligence;
    [Range(0,1)] public float corruptibility;
}
```

These traits feed into utility calculations for action selection, making every NPC behave uniquely.

---

## 12. Economy Simulation

### Market Model

Each district has independent supply/demand curves:

```
DEMAND = BaseDemand × PopulationFactor × AddictionFactor × CompetitionFactor
PRICE  = BasePrice × (Demand / Supply) × QualityMultiplier × HeatPenalty
PROFIT = (SellPrice - ProductionCost - DistributionCost) × Volume
```

### Economic Mechanics

- **Supply and demand per district** — Flooding a market crashes prices
- **Price fluctuation** — Dynamic based on availability and competition
- **Addiction economics** — Addicted customers pay more but attract heat
- **Scarcity premiums** — Cutting supply to a district raises prices
- **Competition pricing** — Rival gangs undercut player prices
- **Quality tiers** — Premium product commands exponential price increases
- **Seasonal demand** — Events, weekends, and holidays affect demand
- **Black market economy** — Weapons, vehicles, information all have prices

### Money Laundering

Revenue must be laundered to be usable for major purchases:

| Business Front | Laundering Rate | Cost | Monthly Overhead |
|---------------|----------------|------|-----------------|
| Laundromat | 15% per day | Low | Low |
| Restaurant | 25% per day | Medium | Medium |
| Nightclub | 40% per day | High | High |
| Car Wash | 20% per day | Low | Low |
| Real Estate | 60% per deal | Very High | Low |
| Casino | 50% per day | Very High | Very High |

Unlaundered cash triggers federal investigation if used for large purchases.

---

## 13. NPC & Workforce System

### Customer System

- **Customer types** — Casual users, addicts, premium clients, bulk buyers
- **Preference system** — Each customer prefers specific effects and products
- **Addiction progression** — Casual → Regular → Dependent → Addicted
- **Loyalty system** — Consistent quality and reliability builds loyalty
- **Snitch risk** — Unhappy or desperate customers may inform police
- **Phone ordering** — Established customers order via phone
- **Street deals** — Addicted customers hunt player down for deals

### Workforce Management

Each employee has:

- **Stats:** Skill, Loyalty, Stealth, Speed, Combat
- **Salary:** Weekly payment requirement
- **Morale:** Affected by pay, danger, player reputation
- **Betrayal risk:** Low loyalty + high greed = potential snitch
- **Skill growth:** Workers improve over time through experience
- **Permadeath:** Workers killed in raids or fights are permanently lost

### Recruitment

- **Street recruitment** — Find and convince NPCs to join
- **Reputation-gated** — Better workers available at higher rep levels
- **Referral system** — Existing workers recommend new hires
- **Rival poaching** — Steal workers from rival gangs
- **Prison connections** — Recruit specialized workers through jail contacts

---

## 14. Open World Design

### City Layout: Hyland Point (Working Name)

The city is designed with **Synty POLYGON modularity** in mind. Each district uses specific Synty packs for visual identity.

| District | Packs Used | Character | Activities |
|----------|-----------|-----------|------------|
| Downtown | City, Office | Corporate, upscale | Money laundering fronts, premium dealing |
| Industrial | Gang Warfare, Construction | Gritty, abandoned | Labs, warehouses, smuggling |
| Slums | Town, Street | Run-down, dense | Street dealing, recruitment, turf wars |
| Suburbs | Neighborhood, Residential | Quiet, deceptive | Grow houses, stash houses |
| Waterfront | Palm City | Nightlife, tourism | Nightclubs, smuggling docks, high-end clients |
| Arts District | City, Shops | Hipster, creative | Boutique dealing, premium customers |
| Outskirts | Countryside elements | Rural, isolated | Supplier meetings, large-scale production |

### World Systems

- **Day/night cycle** — 24-minute real-time day (1 second = 1 minute)
- **Dynamic weather** — Rain, fog, clear, overcast (affects visibility and NPC behavior)
- **Traffic system** — NPC vehicles follow traffic rules, create dynamic obstacles
- **Pedestrian system** — NPCs walk schedules, react to events
- **Interactive interiors** — All player properties have full interiors
- **Dynamic signage** — Billboards and signs reflect player's empire status
- **Environmental storytelling** — Graffiti, police tape, burned-out buildings show territory history

---

## 15. Vehicle System

### Vehicle Types

| Category | Examples | Speed | Durability | Stealth | Capacity |
|----------|----------|-------|-----------|---------|----------|
| Skateboard | Cruiser, Longboard | Low | — | High | 0 |
| Motorcycle | Sport bike, Chopper | Very High | Low | Medium | 1 bag |
| Sedan | Economy, Luxury | Medium | Medium | High | Trunk |
| SUV | Standard, Armored | Medium | High | Medium | Large trunk |
| Van | Delivery, Custom | Low | Medium | Varies | Very Large |
| Truck | Box truck, Semi | Low | High | Low | Massive |
| Sports Car | Muscle, Exotic | Very High | Low | Low | Small trunk |
| Boat | Speedboat, Yacht | High | Medium | High | Variable |

### Driving Mechanics

- **Arcade-style physics** — Fun over realism, appropriate for low-poly aesthetic
- **Drive-by shooting** — AI or player passenger can shoot while driving
- **Vehicle damage** — Visual damage model, functional degradation
- **Vehicle storage** — Stash products in vehicles
- **Vehicle purchase** — Buy from dealerships or steal and mod
- **Custom paint** — Recolor vehicles at body shops
- **Police chases** — Evasion mechanics with pursuit AI

---

## 16. Property & Base System

### Property Types

```
PROPERTY SYSTEM
├── Safehouses
│   ├── Motel Room (starter, cheap, no upgrades)
│   ├── Apartment (mid-tier, basic customization)
│   ├── House (full customization, garage)
│   └── Penthouse (luxury, status symbol)
├── Production Facilities
│   ├── Basement Lab (hidden, limited capacity)
│   ├── Grow House (plant cultivation)
│   ├── Industrial Lab (large-scale production)
│   └── Kitchen (food-front production)
├── Commercial Properties
│   ├── Laundromat (laundering front)
│   ├── Restaurant (laundering + reputation)
│   ├── Nightclub (laundering + territory influence)
│   ├── Car Wash (laundering, cheap)
│   ├── Pawn Shop (fencing stolen goods)
│   └── Gun Shop (weapons supply)
└── Strategic Properties
    ├── Warehouse (bulk storage)
    ├── Garage (vehicle storage + modification)
    ├── Dock (smuggling operations)
    └── Rooftop (sniper positions, lookout)
```

### Upgrade System

Each property has upgrade trees:

- **Security:** Cameras, alarms, reinforced doors, guards
- **Efficiency:** Better equipment, automation, capacity
- **Stealth:** Sound insulation, hidden rooms, secret entrances
- **Comfort:** Morale bonuses for workers, rest quality for player
- **Automation:** Self-running production with minimal oversight

---

## 17. Reputation & Influence System

### Dual Reputation Model

**Fear** — Gained through violence, intimidation, eliminating rivals
- Advantages: Cheaper worker salaries, territorial submission, fewer challenges
- Disadvantages: Snitch risk increases, police attention, assassination attempts

**Respect** — Gained through fair dealing, quality product, community investment
- Advantages: Better workforce loyalty, customer loyalty, community protection
- Disadvantages: Rivals may see you as weak, slower territory expansion

### Reputation Tiers

| Tier | Name | Unlock |
|------|------|--------|
| 0 | Nobody | Starting state |
| 1 | Hustler | First territory, basic recruitment |
| 2 | Dealer | Production facilities, mid-tier workers |
| 3 | Supplier | Wholesale operations, corruption access |
| 4 | Boss | Multiple territories, heist planning |
| 5 | Kingpin | City-wide operations, political influence |
| 6 | Legend | Endgame content, prestige mechanics |

---

## 18. Corruption & Political System

### Corruptible Entities

| Entity | Cost | Effect | Risk |
|--------|------|--------|------|
| Beat Cop | Low | Ignores minor crimes in area | Low |
| Detective | Medium | Warns of investigations | Medium |
| Police Captain | High | Reduces heat in district | High |
| Judge | Very High | Reduces sentences, drops charges | Very High |
| Politician | Extreme | Policy changes, zoning permits | Extreme |
| Federal Agent | Near Impossible | Ultimate protection | Extreme |

### Corruption Mechanics

- **Relationship building** — Multiple interactions before bribery works
- **Maintenance costs** — Corrupted officials require ongoing payments
- **Exposure risk** — Corrupted officials can be investigated themselves
- **Chain reactions** — One exposed official can reveal entire corruption network
- **Political missions** — Campaign funding, opposition research, blackmail

---

## 19. Multiplayer Architecture

### Network Model

**Unity Netcode for GameObjects 2.0** with **Distributed Authority** mode

- **Host-based** — One player hosts, others join (peer-to-peer via Relay)
- **2–4 players** per session
- **Persistent world** — Host's save file is the canonical state
- **Drop-in/drop-out** — Players can join and leave without disrupting the session

### Multiplayer Modes

#### Co-op Empire (Default)

- All players share one empire
- Divide responsibilities: one manages production, another handles dealing, another does combat/heists
- Shared economy and territory
- Democratic decisions on major purchases (vote system)

#### Rival Empires (Competitive PvPvE)

- Each player runs their own empire in the same city
- Compete for territory, customers, and supply routes
- PvP combat enabled in contested zones
- Temporary alliances and betrayals
- Win condition: control majority of city territory

### Networked Systems

| System | Sync Model | Authority |
|--------|-----------|-----------|
| Player Movement | Client-authoritative with prediction | Owner |
| Combat | Server-authoritative | Host |
| Economy | Server-authoritative | Host |
| NPC AI | Server-authoritative | Host |
| Inventory | Owner-authoritative | Owner |
| Territory | Server-authoritative | Host |
| Production | Server-authoritative | Host |

### Technical Requirements

- **Unity Transport Package** for reliable/unreliable channels
- **Unity Relay** for NAT traversal (no port forwarding required)
- **Unity Lobby** for session management and matchmaking
- **Interest management** — Only sync nearby entities to reduce bandwidth
- **NetworkVariable** for state synchronization
- **RPCs** for event-driven communication
- **NetworkAnimator** for combat animation sync

---

## 20. Save & Serialization System

### Architecture

Built on Sharp Accent's 7-part serialization system (lessons 71–79) extended for massive state tracking.

### Save Data Structure

```
SaveFile
├── PlayerData (position, health, stats, inventory, skills)
├── EmpireData
│   ├── Properties (owned, upgrades, contents)
│   ├── Workforce (employees, assignments, stats)
│   ├── Territory (control levels, heat, infrastructure)
│   ├── Finances (cash, laundered, investments)
│   └── Reputation (fear, respect, tier)
├── WorldState
│   ├── NPCStates (alive/dead, relationships, schedules)
│   ├── EconomyState (prices, supply, demand per district)
│   ├── HeatState (local, city, federal levels)
│   ├── RivalGangStates (territory, strength, diplomacy)
│   └── CorruptionState (bribed officials, relationships)
├── ProgressionData (missions, discoveries, achievements)
└── Settings (preferences, keybinds, display)
```

### Implementation

- **JSON serialization** with encryption for save files
- **Auto-save** on property exit, deal completion, and timed intervals
- **Multiple save slots** (3 per profile)
- **Cloud save** support via Steam Cloud
- **Save file versioning** for backwards compatibility

---

## 21. UI/UX Design Specification

### UI Philosophy

- **Diegetic where possible** — Phone for management, in-world screens for cameras
- **Minimal HUD** — Health, stamina, cash, heat indicator, minimap
- **Context-sensitive** — Show relevant info only when needed
- **Controller-friendly** — All menus navigable with gamepad

### HUD Layout

```
┌─────────────────────────────────────────────────────┐
│ [Heat]                                    [Minimap] │
│ ●●●○○                                    ┌───────┐ │
│                                           │  MAP  │ │
│                                           └───────┘ │
│                                                     │
│                                                     │
│                                                     │
│                                                     │
│                                                     │
│ [Health]──────────  [Ammo/Weapon]                   │
│ [Stamina]─────────  [Quick Items]                   │
│ [Cash: $XXX,XXX]    [D-Pad Items]                   │
└─────────────────────────────────────────────────────┘
```

### Menu Systems

**Pause Menu:**
- Resume, Save, Load, Settings, Quit
- Empire overview (quick stats)

**Phone (In-Game Management):**
- Contacts (dealers, suppliers, workforce)
- Products (pricing, inventory)
- Map (territory, properties, routes)
- Finances (income, expenses, laundering)
- Messages (tips, warnings, orders)
- Camera feeds (property security)

**Inventory:**
- Grid-based inventory (Sharp Accent's navigateable system, lessons 60–67)
- Equipment slots (weapons, armor, tools)
- Quick-access wheel (4 items)
- Product inventory (separate from personal)

**Command Mode UI:**
- Top-down city map
- Worker cards with drag-and-drop assignment
- Route drawing tool
- Territory overlay (control %, heat %, income)
- Financial dashboard

**Deal UI:**
- Product selection wheel
- Price negotiation slider
- Customer mood indicator
- Risk assessment meter
- Quick-deal option for repeat customers

### UI Art Direction

- **Dark theme** with neon accent colors
- **Monospace font** for financial data
- **Sans-serif** for general text
- **Animated transitions** — slide, fade, scale
- **Color coding:** Green (money/safe), Red (danger/heat), Blue (information), Yellow (warning), Purple (premium)

---

## 22. Audio Design

### Sound Categories

**Ambient:** City traffic, distant sirens, wind, rain, nightclub bass, construction
**Combat:** Impact sounds, gunshots (with distance attenuation), bullet ricochets, glass breaking
**UI:** Menu clicks, phone notifications, cash register, deal confirmation
**Music:** Dynamic layered soundtrack responding to game state
**Voice:** NPC barks, deal dialogue, police radio chatter, crew communication

### Dynamic Music System

- **Exploration layers** — Chill lo-fi hip hop, ambient electronic
- **Tension layers** — Added when heat rises or rivals approach
- **Combat layers** — Full intensity during fights and chases
- **Heist layers** — Custom tracks for planning and execution phases
- **Victory stings** — Successful deals, territory captures, level ups

---

## 23. Visual Effects & Post-Processing

### URP Post-Processing Stack

- **Bloom** — Neon signs, streetlights, muzzle flash
- **Color grading** — Dark, desaturated with neon pops
- **Vignette** — Subtle darkening at edges, increases with heat
- **Chromatic aberration** — On damage, drug use effects
- **Film grain** — Subtle, increases in tense moments
- **Depth of field** — ADS focus, cinematic moments
- **Volumetric lighting** — God rays through windows, streetlight cones in fog
- **Screen shake** — On explosions, heavy melee hits, vehicle impacts

### Particle Systems

- **Muzzle flash** — Per-weapon VFX
- **Impact effects** — Material-specific (concrete chips, wood splinters, blood spray)
- **Environmental** — Rain, fog, smoke, dust
- **Production** — Cooking steam, chemical fumes, grow-light glow
- **UI particles** — Cash collection, level up, territory capture

---

## 24. Procedural Systems

### Procedural Crime Events

Random events that create emergent gameplay:

- **Rival ambush** — Gang members attack player in controlled territory
- **Police raid** — Cops storm a known property
- **Customer overdose** — Creates heat and moral choices
- **Supply shortage** — Supplier goes dark, prices spike
- **Snitch discovery** — Worker or customer is informing police
- **Turf war outbreak** — Two rival gangs fight near player territory
- **VIP customer** — High-value buyer with specific demands
- **Federal tip-off** — Major heat incoming, time to hide evidence
- **Natural disaster** — Flood/storm damages properties
- **Market crash** — Sudden price collapse in a product category

### Procedural NPC Generation

- **Name generation** — Cultural name pools per district
- **Appearance** — Synty modular character system with random combination
- **Personality** — Random trait assignment within district-appropriate ranges
- **Schedule** — Generated based on job type and district
- **Relationships** — Procedural social networks affecting loyalty and snitching

---

## 25. Progression & Skill System

### Skill Trees

```
COMBAT
├── Melee Mastery (damage, combo speed, parry window)
├── Firearms Proficiency (accuracy, recoil control, reload speed)
├── Toughness (health, armor effectiveness, stagger resistance)
└── Stealth (detection range, silent takedowns, lockpicking)

BUSINESS
├── Chemistry (product quality, recipe discovery, mixing speed)
├── Charisma (better deals, cheaper prices, faster recruitment)
├── Management (worker efficiency, automation, capacity)
└── Finance (laundering rate, investment returns, bribe costs)

STREET
├── Driving (vehicle handling, pursuit evasion, drive-by accuracy)
├── Intimidation (fear generation, territory defense, surrender chance)
├── Connections (supplier access, informant network, prison contacts)
└── Survival (heat reduction, escape routes, safe house quality)
```

### XP Sources

- **Combat XP** — From fights, heists, defending territory
- **Business XP** — From deals, production, management
- **Street XP** — From driving, exploration, crime events

---

## 26. Next-Level Features (Push It Further)

### Permadeath Mode

- Lose everything on death — restart from nothing
- Leaderboard for longest-surviving empires
- Unique starting conditions each run
- Roguelike territory layouts

### Reputation Fear vs Respect System (Deep)

- NPCs remember and gossip about player actions
- City-wide reputation affects ALL interactions
- Legendary status unlocks unique content
- Infamy system — most wanted lists, bounties

### Corruption System (Deep)

- Full political simulation
- Campaign financing mini-game
- Blackmail system with evidence collection
- Judicial manipulation
- Policy influence (decriminalization, zoning changes)

### Procedural Narrative Events

- Dynamic story arcs generated by system interactions
- Betrayal narratives when loyalty drops
- Rise-and-fall story structure
- News broadcasts reporting player actions
- Social media simulation (in-game) tracking reputation

### Mod Support

- Custom products and recipes (SO-based, easy to add)
- Custom character models (Synty-compatible)
- Custom properties and interiors
- Custom AI behaviors
- Steam Workshop integration

---

## 27. Technical Architecture

### Core Architecture

```
CLOUT Technical Stack
├── Unity 6 (6000.x) + URP
├── C# 12 / .NET Standard 2.1
├── ScriptableObject Architecture (Sharp Accent pattern)
│   ├── State Machine (strategy pattern)
│   ├── Event System (SO-based events)
│   ├── Database Management (SO catalogs)
│   └── Inventory Framework (SO-driven items)
├── AI System
│   ├── Utility Theory (Sharp Accent base)
│   ├── Behavior Trees (for complex sequences)
│   └── GOAP (for strategic AI decisions)
├── Networking
│   ├── Netcode for GameObjects 2.0
│   ├── Unity Transport Package
│   ├── Unity Relay (NAT traversal)
│   └── Unity Lobby (session management)
├── Navigation
│   ├── NavMesh (ground movement)
│   ├── A* Pathfinding (Sharp Accent's open-source)
│   └── Dynamic obstacles
├── Physics
│   ├── Unity Physics (rigidbody)
│   ├── Raycasting (combat, interaction)
│   └── Trigger volumes (zones, interactions)
├── Serialization
│   ├── JSON serialization
│   ├── Binary backup
│   └── Steam Cloud sync
└── Third-Party
    ├── Synty POLYGON assets
    ├── ThatAnimator animation packs
    ├── DOTween (animation/UI tweening)
    └── TextMeshPro (UI text)
```

### Assembly Definitions

```
CLOUT.Core          — Shared utilities, events, data structures
CLOUT.Player        — Controller, camera, input, states
CLOUT.Combat        — Melee, ranged, damage, effects
CLOUT.AI            — NPC behavior, utility, pathfinding
CLOUT.Empire        — Production, territory, workforce, economy
CLOUT.World         — City management, weather, traffic, time
CLOUT.UI            — All UI systems, HUD, menus, phone
CLOUT.Network       — Multiplayer sync, RPCs, network objects
CLOUT.Save          — Serialization, save/load, cloud sync
CLOUT.Audio         — Sound management, music system
CLOUT.VFX           — Particle systems, post-processing
```

---

## 28. Development Phases

### Phase 0: Foundation (Months 1–2)

- [ ] Merge Souls-like and COD-like codebases into unified project
- [ ] Establish ScriptableObject architecture and assembly definitions
- [ ] Import core Synty POLYGON packs
- [ ] Set up URP pipeline with base post-processing
- [ ] Build unified player controller (3rd person + FPS switching)
- [ ] Implement basic movement, camera, and input
- [ ] Create test scene with placeholder geometry

### Phase 1: Core Loop (Months 3–5)

- [ ] Melee combat system (attacks, combos, parry, block)
- [ ] Ranged combat system (shooting, ADS, recoil)
- [ ] Basic inventory and item system
- [ ] NPC dealing mechanic (approach, negotiate, exchange)
- [ ] Basic production (single mixing station)
- [ ] Simple economy (buy/sell with flat prices)
- [ ] One playable district with hand-built layout

**Milestone: Playable vertical slice — walk around, fight, deal, produce**

### Phase 2: Empire Systems (Months 6–9)

- [ ] Full production chain system
- [ ] Worker hiring and management
- [ ] Property purchase and basic upgrades
- [ ] Territory control (basic claim/defend)
- [ ] Heat system (local + city)
- [ ] Police AI (patrol, investigate, chase)
- [ ] Money system (cash + laundering basics)
- [ ] Save/load system

**Milestone: Full empire loop — produce, distribute, manage workers, hold territory**

### Phase 3: World & AI (Months 10–13)

- [ ] Full city layout (all districts)
- [ ] Vehicle system (driving, chase mechanics)
- [ ] Advanced AI (utility theory + behavior trees)
- [ ] Rival gang system
- [ ] Customer system with preferences and addiction
- [ ] Dynamic economy (supply/demand)
- [ ] Day/night cycle + weather
- [ ] Command mode

**Milestone: Living world — NPCs have schedules, economy fluctuates, rivals compete**

### Phase 4: Multiplayer (Months 14–17)

- [ ] Netcode for GameObjects integration
- [ ] Player synchronization (movement, combat, inventory)
- [ ] Shared empire mode (co-op)
- [ ] Rival empire mode (competitive)
- [ ] Lobby and session management
- [ ] Interest management (bandwidth optimization)
- [ ] Network testing and optimization

**Milestone: 2–4 player multiplayer functional**

### Phase 5: Polish & Content (Months 18–22)

- [ ] Heist system
- [ ] Corruption and political system
- [ ] Reputation system (fear/respect deep)
- [ ] Procedural events
- [ ] Full UI polish
- [ ] Audio implementation
- [ ] VFX polish
- [ ] Tutorial and onboarding
- [ ] Achievement system
- [ ] Permadeath mode

**Milestone: Feature-complete, content-rich, polished**

### Phase 6: Ship (Months 23–24)

- [ ] Performance optimization
- [ ] Bug fixing marathon
- [ ] Steam integration (achievements, cloud save, workshop)
- [ ] Trailer and marketing materials
- [ ] Community beta test
- [ ] Launch on Steam Early Access

---

## 29. Performance Targets

| Metric | Target | Minimum |
|--------|--------|---------|
| Frame Rate | 60 FPS | 30 FPS |
| Resolution | 1440p | 1080p |
| Load Time | <10 seconds | <20 seconds |
| Memory | <4 GB RAM | <6 GB RAM |
| VRAM | <4 GB | <6 GB |
| Network Bandwidth | <100 KB/s per player | <200 KB/s |
| Network Latency | <100ms acceptable | <200ms playable |
| Save File Size | <10 MB | <50 MB |
| City NPCs (Active) | 50+ simultaneously | 30 minimum |
| Draw Calls | <500 per frame | <1000 |

### Optimization Strategies

- **LOD system** — Synty assets support LOD levels
- **Occlusion culling** — Aggressive for dense city environments
- **Object pooling** — Sharp Accent's reusable pooler (lesson 40) for all spawned objects
- **Interest management** — Only simulate nearby zones in detail
- **Async loading** — Addressable Assets for district streaming
- **GPU instancing** — Synty assets share materials, perfect for instancing
- **Static batching** — All non-moving city geometry
- **NavMesh carving** — Dynamic obstacles only where needed

---

## 30. Asset Pipeline & Art Direction

### Art Direction

- **Style:** Synty POLYGON low-poly — clean geometric forms, flat colors, no textures needed
- **Lighting:** Dark, high-contrast. Neon signs as primary color sources. Volumetric fog.
- **Color Palette:** Desaturated base with neon pops (cyan, magenta, amber, red)
- **Time of Day:** Night is the primary aesthetic. Dawn/dusk for cinematic transitions.
- **Weather:** Rain is the signature weather state. Wet streets with reflections.

### Character Art

- **Player:** Synty Modular City Characters — full customization via modular pieces
- **Gang Members:** Gang Warfare pack characters + modular mix
- **Police:** Police Station pack characters
- **Civilians:** City Characters pack + Town Characters
- **Premium NPCs:** Custom assemblies from modular parts

### Environment Art

- **Buildings:** Synty modular building systems — snap-together architecture
- **Interiors:** Pack-specific interior sets (Office, Heist bank vaults, Gang Warfare labs)
- **Props:** Cross-pack prop library (hundreds of unique objects)
- **Vehicles:** Pack-specific vehicles supplemented with additional car packs
- **Vegetation:** Minimal — urban plants, park trees, potted plants

### Animation Pipeline

- **Player animations:** Souls-like Essential + Extended animation packs (ThatAnimator)
- **Combat animations:** Sword Set 1 adapted for melee weapons
- **NPC animations:** Synty animation packs
- **Custom animations:** Mixamo for gap-filling (retargeted to Synty rig)
- **Procedural animation:** Foot IK, weapon sway, head tracking

---

## End of Build Specification v1.0

This document represents the complete design vision for CLOUT. It is a living document that will be updated as development progresses and systems are implemented, tested, and refined.

The foundation is built on battle-tested code (Sharp Accent's Souls-like + COD-like), proven art assets (Synty POLYGON), and modern networking (Unity Netcode 2.0). The vision is ambitious but achievable through the phased development approach outlined above.

**Next Step:** Set up the Unity 6 project, import core assets, and begin Phase 0 — merging the codebases.

---

*CLOUT Build Specification v1.0 — SlicedLabs — March 2026*
