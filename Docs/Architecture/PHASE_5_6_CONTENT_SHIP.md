# CLOUT -- Phase 5: Content and Polish / Phase 6: Ship

> From functional to finished. From finished to live.

---

## Overview

Phase 5 fills the world with content, polishes every surface, and prepares the game for players. Phase 6 validates at scale, builds live operations infrastructure, and executes a two-stage launch. These phases assume all core systems from Phases 1-4 are stable and integrated.

**Steps:** 20 through 28
**Estimated Duration:** Phase 5: 10-14 weeks / Phase 6: 8-12 weeks
**Prerequisite:** Phase 4 multiplayer foundation stable, all game systems operational

---

# PHASE 5: CONTENT AND POLISH (Steps 20-24)

---

## Step 20: Procedural Music System

### Design Intent

Generate adaptive music that responds to game state -- tension during police encounters, ambient during exploration, aggressive during combat, melancholic during downtime. The system layers procedural audio stems rather than relying solely on pre-composed tracks.

### 20A: Architecture

```
Scripts/Audio/
    ProceduralMusicManager.cs
    MusicLayerController.cs
    MusicStateMapper.cs
    StemDefinition.cs            (ScriptableObject)
    MusicMoodProfile.cs          (ScriptableObject)
```

**Layer System:**

| Layer | Description | Trigger |
|-------|-------------|---------|
| Base | Ambient pad, sets key and tempo | Always active |
| Rhythm | Percussion pattern | Movement, activity level |
| Melody | Lead instrument phrase | Location, time of day |
| Tension | Dissonant overlay, rising pitch | Heat level, police proximity |
| Action | Aggressive drums, bass hits | Combat, chase |
| Stinger | One-shot accent | Kill, deal, arrest, level up |

**State Mapping:**

| Game State | Mood | Tempo (BPM) | Key | Active Layers |
|------------|------|-------------|-----|---------------|
| Exploration | Chill | 80-100 | Minor | Base, Rhythm |
| Dealing | Tense-Chill | 90-110 | Minor | Base, Rhythm, Melody |
| Production | Industrial | 100-120 | Diminished | Base, Rhythm |
| Combat | Aggressive | 130-160 | Phrygian | Base, Rhythm, Action |
| Police Chase | High Tension | 140-170 | Chromatic | Base, Tension, Action |
| Empire Management | Ambient | 70-90 | Major | Base, Melody |
| Nightclub Interior | Club | 125-130 | Minor | Custom club stems |

**Crossfade Behavior:**

- Layer transitions use 2-4 beat crossfades quantized to bar boundaries
- Tempo changes interpolate over 8 bars
- Key changes snap at phrase boundaries (4 or 8 bars)
- Stingers play immediately, unquantized

### 20B: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Audio/ProceduralMusicManager.cs` | Singleton | 300-350 |
| `Scripts/Audio/MusicLayerController.cs` | MonoBehaviour | 200-250 |
| `Scripts/Audio/MusicStateMapper.cs` | Class | 150-180 |
| `Scripts/Audio/StemDefinition.cs` | ScriptableObject | 60-80 |
| `Scripts/Audio/MusicMoodProfile.cs` | ScriptableObject | 60-80 |

### 20C: Validation Criteria

- [ ] Music responds to game state changes within 2-4 beats
- [ ] Layer crossfades are quantized and musically coherent
- [ ] Tempo and key transitions are smooth
- [ ] Combat, chase, and ambient states are distinctly audible
- [ ] Stingers play on correct game events
- [ ] Audio CPU usage < 5% on target hardware

---

## Step 21: Advanced Procedural Generation

### Design Intent

Extend the procedural city generator into a full San Francisco-inspired urban template with detailed interiors, NPC schedules, and environmental storytelling. Every building the player enters should feel inhabited and purposeful.

### 21A: City Template

```
Scripts/World/Procedural/
    CityTemplateGenerator.cs
    InteriorGenerator.cs
    NPCScheduleSystem.cs
    EnvironmentalStorytelling.cs
```

**San Francisco Terrain Features:**

