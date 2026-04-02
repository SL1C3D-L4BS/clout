# CLOUT -- Phase 4: World Expansion and Multiplayer

> From single district to living city to connected world. This is where CLOUT becomes massive.

---

## Overview

Phase 4 expands the game world beyond a single district and lays the multiplayer foundation. The player's empire grows from a neighborhood operation into a city-wide and eventually global enterprise. Multiplayer transforms the single-player simulation into a shared persistent world where player organizations compete and cooperate.

**Steps:** 16 through 19
**Estimated Duration:** 12-16 weeks
**Prerequisite:** Phase 3 fully integrated, all advanced empire systems operational

---

## Step 16: Multi-District City

### Design Intent

The city expands from one playable district to four or more distinct districts, each with unique character, demographics, market conditions, and law enforcement presence. Districts are connected by travel routes with border mechanics. The property market spans all districts, and district-level events create localized opportunities and threats.

### 16A: District Architecture

```
Scripts/World/Districts/
    DistrictManager.cs
    DistrictDefinition.cs      (ScriptableObject)
    DistrictGenerator.cs
    DistrictBorder.cs
    DistrictTravelSystem.cs
```

**District Profiles:**

| District | Character | Demographics | Base Demand | Police Presence | Property Cost |
|----------|-----------|-------------|-------------|-----------------|---------------|
| Downtown | Dense commercial, nightlife | Young professionals, tourists | High (party drugs) | High | Very High |
| Industrial | Warehouses, docks, factories | Blue collar, transient | Medium (stimulants) | Low | Low |
| Waterfront | Mixed residential/commercial | Affluent, mixed | High (premium product) | Medium | High |
| Suburbs | Residential sprawl, strip malls | Families, students | Low-Medium (prescriptions) | Very High | Medium |
| The Flats | Low-income residential, projects | Underserved community | High (volume, low price) | Medium-Low | Very Low |

### 16B: District Generation

Each district uses a procedural generation template extending the existing city block generator.

**Generation Parameters (per DistrictDefinition ScriptableObject):**

| Parameter | Type | Description |
|-----------|------|-------------|
| `gridSize` | `Vector2Int` | Block grid dimensions (e.g., 8x8 for Downtown, 12x6 for Industrial) |
| `buildingDensity` | `float` | 0.0 - 1.0, fill ratio |
| `heightRange` | `Vector2` | Min/max building heights |
| `roadWidth` | `float` | Street width (narrow downtown, wide suburbs) |
| `landmarkPrefabs` | `List<GameObject>` | District-specific landmark buildings |
| `ambientNPCDensity` | `float` | Background NPC population |
| `vegetationDensity` | `float` | Trees, parks, green space |
| `lightingProfile` | `LightingProfile` | District-specific lighting (neon downtown, dim industrial) |

### 16C: District Travel and Borders

**Travel System:**

| Travel Method | Speed | Cost | Risk | Availability |
|---------------|-------|------|------|-------------|
| Walking | Slow | Free | Low (police checks at borders) | Always |
| Personal Vehicle | Fast | Gas cost | Medium (vehicle tracked) | Own vehicle |
| Taxi / Rideshare | Medium | $20-50 | Low (no vehicle link) | Always |
| Fast Travel (unlockable) | Instant | $100 | None | Safe houses in both districts |

**Border Mechanics:**

- District borders are physical locations with optional police checkpoints
- Checkpoint probability based on district heat levels
- Carrying product through checkpoints: detection risk based on quantity and concealment
- Vehicle searches: slower but more thorough than pedestrian checks
- Border heat: frequent crossings with product increase checkpoint probability

### 16D: Cross-District Property Market

**PropertyMarketManager** extension:

| Feature | Description |
|---------|-------------|
| District pricing tiers | Base property cost varies by district |
| Cross-district ownership | Player can own properties in multiple districts |
| Commute mechanics | Workers assigned to distant properties have reduced efficiency |
| District reputation | Player has per-district reputation affecting deals and prices |
| Gentrification | Player investment in a district gradually increases property values |

### 16E: District Events