| Feature | Implementation |
|---------|---------------|
| Hills | Perlin noise height map with SF-inspired topology |
| Grid streets | Rectilinear grid following terrain contours |
| Cable car routes | Fixed transit lines on steep grades |
| Waterfront | Flat coastal zone with pier structures |
| Parks | Green space nodes with open areas |
| Alleys | Narrow back passages between blocks |

**Building Type Distribution (per district):**

| Type | Downtown | Industrial | Waterfront | Suburbs | The Flats |
|------|----------|-----------|------------|---------|-----------|
| Residential (low) | 10% | 15% | 20% | 60% | 50% |
| Residential (high) | 30% | 0% | 15% | 5% | 10% |
| Commercial | 35% | 10% | 30% | 20% | 15% |
| Industrial | 5% | 50% | 15% | 5% | 10% |
| Civic | 10% | 5% | 10% | 5% | 5% |
| Entertainment | 10% | 5% | 10% | 5% | 10% |

### 21B: Interior Generation

**20+ Interior Variants:**

| Category | Variants | Key Features |
|----------|----------|-------------|
| Residential Apartment | Small, Medium, Large, Penthouse | Furniture, kitchen, bathroom, personal items |
| Commercial Retail | Bodega, Boutique, Electronics, Pawn Shop | Display shelves, counter, back room, register |
| Restaurant | Fast Food, Diner, Fine Dining, Bar | Kitchen, seating, storage, office |
| Industrial | Warehouse, Factory, Workshop, Lab | Open floor, equipment, loading dock, office |
| Civic | Police Station, Courthouse, City Hall | Lobby, offices, holding cells, records |

**Interior Generation Pipeline:**

```
1. Determine building footprint and floor count
2. Select interior template based on building type
3. Generate room layout using BSP (Binary Space Partition)
4. Place furniture and props from category-appropriate pools
5. Set lighting based on time of day and occupancy
6. Spawn NPC occupants based on schedule system
```

### 21C: NPC Schedules

**NPCScheduleSystem:**

| Time Block | Duration | NPC Behavior |
|------------|----------|-------------|
| 00:00-06:00 | Night | Home (sleep), nightclub patrons, night shift workers |
| 06:00-09:00 | Morning | Commute, coffee shops, opening businesses |
| 09:00-12:00 | Morning Work | Office workers, retail open, school |
| 12:00-14:00 | Lunch | Restaurants busy, park visitors, lunch deals |
| 14:00-17:00 | Afternoon | Work continues, school lets out, errands |
| 17:00-20:00 | Evening | Commute home, dinner, evening activities |
| 20:00-00:00 | Night Life | Bars, clubs, late dining, street activity |

Each NPC has a schedule template with location waypoints and activity states. Schedules vary by NPC type (worker, student, professional, dealer, customer, officer).

### 21D: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/World/Procedural/CityTemplateGenerator.cs` | MonoBehaviour | 500-600 |
| `Scripts/World/Procedural/InteriorGenerator.cs` | MonoBehaviour | 400-500 |
| `Scripts/World/Procedural/NPCScheduleSystem.cs` | Singleton | 300-350 |
| `Scripts/World/Procedural/EnvironmentalStorytelling.cs` | MonoBehaviour | 200-250 |

### 21E: Validation Criteria

- [ ] City terrain follows SF-inspired hill topology
- [ ] Building distribution matches per-district profiles
- [ ] 20+ interior variants generate correctly with appropriate props
- [ ] NPC schedules produce realistic population flow across time of day
- [ ] Interiors load on demand without frame drops (< 16ms load time)
- [ ] Environmental details reflect building owner and neighborhood character

---

## Step 22: UI/UX Polish

### Design Intent

Migrate the functional OnGUI prototype interfaces to UI Toolkit for production quality. Add the War Room strategic overview and network graph visualization. Every interface should be readable, responsive, and consistent.

### 22A: UI Toolkit Migration

**Migration Priority (by player interaction frequency):**

| Priority | UI Element | Current | Target |
|----------|-----------|---------|--------|
| P0 | HUD (health, cash, heat, minimap) | OnGUI | UI Toolkit + USS |
| P0 | Inventory | OnGUI | UI Toolkit + USS |
| P0 | Deal negotiation | OnGUI | UI Toolkit + USS |
| P1 | Property management | OnGUI | UI Toolkit + USS |
| P1 | Worker management | OnGUI | UI Toolkit + USS |
| P1 | Crafting station | OnGUI | UI Toolkit + USS |
| P1 | Laundering dashboard | OnGUI | UI Toolkit + USS |
| P2 | Diplomacy | OnGUI | UI Toolkit + USS |
| P2 | Market data | OnGUI | UI Toolkit + USS |
| P2 | War Room | New | UI Toolkit + USS |
| P2 | Network graph viewer | New | UI Toolkit + USS |

**Design System:**

| Token | Value | Usage |
|-------|-------|-------|
| `--color-bg-primary` | #0A0A0A | Main background |
| `--color-bg-secondary` | #1A1A1A | Panel backgrounds |
| `--color-bg-tertiary` | #2A2A2A | Card backgrounds |
| `--color-accent` | #FF6B00 | Primary accent (CLOUT orange) |
| `--color-danger` | #FF2D2D | Warnings, heat, wanted |
| `--color-success` | #00CC66 | Clean cash, positive outcomes |
| `--color-text-primary` | #FFFFFF | Primary text |
| `--color-text-secondary` | #999999 | Secondary text |
| `--font-heading` | Bold, 18-24px | Section headers |
| `--font-body` | Regular, 14px | Body text |
| `--font-mono` | Monospace, 12px | Numbers, data |
| `--border-radius` | 4px | Standard corner radius |
| `--spacing-unit` | 8px | Base spacing grid |

### 22B: War Room

Full-screen strategic overview combining multiple data sources.

**War Room Panels:**

| Panel | Content | Data Source |
|-------|---------|-------------|
| City Map | District overview with territory coloring | TerritoryManager, FactionManager |
| Global Map | Supply chain visualization | GlobalSupplyChain |
| Finance | Cash flow, laundering status, projections | CashManager, LaunderingManager |
| Intel | Investigation status, threat assessment | InvestigationGraph, WantedSystem |
| Personnel | Worker roster, assignments, loyalty | WorkerManager |
| Market | Price charts, commodity trends, events | MarketSimulator, CommodityTracker |

### 22C: Network Graph Viewer

Interactive visualization of the player's social/organizational network.

**Features:**

| Feature | Description |
|---------|-------------|
| Force-directed layout | Nodes repel, edges attract, auto-arranges |
| Node coloring | By type (player, NPC, location, asset) |
| Edge styling | By type (trust = green, debt = red, info = blue) |
| Zoom and pan | Mouse wheel zoom, click-drag pan |
| Node selection | Click node for detail panel |
| Compartment view | Toggle to show information boundaries |
| Investigation overlay | Show what police know (if intel available) |

### 22D: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/UI/Toolkit/HUDController.cs` | UI Toolkit | 300-350 |
| `Scripts/UI/Toolkit/InventoryUI.cs` | UI Toolkit | 250-300 |
| `Scripts/UI/Toolkit/DealUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/PropertyUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/WorkerUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/CraftingUI.cs` | UI Toolkit | 150-200 |
| `Scripts/UI/Toolkit/LaunderingUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/DiplomacyUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/MarketUI.cs` | UI Toolkit | 200-250 |
| `Scripts/UI/Toolkit/WarRoomController.cs` | UI Toolkit | 400-500 |
| `Scripts/UI/Toolkit/NetworkGraphViewer.cs` | UI Toolkit | 350-400 |
| `Assets/UI/Styles/` | USS stylesheets | Multiple files |

### 22E: Validation Criteria

- [ ] All P0 UI elements migrated to UI Toolkit
- [ ] Design system tokens applied consistently across all panels
- [ ] War Room displays all 6 panels with live data
- [ ] Network graph viewer renders 100+ nodes at 60fps
- [ ] All UI elements responsive at 1080p, 1440p, and 4K
- [ ] Keyboard navigation functional for all interactive elements
- [ ] UI load time < 100ms for any panel