**DistrictEvent ScriptableObject:**

| Event | District | Duration | Effect |
|-------|----------|----------|--------|
| Block Party | The Flats | 2 days | 3x demand, reduced police |
| Restaurant Week | Downtown | 7 days | Increased front business revenue |
| Port Shutdown | Industrial | 14 days | No imports, commodity shortage |
| Neighborhood Watch | Suburbs | 10 days | 2x police patrols, customer reluctance |
| Art Festival | Waterfront | 5 days | Tourist influx, premium pricing |
| Rolling Blackout | Industrial | 1 day | Production halted, security systems offline |
| Gang Shootout | The Flats | 3 days | Police surge, rival faction weakened |
| City Council Vote | Downtown | 1 day | Policy change (legalization scare, new regulations) |

### 16F: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/World/Districts/DistrictManager.cs` | Singleton | 300-350 |
| `Scripts/World/Districts/DistrictDefinition.cs` | ScriptableObject | 80-100 |
| `Scripts/World/Districts/DistrictGenerator.cs` | MonoBehaviour | 400-500 |
| `Scripts/World/Districts/DistrictBorder.cs` | MonoBehaviour | 150-200 |
| `Scripts/World/Districts/DistrictTravelSystem.cs` | MonoBehaviour | 200-250 |
| `Scripts/World/Districts/DistrictEvent.cs` | ScriptableObject | 60-80 |
| `ScriptableObjects/Districts/` | 5+ district definitions | Data assets |

### 16G: Validation Criteria

- [ ] 4+ distinct districts generate with correct visual character
- [ ] Player can travel between districts using all travel methods
- [ ] Border checkpoints function with correct detection probability
- [ ] Property prices vary by district according to profiles
- [ ] Per-district market conditions function independently
- [ ] District events trigger and apply correct modifiers
- [ ] Rival factions operate across multiple districts
- [ ] District-specific police presence affects investigation intensity
- [ ] Performance: all districts loaded simultaneously maintain 60fps (LOD for distant districts)

---

## Step 17: Global Supply Chain

### Design Intent

Expand the economy beyond the local city to a global network of source regions, transit hubs, and trade routes. The player imports raw materials and precursor chemicals from international sources, navigates smuggling routes through transit hubs, and manages a supply chain that spans continents. This transforms production from a local crafting exercise into a logistics operation.

### 17A: Global Map Architecture

```
Scripts/World/Global/
    GlobalSupplyChain.cs
    SourceRegion.cs            (ScriptableObject)
    TransitHub.cs              (ScriptableObject)
    TradeRoute.cs
    SmugglingSystems.cs
    WarRoomUI.cs
```

**24 Source Regions (grouped by continent):**

| Region | Continent | Primary Export | Base Price | Reliability | Risk |
|--------|-----------|---------------|-----------|-------------|------|
| Sinaloa | North America | Methamphetamine precursors | Medium | High | Medium |
| Michoacan | North America | Avocado farms (front), fentanyl precursors | Low | Medium | High |
| Medellin | South America | Cocaine base | High | Medium | Medium |
| Cali | South America | Cocaine (refined) | Very High | Low | High |
| Lima | South America | Coca leaf | Low | High | Low |
| Golden Triangle (Myanmar) | Asia | Opium/heroin | Medium | Medium | Very High |
| Guangdong | Asia | Synthetic precursors | Low | Very High | Low |
| Mumbai | Asia | Pharmaceutical precursors | Medium | High | Low |
| Kabul | Asia | Raw opium | Very Low | Low | Very High |
| Rotterdam | Europe | Transit/redistribution | High markup | Very High | Very Low |
| Marseille | Europe | Hashish, MDMA precursors | Medium | Medium | Medium |
| Calabria | Europe | Transit, connections | High markup | High | Medium |

(Remaining 12 regions follow similar pattern across Africa, Oceania, Caribbean, Central America, Eastern Europe, and Central Asia.)

**12 Transit Hubs:**