---

## Step 23: Accessibility

### Design Intent

Make CLOUT playable by the widest possible audience. Accessibility is not optional -- it is a design requirement. This step adds color-blind support, screen reader compatibility, input remapping, and one-button macro systems.

### 23A: Visual Accessibility

```
Scripts/Accessibility/
    AccessibilityManager.cs
    ColorBlindFilter.cs
    HighContrastMode.cs
    TextScaling.cs
```

**Color-Blind Modes:**

| Mode | Affected Colors | Adaptation |
|------|----------------|------------|
| Protanopia (red-blind) | Red/green distinction | Shift red indicators to blue/orange |
| Deuteranopia (green-blind) | Red/green distinction | Shift green indicators to blue/yellow |
| Tritanopia (blue-blind) | Blue/yellow distinction | Shift blue indicators to red/cyan |

**Implementation:** Post-processing shader that remaps game colors based on selected mode. All gameplay-critical information has redundant non-color indicators (icons, patterns, text labels).

### 23B: Audio Accessibility

| Feature | Description |
|---------|-------------|
| Screen reader support | All UI elements have accessible names and descriptions |
| Audio descriptions | Optional narration of visual game events |
| Subtitle system | Dialogue, ambient speech, and environmental audio captioned |
| Directional audio indicators | Visual compass showing audio source direction |
| Volume per-channel | Master, Music, SFX, Voice, UI independently adjustable |

### 23C: Input Accessibility

| Feature | Description |
|---------|-------------|
| Full key remapping | Every action rebindable to any key/button |
| One-button macros | Complex sequences (deal, craft, launder) as single input |
| Hold vs toggle | All hold inputs have toggle alternative |
| Input sensitivity | Adjustable dead zones, acceleration curves |
| Controller support | Full gamepad support with UI navigation |
| Mouse-only mode | All actions performable without keyboard |

### 23D: One-Button Macro System

```
Scripts/Accessibility/
    MacroSystem.cs
    MacroDefinition.cs          (ScriptableObject)
```

**Pre-Built Macros:**

| Macro | Sequence | Description |
|-------|----------|-------------|
| Quick Deal | Approach + Negotiate + Best Offer + Confirm | One-button street deal |
| Quick Craft | Select Recipe + Start + Wait + Collect | One-button production |
| Quick Launder | Select Business + Best Method + Max Amount + Start | One-button laundering |
| Emergency Flee | Sprint + Drop Contraband + Change Clothes | One-button heat escape |

### 23E: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Accessibility/AccessibilityManager.cs` | Singleton | 250-300 |
| `Scripts/Accessibility/ColorBlindFilter.cs` | MonoBehaviour | 150-200 |
| `Scripts/Accessibility/HighContrastMode.cs` | MonoBehaviour | 100-130 |
| `Scripts/Accessibility/TextScaling.cs` | MonoBehaviour | 80-100 |
| `Scripts/Accessibility/ScreenReaderBridge.cs` | MonoBehaviour | 200-250 |
| `Scripts/Accessibility/MacroSystem.cs` | Singleton | 200-250 |
| `Scripts/Accessibility/MacroDefinition.cs` | ScriptableObject | 60-80 |

### 23F: Validation Criteria

- [ ] All three color-blind modes render correctly with no information loss
- [ ] Screen reader can navigate all UI elements with descriptive labels
- [ ] Subtitles display for all dialogue and significant audio events
- [ ] Every input action can be rebound
- [ ] One-button macros execute full sequences correctly
- [ ] Hold/toggle preference applies to all hold-based inputs
- [ ] Game fully playable with gamepad only
- [ ] Game fully playable with mouse only
- [ ] Text scaling from 80% to 200% without layout breakage

---

## Step 24: Content Pipeline

### Design Intent

Fill the world with content at production scale. This step defines the volume targets and data assets required for a content-complete game. All content uses the ScriptableObject and procedural systems built in prior phases.

### 24A: Content Volume Targets