| Hub | Location | Capacity | Speed | Interdiction Risk | Special |
|-----|----------|----------|-------|--------------------|---------|
| Juarez Border | US-Mexico | Very High | Fast | High | Land crossing, volume |
| Miami Port | Florida | High | Medium | Medium | Maritime, diverse routes |
| LAX Cargo | California | Medium | Very Fast | Medium | Air freight, expensive |
| Vancouver Port | Canada | High | Slow | Low | Pacific route, bulk |
| Panama Canal | Central America | Very High | Slow | Low | Cheapest maritime |
| Antwerp Port | Belgium | High | Slow | Low | European gateway |
| Istanbul | Turkey | Medium | Medium | Medium | East-West crossroads |
| Hong Kong | China | High | Medium | Low | Asian redistribution |
| Lagos | Nigeria | Medium | Slow | Very Low | West African route |
| Dubai Free Zone | UAE | High | Fast | Very Low | Financial hub, luxury |
| Vladivostok | Russia | Low | Slow | Very Low | Unmonitored, unreliable |
| Sydney Port | Australia | Low | Very Slow | High | Premium market access |

### 17B: Trade Route System

**TradeRoute (runtime class):**

| Field | Type | Description |
|-------|------|-------------|
| `sourceRegion` | `SourceRegion` | Origin |
| `transitHubs` | `List<TransitHub>` | Intermediate stops |
| `destination` | `DistrictId` | Player's receiving district |
| `commodity` | `CommodityType` | What is being transported |
| `quantity` | `float` | Amount per shipment |
| `totalCost` | `float` | Source cost + transit fees + bribes |
| `transitTime` | `int` | Total game days from order to delivery |
| `interdictionRisk` | `float` | Cumulative seizure probability |
| `reliability` | `float` | Probability shipment arrives intact |

**Route Cost Calculation:**

```
baseCost = sourceRegion.basePrice * quantity
transitFees = sum(hub.feePerUnit * quantity) for each hub
bribeCost = sum(hub.bribeCost) for each hub where bribe is active
insuranceCost = optional, covers lost shipments
totalCost = baseCost + transitFees + bribeCost + insuranceCost

transitTime = sum(hub.transitDays) + sourceRegion.preparationDays
interdictionRisk = 1.0 - product(1.0 - hub.interdictionRate) for each hub
```

### 17C: Smuggling Mini-Games

Event-driven mini-games triggered during high-risk transit points.

| Mini-Game | Trigger | Mechanic | Outcome |
|-----------|---------|----------|---------|
| Border Run | Land crossing hub | Timing-based: choose lane, manage papers | Pass or seized |
| Dock Shuffle | Maritime hub | Shell game: hide cargo among containers | Found or clear |
| Customs Bribe | Any hub | Negotiation: bid amount vs agent greed | Accepted or reported |
| Decoy Route | Air hub | Route planning: choose real vs decoy flights | Intercepted or delivered |
| Coast Guard Evasion | Maritime arrival | Navigation: avoid patrol patterns | Caught or landed |

Each mini-game is optional -- the player can auto-resolve (using the base interdiction risk) or play manually for better odds.

### 17D: War Room Visualization

```
Scripts/UI/
    WarRoomUI.cs
```

**War Room OnGUI Layout:**

```
+--------------------------------------------------+
| WAR ROOM - GLOBAL OPERATIONS                     |
+--------------------------------------------------+
|                                                    |
|  [WORLD MAP - Source regions and transit hubs]     |
|  Lines show active trade routes                    |
|  Color = status (green/yellow/red)                 |
|  Animated dots = shipments in transit              |
|                                                    |
+--------------------------------------------------+
| ACTIVE SHIPMENTS                                   |
| #001 Guangdong->Vancouver->City  ETA: 12 days [=] |
| #002 Medellin->Panama->Miami->City  ETA: 8 days ! |
| #003 Marseille->Rotterdam->City  ETA: 5 days OK   |
+--------------------------------------------------+
| SUPPLY CHAIN METRICS                               |
| Monthly Import Volume: 450 units                   |
| Average Transit Time: 9.2 days                     |
| Loss Rate (90 day): 8.3%                           |
| Total Logistics Cost: $127,000/month               |
+--------------------------------------------------+
```

### 17E: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/World/Global/GlobalSupplyChain.cs` | Singleton | 400-450 |
| `Scripts/World/Global/SourceRegion.cs` | ScriptableObject | 80-100 |
| `Scripts/World/Global/TransitHub.cs` | ScriptableObject | 80-100 |
| `Scripts/World/Global/TradeRoute.cs` | Class | 200-250 |
| `Scripts/World/Global/SmugglingSystems.cs` | MonoBehaviour | 300-350 |
| `Scripts/UI/WarRoomUI.cs` | OnGUI | 250-300 |
| `ScriptableObjects/Global/` | 36+ region/hub definitions | Data assets |

### 17F: Validation Criteria

- [ ] All 24 source regions defined with correct commodity types and pricing
- [ ] All 12 transit hubs functional with correct capacity and risk profiles
- [ ] Trade routes calculate cost, time, and risk correctly
- [ ] Shipments transit in real-time with correct arrival timing
- [ ] Interdiction events trigger at correct probability per hub
- [ ] Smuggling mini-games playable and affect shipment outcome
- [ ] War Room UI displays all active shipments and supply chain metrics
- [ ] CommodityTracker (Step 13) prices reflect global supply chain conditions
- [ ] Lost shipments correctly reduce inventory and trigger financial loss

---

## Step 18: Multiplayer Foundation

### Design Intent

Transform CLOUT from a single-player simulation into a shared persistent world. This step establishes the networking architecture, server-authoritative economy, player organization system, and territorial PvP. The goal is a stable foundation that supports 10-100 concurrent players per server, not an MMO -- a session-based persistent world where player actions have lasting consequences.

### 18A: Networking Architecture

```
Scripts/Networking/
    NetworkManager.cs          (extends or replaces existing FishNet setup)
    ServerAuthority.cs
    ClientPrediction.cs
    NetworkSyncManager.cs
    AntiCheatFoundation.cs
```

**Technology Decision:**

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| FishNet (existing) | Already integrated, familiar API | Community-maintained, fewer enterprise features | Re-enable if stable |
| Netcode for GameObjects | Unity official, active development | Migration cost, different API | Evaluate as alternative |

**Decision criteria:** Benchmark both against CLOUT's requirements (see below). Choose based on performance at target scale.

**Requirements:**

| Requirement | Target |
|-------------|--------|
| Concurrent players per server | 10-100 |
| Server tick rate | 20 Hz |
| Client prediction | Position, combat actions |
| Server authority | Economy, inventory, territory, investigations |
| State sync | Delta compression, priority-based |
| Bandwidth per player | < 50 KB/s |
| Reconnection | Seamless within 60 seconds |

### 18B: Server-Authoritative Economy

All economic operations validated and executed server-side.

**Server-Authoritative Systems:**

| System | Authority Level | Client Role |
|--------|----------------|-------------|
| CashManager | Full server | Display only |
| MarketSimulator | Full server | Price queries |
| PropertyManager | Full server | Purchase requests |
| TransactionLedger | Full server | Read only |
| LaunderingManager | Full server | Operation requests |
| DealManager | Full server | Deal initiation |
| InventoryManager | Full server | Use/transfer requests |
| CraftingStation | Full server | Craft requests |

**Client Prediction (latency hiding):**

| System | Prediction Type |
|--------|----------------|
| Player movement | Full client prediction, server reconciliation |
| Combat | Client-side hit detection, server validation |
| UI interactions | Optimistic updates, server confirmation |
| Economy | No prediction (server response required) |

### 18C: Player Organization System

```
Scripts/Multiplayer/
    SyndicateManager.cs
    SyndicateData.cs
    SyndicateRoles.cs
    SyndicateUI.cs
```

**Syndicate (player organization):**

| Feature | Description |
|---------|-------------|
| Creation | Any player can found a syndicate (costs in-game cash) |
| Membership | Invite-based, up to 20 players per syndicate |
| Roles | Boss, Underboss, Capo, Soldier, Associate |
| Shared resources | Syndicate treasury, shared properties, pooled territory |
| Hierarchy permissions | Role-based access to operations and information |