| Category | Target Count | Source |
|----------|-------------|--------|
| Source Regions | 24 | ScriptableObject definitions |
| Transit Hubs | 12 | ScriptableObject definitions |
| NPC Templates | 50+ | ScriptableObject with visual/behavior presets |
| Interior Variants | 20+ | Procedural templates with prop pools |
| Recipes | 30+ | ScriptableObject crafting definitions |
| Weapons | 15+ | ScriptableObject weapon stats |
| Vehicles | 10+ | Prefabs with driving physics |
| District Themes | 5+ | DistrictDefinition ScriptableObjects |
| Market Events | 20+ | MarketEvent ScriptableObjects |
| Laundering Methods | 6+ | LaunderingMethod ScriptableObjects |
| Faction Profiles | 8+ | FactionProfile ScriptableObjects |
| Music Stems | 100+ | Audio clips per mood/layer |
| Ambient Sound Sets | 10+ | Per-district ambient audio |

### 24B: NPC Template System

**50+ NPC Templates across categories:**

| Category | Count | Examples |
|----------|-------|---------|
| Civilian | 15 | Office worker, student, tourist, homeless, jogger |
| Criminal | 10 | Street dealer, enforcer, smuggler, fence, cook |
| Law Enforcement | 8 | Beat cop, detective, SWAT, undercover, federal agent |
| Service | 8 | Bartender, shopkeeper, taxi driver, doctor, lawyer |
| Special | 9 | Informant, journalist, politician, rival boss, contact |

Each template defines: visual appearance range, behavior profile, schedule template, dialogue set, stat ranges, and spawn conditions.

### 24C: Recipe Expansion

**30+ Recipes across product categories:**

| Category | Recipe Count | Complexity Range |
|----------|-------------|------------------|
| Stimulants | 8 | 2-6 ingredients, 3-8 steps |
| Depressants | 6 | 2-5 ingredients, 2-6 steps |
| Hallucinogens | 5 | 3-7 ingredients, 4-9 steps |
| Synthetics | 6 | 4-8 ingredients, 5-10 steps |
| Pharmaceuticals | 5 | 2-4 ingredients, 2-5 steps |

Each recipe defines: ingredients, equipment requirements, skill thresholds, yield range, quality modifiers, signature characteristics, and market value curve.

### 24D: Vehicle System

**10+ Vehicle Types:**

| Vehicle | Speed | Capacity | Stealth | Cost | Special |
|---------|-------|----------|---------|------|---------|
| Sedan | Medium | 2 passengers + trunk | High | $15K | Common, blends in |
| SUV | Medium | 4 passengers + cargo | Medium | $30K | Off-road capable |
| Sports Car | Fast | 1 passenger | Low | $60K | Chase escape |
| Van | Slow | Large cargo | Medium | $20K | Bulk transport |
| Box Truck | Very Slow | Very large cargo | Low | $35K | Maximum hauling |
| Motorcycle | Very Fast | Minimal | High | $10K | Lane splitting, fast escape |
| Boat | Medium | Large cargo | High | $50K | Waterfront district access |
| Bicycle | Slow | Minimal | Very High | $500 | Silent, no license plate |
| Taxi (disguise) | Medium | Standard | Very High | Hire cost | Invisible transport |
| Armored Vehicle | Slow | Medium cargo | Very Low | $100K | Combat resistant |

### 24E: Validation Criteria

- [ ] All 24 source regions and 12 transit hubs have complete ScriptableObject data
- [ ] 50+ NPC templates produce visually distinct, behaviorally varied characters
- [ ] 20+ interior variants generate without visual artifacts or prop overlap
- [ ] 30+ recipes are craftable with correct ingredient and equipment requirements
- [ ] 10+ vehicles are drivable with distinct handling characteristics
- [ ] All content loads without exceeding memory budget (< 4GB total)
- [ ] Content variety prevents noticeable repetition within 10 hours of gameplay

---

# PHASE 6: SHIP (Steps 25-28)

---

## Step 25: Scale Testing

### Design Intent

Validate that all game systems function correctly and performantly at production scale. This is the stress-testing phase -- every system gets pushed to its limits to identify bottlenecks, stability issues, and balance problems.