**Role Permissions:**

| Permission | Boss | Underboss | Capo | Soldier | Associate |
|------------|------|-----------|------|---------|-----------|
| Manage members | Yes | Yes | No | No | No |
| Access treasury | Full | Full | Withdraw limit | No | No |
| Assign territory | Yes | Yes | Yes | No | No |
| Start operations | Yes | Yes | Yes | Yes | No |
| View financials | Full | Full | District only | None | None |
| Declare war | Yes | Yes | No | No | No |
| Negotiate diplomacy | Yes | Yes | Delegate | No | No |

### 18D: Territory Wars (PvPvE)

```
Scripts/Multiplayer/
    TerritoryWarManager.cs
    WarDeclaration.cs
    TerritoryCapture.cs
    WarScoreTracker.cs
```

**Territory War Mechanics:**

| Phase | Duration | Mechanic |
|-------|----------|----------|
| Declaration | 24 hours (real time) | War declared, defenders notified, preparation period |
| Contestation | 48-72 hours | Territory blocks become capturable via control points |
| Resolution | Instant | Winning side claims contested territory |
| Cooldown | 7 days | No new wars between same factions |

**Capture Mechanics:**

```
Control Point: Physical location in territory block
    - Stand in zone to contest (capture timer)
    - Contested by opposing players = timer paused
    - NPCs defend (PvE element): hired guards, faction enforcers
    - Capture time: 5 minutes uncontested
    - Capture blocked if defending players outnumber attackers 2:1
```

**War Score:**

```
Territory blocks captured: +100 per block
Enemy players eliminated: +10 per elimination
Enemy property damaged: +25 per property
Defending own territory: +5 per minute held under attack
Total war score determines winner at resolution
```

### 18E: Anti-Cheat Foundation

| Layer | Implementation | Purpose |
|-------|---------------|---------|
| Server authority | All game state server-side | Eliminates client-side exploits |
| Input validation | Rate limiting, range checks | Prevents speed/teleport hacks |
| Economy validation | Transaction sanity checks | Prevents duplication, overflow |
| Movement validation | Server-side position verification | Detects teleportation |
| Statistical analysis | Anomaly detection on player metrics | Identifies subtle exploits |
| Replay system | Server-side action logging | Post-hoc investigation |

### 18F: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Networking/NetworkManager.cs` | Singleton | 400-500 |
| `Scripts/Networking/ServerAuthority.cs` | MonoBehaviour | 300-350 |
| `Scripts/Networking/ClientPrediction.cs` | MonoBehaviour | 250-300 |
| `Scripts/Networking/NetworkSyncManager.cs` | MonoBehaviour | 200-250 |
| `Scripts/Networking/AntiCheatFoundation.cs` | MonoBehaviour | 200-250 |
| `Scripts/Multiplayer/SyndicateManager.cs` | Singleton | 300-350 |
| `Scripts/Multiplayer/SyndicateData.cs` | Data class | 100-120 |
| `Scripts/Multiplayer/SyndicateRoles.cs` | Enum/Config | 60-80 |
| `Scripts/Multiplayer/TerritoryWarManager.cs` | Singleton | 350-400 |
| `Scripts/Multiplayer/WarScoreTracker.cs` | Class | 150-180 |
| `Scripts/UI/SyndicateUI.cs` | OnGUI | 200-250 |

### 18G: Validation Criteria

- [ ] Server starts and accepts client connections (10+ concurrent)
- [ ] All economic operations are server-authoritative with no client exploits
- [ ] Client prediction provides smooth movement at 100ms latency
- [ ] Player can create, join, and leave syndicates
- [ ] Role-based permissions enforced server-side
- [ ] Territory war lifecycle executes through all phases
- [ ] Capture points function with correct timing and contestation rules
- [ ] War score tracked accurately and determines winner
- [ ] Anti-cheat detects basic teleportation and speed hacks
- [ ] Reconnection restores full game state within 60 seconds

---