### 25A: Concurrency Testing

| Test | Target | Metric |
|------|--------|--------|
| Concurrent players | 10,000 (total across servers) | Server CPU < 80% |
| Players per server | 100 | Server tick rate stable at 20 Hz |
| Simultaneous deals | 50 per server | Transaction processing < 50ms |
| Active territory wars | 5 per server | War score updates < 100ms |
| Investigation graphs | 500 nodes per graph | Query response < 1ms |
| Market simulation | 5 districts x 10 products | Price update < 10ms |

### 25B: Economy Stability Testing

| Test | Duration | Success Criteria |
|------|----------|-----------------|
| Inflation test | 90 simulated game days | Money supply growth < 5% per month |
| Deflation test | 90 simulated game days | No deflationary spiral |
| Market manipulation | Single player corners market | Economy recovers within 14 game days |
| Faction economic war | 3 factions + player compete | No faction accumulates > 50% of economy |
| Laundering throughput | Max capacity for 30 days | IRS system triggers at correct thresholds |

### 25C: Investigation AI Performance

| Test | Scenario | Success Criteria |
|------|----------|-----------------|
| Graph scaling | 1000 nodes, 5000 edges | All operations < 10ms |
| PageRank convergence | 500-node graph | Converges in < 100ms |
| Case throughput | 10 simultaneous cases | Detective AI decisions < 50ms/day |
| RICO detection | Large connected component | Score calculation < 5ms |
| Undercover lifecycle | 90-day infiltration | No memory leaks, correct state transitions |

### 25D: Client Performance

| Test | Target | Metric |
|------|--------|--------|
| Frame rate (min spec) | GTX 1060 / equivalent | Stable 30fps |
| Frame rate (recommended) | RTX 3060 / equivalent | Stable 60fps |
| Memory usage | All districts loaded | < 4GB VRAM, < 8GB RAM |
| Load time (initial) | Cold start to gameplay | < 30 seconds |
| Load time (district travel) | District transition | < 3 seconds |
| Network bandwidth | Active gameplay | < 50 KB/s upstream |

### 25E: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Tests/Scale/ConcurrencyTests.cs` | Test suite | 300-400 |
| `Tests/Scale/EconomyStabilityTests.cs` | Test suite | 250-300 |
| `Tests/Scale/InvestigationPerfTests.cs` | Test suite | 200-250 |
| `Tests/Scale/ClientPerfBenchmarks.cs` | Benchmark suite | 200-250 |

### 25F: Validation Criteria

- [ ] All concurrency targets met under sustained load
- [ ] Economy remains stable over 90-day simulation
- [ ] Investigation AI performs within time budgets at scale
- [ ] Client maintains target frame rates on min/recommended hardware
- [ ] No memory leaks over 4-hour play sessions
- [ ] Network bandwidth stays within budget

---

## Step 26: Live Operations Infrastructure

### Design Intent

Build the systems required to operate CLOUT as a live service: seasonal events, modding support, community tools, and telemetry. These systems enable the game to evolve post-launch without client patches for content updates.

### 26A: Seasonal Event System

```
Scripts/LiveOps/
    SeasonalEventManager.cs
    SeasonalEventDefinition.cs  (ScriptableObject)
    EventRewardTracker.cs
```

**Seasonal Event Structure:**

| Component | Description |
|-----------|-------------|
| Theme | Visual and narrative theme (e.g., summer heat wave, holiday season) |
| Duration | 2-4 weeks real time |
| Modifiers | Economy, police, weather, NPC behavior changes |
| Challenges | Timed objectives with rewards |
| Rewards | Exclusive items, cosmetics, titles |
| Leaderboard | Per-server rankings for competitive objectives |

### 26B: Modding API

```
Scripts/Modding/
    ModLoader.cs
    ModManifest.cs
    ModSandbox.cs
    ModAPI.cs
```

**Modding Support Tiers:**

| Tier | Capability | Sandboxing |
|------|-----------|------------|
| Data Mods | ScriptableObject overrides (recipes, NPCs, events) | Full sandbox, no code execution |
| Asset Mods | Custom models, textures, audio | Asset validation, size limits |
| Script Mods | Custom MonoBehaviours via assembly loading | Restricted API surface, no file system access |
| Total Conversion | Full game modification | Separate mod profile, no online play |

### 26C: Community Tools

| Tool | Description |
|------|-------------|
| Server browser | List, filter, and join game servers |
| Syndicate finder | Browse and apply to player organizations |
| Market dashboard (web) | Out-of-game price tracking and alerts |
| Replay viewer | Watch recorded gameplay sessions |
| Wiki integration | In-game links to community wiki |

### 26D: Telemetry System

```
Scripts/LiveOps/
    TelemetryManager.cs
    TelemetryEvent.cs
    PrivacyManager.cs
```

**Telemetry Events (opt-in, anonymized):**

| Category | Events | Purpose |
|----------|--------|---------|
| Economy | Transaction volumes, price distributions | Balance tuning |
| Combat | Weapon usage, kill/death ratios | Weapon balance |
| Progression | Time to milestones, drop-off points | Pacing adjustment |
| Systems | Feature usage frequency | Feature prioritization |
| Performance | Frame rate distribution, crash reports | Optimization targeting |
| Multiplayer | Session lengths, player counts | Server capacity planning |

**Privacy Requirements:**

- All telemetry opt-in with clear disclosure
- No personally identifiable information collected
- Data anonymized before transmission
- Player can view and delete their telemetry data
- Compliance with GDPR, CCPA, and applicable regulations

### 26E: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/LiveOps/SeasonalEventManager.cs` | Singleton | 250-300 |
| `Scripts/LiveOps/SeasonalEventDefinition.cs` | ScriptableObject | 80-100 |
| `Scripts/LiveOps/EventRewardTracker.cs` | MonoBehaviour | 150-180 |
| `Scripts/Modding/ModLoader.cs` | Singleton | 300-350 |
| `Scripts/Modding/ModManifest.cs` | Data class | 80-100 |
| `Scripts/Modding/ModSandbox.cs` | Class | 200-250 |
| `Scripts/Modding/ModAPI.cs` | Static API | 300-350 |
| `Scripts/LiveOps/TelemetryManager.cs` | Singleton | 200-250 |
| `Scripts/LiveOps/TelemetryEvent.cs` | Data class | 60-80 |
| `Scripts/LiveOps/PrivacyManager.cs` | Singleton | 150-200 |

### 26F: Validation Criteria

- [ ] Seasonal events deploy without client update
- [ ] Event modifiers apply correctly to all game systems
- [ ] Data mods load and override ScriptableObjects without errors
- [ ] Asset mods pass validation and render correctly
- [ ] Script mods run within sandbox restrictions
- [ ] Telemetry events fire correctly and respect opt-in preference
- [ ] Privacy manager allows data viewing and deletion
- [ ] Server browser lists active servers with correct player counts

---

## Step 27: Early Access Launch

### Target: Q3 2027

### 27A: Early Access Scope

| Feature | Status Required |
|---------|----------------|
| Single-player core loop | Complete, polished |
| 3+ districts | Playable, content-filled |
| Money laundering | Full pipeline operational |
| Forensics system | Functional, tuned |
| Market simulation | Dynamic, balanced |
| Rival factions (3+) | AI operational |
| Investigation system | Full lifecycle |
| Basic multiplayer (2-10 players) | Stable |
| UI Toolkit migration (P0 elements) | Complete |
| Procedural city | 1 full city template |

### 27B: Early Access Exclusions

| Feature | Reason |
|---------|--------|
| Global supply chain | Deferred to full launch |
| Large-scale multiplayer (100 players) | Needs EA validation |
| Modding API | Needs stable base |
| Seasonal events | Post-EA content cadence |
| Full accessibility suite | Progressive rollout |

### 27C: Launch Checklist