## Step 19: Network Graph System

### Design Intent

Model the social network between players as a first-class game mechanic. Trust, debt, and information flow through player-to-player connections. Compartmentalization becomes a multiplayer strategy -- what you know and who knows you matters. Informants can betray organizations by sharing network intelligence with law enforcement. Communication interception adds a surveillance dimension to multiplayer.

### 19A: Player Network Graph

```
Scripts/Multiplayer/Network/
    PlayerNetworkGraph.cs
    TrustSystem.cs
    DebtLedger.cs
    InformationFlow.cs
    CompartmentalizationManager.cs
```

**PlayerNetworkGraph:**

| Node Type | Description |
|-----------|-------------|
| Player | Human player with trust scores |
| NPC Contact | AI-controlled network contacts |
| Syndicate | Organization as aggregate node |
| Information | Discrete knowledge packets |

| Edge Type | Description | Weight Meaning |
|-----------|-------------|---------------|
| Trust | Bidirectional trust score | 0.0 = stranger, 1.0 = absolute trust |
| Debt | Directional financial obligation | Dollar amount owed |
| InfoFlow | Who knows what about whom | Information categories accessible |
| Authority | Hierarchical relationship | Syndicate role connection |

### 19B: Trust System

```
Trust accumulates through:
    - Completed deals: +0.05 per successful transaction
    - Shared operations: +0.02 per day working together
    - Debt repayment: +0.10 per debt cleared
    - Alliance defense: +0.15 per territory defended together

Trust decays through:
    - Inactivity: -0.01 per day without interaction
    - Failed deals: -0.10 per broken agreement
    - Debt default: -0.20 per unpaid debt
    - Betrayal: -1.0 (instant hostile)

Trust thresholds:
    < 0.2: Stranger (basic trades only)
    0.2 - 0.4: Acquaintance (can share territory)
    0.4 - 0.6: Associate (can share operations)
    0.6 - 0.8: Trusted (can share financial information)
    > 0.8: Inner Circle (full information access)
```

### 19C: Compartmentalization Mechanics

Information compartmentalization as a game mechanic -- limiting what each player/NPC knows reduces exposure if any node is compromised.

**Information Categories:**

| Category | Description | Exposure Risk if Leaked |
|----------|-------------|------------------------|
| Identity | Real syndicate membership | Moderate |
| Territory | Which blocks are controlled | Low |
| Production | Facility locations, recipes | High |
| Financial | Cash flows, laundering operations | Very High |
| Network | Full organizational chart | Critical (RICO) |
| Operations | Planned actions, schedules | Medium |

**Compartmentalization Rules:**

```
Each player has an information clearance level per category
Information flows DOWN hierarchy freely
Information flows UP only when explicitly shared
Lateral information flow requires trust > 0.6
Compromised node reveals only information they had access to
```

**Cutout System:**

- Players can designate intermediaries (cutouts) for deals
- Cutout knows buyer and seller but not their organizations
- If cutout is compromised, investigation reaches dead end at cutout
- Cutouts reduce efficiency (transaction fees) but increase security

### 19D: Informant System

```
Scripts/Multiplayer/Network/
    InformantSystem.cs
    InformantHandler.cs
```

**Player Informant Mechanics:**

| Feature | Description |
|---------|-------------|
| Recruitment | Police AI approaches low-trust or arrested players with deal |
| Incentives | Reduced sentences, cash payments, immunity |
| Information delivery | Informant reveals graph edges to InvestigationGraph |
| Exposure risk | Other players can detect informant behavior patterns |
| Counter-intelligence | Syndicate can feed false information through suspected informants |

**NPC Informant Mechanics:**

| Feature | Description |
|---------|-------------|
| Worker flipping | Police can turn arrested workers into informants |
| Loyalty threshold | Workers with loyalty < 0.4 susceptible to flipping |
| Detection | Security workers have chance to identify NPC informants |
| False flag | Player can deliberately hire suspicious workers as counter-intel |

### 19E: Communication Interception

```
Scripts/Multiplayer/Network/
    CommunicationSystem.cs
    WiretapSystem.cs
```