| Category | Requirement |
|----------|-------------|
| Build | Release build compiles, no errors, no warnings |
| Performance | 60fps on recommended spec, 30fps on minimum |
| Stability | No crash in 4-hour play session |
| Save system | Save/load preserves full game state |
| Tutorial | New player can complete core loop within 30 minutes |
| Storefront | Steam page live, screenshots, trailer, description |
| Community | Discord server, feedback channels, bug reporting |
| Legal | EULA, privacy policy, content ratings |
| Support | Known issues list, FAQ, contact method |
| Analytics | Telemetry operational, dashboard configured |

### 27D: Early Access Update Cadence

| Frequency | Content |
|-----------|---------|
| Weekly | Bug fixes, hotfixes |
| Bi-weekly | Balance patches, tuning |
| Monthly | Feature updates, new content |
| Quarterly | Major system additions |

---

## Step 28: Full Launch

### Target: Q4 2027

### 28A: Full Launch Additions (beyond Early Access)

| Feature | Description |
|---------|-------------|
| Global supply chain | All 24 regions, 12 hubs, smuggling mini-games |
| Large-scale multiplayer | 100 players per server, territory wars |
| Modding API | Full modding support with workshop integration |
| Seasonal events | First season event coincides with launch |
| Complete accessibility | All accessibility features operational |
| Full content pipeline | All 50+ NPC templates, 30+ recipes, 10+ vehicles |
| Network graph system | Trust, informants, communication interception |

### 28B: Launch Quality Gates

| Gate | Criteria |
|------|----------|
| Stability | < 0.1% crash rate over 10,000 play sessions |
| Performance | Meets all Step 25 benchmarks |
| Balance | Economy stable over 90-day simulation, no dominant strategy |
| Content | > 20 hours of non-repetitive gameplay |
| Multiplayer | Server stability over 72-hour stress test |
| Accessibility | WCAG 2.1 AA equivalent compliance |
| Localization | English complete, 4+ languages supported |
| Legal | All ratings obtained, compliance verified |

### 28C: Launch Day Operations

| Time | Action |
|------|--------|
| T-7 days | Final build candidate locked |
| T-3 days | Build uploaded to storefront |
| T-1 day | Server infrastructure scaled to projected load |
| T-0 | Launch, team on standby for hotfixes |
| T+1 hour | First telemetry review |
| T+4 hours | Server health check, scaling adjustment |
| T+24 hours | Day-1 metrics review, hotfix if critical |
| T+7 days | Week-1 retrospective, first patch planning |

### 28D: Post-Launch Roadmap (initial)

| Timeline | Content |
|----------|---------|
| Month 1 | Stability patches, balance tuning based on telemetry |
| Month 2 | First seasonal event, new recipes |
| Month 3 | New district, expanded faction AI |
| Month 6 | Major expansion (new city/region) |
| Year 1 | Modding workshop, community-driven content |

---

## Phase 5-6 Aggregate Metrics

### Script Count

| Step | New Scripts |
|------|------------|
| Step 20: Procedural Music | 5 |
| Step 21: Advanced Procedural Gen | 4 |
| Step 22: UI/UX Polish | 11 |
| Step 23: Accessibility | 7 |
| Step 24: Content Pipeline | 0 (data assets only) |
| Step 25: Scale Testing | 4 (test suites) |
| Step 26: Live Ops | 10 |
| Step 27: Early Access Launch | 0 (integration + polish) |
| Step 28: Full Launch | 0 (integration + polish) |
| **Total** | **41** |

**Final script count: ~234 scripts (193 Phase 1-4 + 41 Phase 5-6)**

### Overall Project Timeline

| Phase | Steps | Duration | Cumulative |
|-------|-------|----------|-----------|
| Phase 1: Foundation | 1-4 | Complete | Complete |
| Phase 2: Vertical Slice | 5-10 | Complete | Complete |
| Phase 3: Advanced Empire | 11-15 | 8-12 weeks | ~12 weeks |
| Phase 4: World and Multiplayer | 16-19 | 12-16 weeks | ~28 weeks |
| Phase 5: Content and Polish | 20-24 | 10-14 weeks | ~42 weeks |
| Phase 6: Ship | 25-28 | 8-12 weeks | ~54 weeks |
| **Early Access** | | | **Q3 2027** |
| **Full Launch** | | | **Q4 2027** |