**Communication Channels:**

| Channel | Speed | Security | Interception Risk |
|---------|-------|----------|-------------------|
| Direct (in-person) | Instant | Maximum | Zero (unless surveilled) |
| Phone (in-game) | Instant | Low | High (wiretap possible) |
| Burner Phone | Instant | Medium | Low (single use) |
| Dead Drop | Delayed (1 day) | High | Medium (surveillance) |
| Encrypted Message | Instant | Very High | Very Low |

**Wiretap Mechanics:**

- Police can obtain wiretap warrant (requires investigation graph evidence)
- Wiretap intercepts phone channel communications for 14 game days
- Intercepted messages become edges in InvestigationGraph
- Players can detect wiretaps with counter-surveillance equipment
- Burner phones bypass wiretaps (but cost money and are one-use)

### 19F: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Multiplayer/Network/PlayerNetworkGraph.cs` | Data structure | 300-350 |
| `Scripts/Multiplayer/Network/TrustSystem.cs` | MonoBehaviour | 200-250 |
| `Scripts/Multiplayer/Network/DebtLedger.cs` | Class | 150-180 |
| `Scripts/Multiplayer/Network/InformationFlow.cs` | Class | 200-250 |
| `Scripts/Multiplayer/Network/CompartmentalizationManager.cs` | Singleton | 250-300 |
| `Scripts/Multiplayer/Network/InformantSystem.cs` | MonoBehaviour | 200-250 |
| `Scripts/Multiplayer/Network/CommunicationSystem.cs` | MonoBehaviour | 200-250 |
| `Scripts/Multiplayer/Network/WiretapSystem.cs` | MonoBehaviour | 150-200 |

### 19G: Validation Criteria

- [ ] Player network graph tracks all node and edge types correctly
- [ ] Trust accumulates and decays according to defined rules
- [ ] Trust thresholds gate information access correctly
- [ ] Debt ledger tracks obligations between players
- [ ] Compartmentalization limits information exposure per player role
- [ ] Cutout system creates dead ends in investigation graph
- [ ] Informant recruitment functions for both player and NPC targets
- [ ] Communication channels have correct security and interception profiles
- [ ] Wiretaps intercept phone communications and feed investigation graph
- [ ] Counter-surveillance equipment detects active wiretaps

---

## Phase 4 Aggregate Metrics

### Script Count

| Step | New Scripts | Notes |
|------|------------|-------|
| Step 16: Multi-District | 7 | + district data assets |
| Step 17: Global Supply Chain | 6 | + 36 region/hub data assets |
| Step 18: Multiplayer Foundation | 11 | Core networking + syndicates |
| Step 19: Network Graph | 8 | Social mechanics |
| **Total** | **32** | |

**Running total: ~193 scripts (161 Phase 1-3 + 32 Phase 4)**

### Dependency Graph

```
Step 16 (Districts) ------> Step 17 (Supply Chain) via import destinations
Step 16 (Districts) ------> Step 18 (Multiplayer) via territory scope
Step 18 (Multiplayer) -----> Step 19 (Network Graph) via player connections
Step 17 (Supply Chain) ----> Step 18 (Multiplayer) via server-authoritative economy
```

**Recommended build order:** 16 -> 17 -> 18 -> 19

Step 16 can begin immediately after Phase 3. Step 17 requires districts as import destinations. Step 18 requires both 16 and 17 for server-authoritative world state. Step 19 requires multiplayer infrastructure from Step 18.

### Phase 4 Exit Criteria

1. All 32 new scripts compile without errors
2. 4+ distinct districts playable with travel and border mechanics
3. Global supply chain operational with 24 source regions and 12 transit hubs
4. Multiplayer server supports 10+ concurrent players
5. Syndicate system functional with role-based permissions
6. Territory wars execute through full lifecycle
7. Network graph tracks trust, debt, and information flow
8. Informant and communication systems integrated with investigation graph
9. No regressions in Phase 1-3 single-player functionality
10. Performance: 60fps with all systems active, <50 KB/s per player network bandwidth
