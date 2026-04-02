# CLOUT -- Build Specification v3.0

## Criminal Ecosystem Simulator | Full-Stack AAA-Indie Architecture

**Project:** CLOUT -- Criminal Logistics & Operations in Urban Territories
**Studio:** SlicedLabs
**Engine:** Unity 6.2+ | URP (current) / HDRP (Phase 5 target)
**Target Platforms:** PC (Steam) -> Console (PS5/Xbox Series X) -> Mobile Companion
**Multiplayer:** Persistent world shards (10,000+ concurrent) + Private sessions (1-4 co-op)
**Art Style:** Synty POLYGON Low-Poly + Selective PBR Interiors | Neon-Noir Aesthetic
**Target Release:** Early Access Q3 2027 | Full Launch Q4 2027
**Team Size Target:** 8-14 developers
**Document Version:** 3.0 -- April 2026
**Author:** the_architect x Claude (AI-Assisted Architecture)

---

## Core Design Principles (Non-Negotiable)

1. **Simulation > Content** -- If a feature cannot be expressed as interacting systems, it does not ship.
2. **Emergence > Handholding** -- The game should feel like discovering criminal physics, not following a quest log.
3. **Consequences > Balance** -- The world must be unfair sometimes. Choices have permanent weight.
4. **Player Agency > Scripted Drama** -- Every major event must be traceable to player or AI decisions.
5. **Scalable Depth** -- Systems must be understandable at 10 hours and still rewarding at 1,000 hours.

---

## Table of Contents

### Part I -- Vision & Identity
1. Product Vision 2026
2. Core Game Identity
3. Core Game Loop
4. Art Direction & Tone

### Part II -- Player Systems
5. Player Identity & Progression
6. Player Controller (Hybrid)
7. Combat Systems (Melee + Ranged)
8. Skill & Attribute Framework

### Part III -- Empire Systems
9. Empire Management Architecture
10. Production & Cooking (Masterclass Depth)
11. Signature & Forensics System
12. Infrastructure & Facility System
13. Workforce & NPC Graph
14. Supply Chain & Logistics
15. Money Laundering Pipeline

### Part IV -- World Systems
16. Procedural World Generation
17. Global Regions & Transit Network
18. Territory Control System
19. Open World Districts
20. Vehicle System
21. Property & Base System
22. Climate, Weather & Seasonal Events

### Part V -- Economy & Markets
23. Multi-Layer Economy Simulation
24. Player-Driven Market Dynamics
25. Cryptocurrency & Dark Web Layer

### Part VI -- Law Enforcement & Investigation
26. 5-Dimensional Heat System
27. Investigation Graph AI (The Star System)
28. FBI Profiler & Behavioral Analysis
29. Police, State, Federal Response Tiers
30. Undercover & Informant Systems

### Part VII -- AI & Faction Systems
31. AI Architecture (Utility + BT + GNN)
32. NPC Personality & Loyalty Graph
33. AI Faction Simulation
34. Rival Gang AI & Territorial AI
35. Civilian Population Simulation

### Part VIII -- Social & Political Systems
36. Reputation System (4D Vector)
37. Corruption & Political Influence
38. Fear vs Respect Dynamics
39. Alliance, Betrayal & Diplomacy
40. Media & Public Perception

### Part IX -- Multiplayer & Persistence
41. Multiplayer Architecture (10K+ Shards)
42. Player Organizations & Syndicates
43. PvPvE Territory Wars
44. Persistence & Event Sourcing
45. Cross-Play & Spectator Systems

### Part X -- Technical Architecture
46. High-Level Tech Stack
47. Client Architecture (Unity 6 DOTS/ECS)
48. Server Architecture (Microservices)
49. Database & Graph Architecture
50. Anti-Cheat & Security
51. Simulation Tick & Performance

### Part XI -- UI/UX Systems
52. HUD & Diegetic Interface
53. Phone / Management Interface
54. Command Mode (Tactical View)
55. War Room (Strategic View)
56. Investigation Dashboard (Spectator)
57. Accessibility Design

### Part XII -- Content & Polish
58. Audio Design & Procedural Music
59. Visual Effects & Post-Processing
60. Cinematic Replay System
61. Heist System (Endgame)
62. Procedural Crime Events
63. Permadeath & Legacy Mode

### Part XIII -- Production
64. Development Phases (0-7)
65. Next Steps & Immediate Actions
66. Asset Pipeline & Synty Integration
67. Monetization (Fair & Player-First)
68. Post-Launch Roadmap
69. Performance Targets
70. Ethical & Community Guardrails

---

> **Full section content for each of the 70 sections is maintained in the master design document.**
> **This file serves as the canonical table of contents, specification index, and implementation tracker.**
> **For detailed section content, see: `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md`**
> **For gap analysis vs codebase, see: `Docs/Architecture/GAP_ANALYSIS.md`**

---

## Implementation Status vs Spec

### Phase 0: Foundation -- COMPLETE

- [x] Merged Sharp Accent codebases (Souls-like + COD-like)
- [x] SO architecture, assembly definitions
- [x] URP pipeline configured
- [x] Basic movement, camera, input
- [x] Test scene with placeholder city block

### Phase 1: Core Loop -- COMPLETE

- [x] Melee combat (attacks, combos, parry, block, backstab)
- [x] Ranged combat (shooting, ADS, recoil, cover)
- [x] Inventory and items
- [x] NPC dealing mechanic
- [x] Basic production (crafting stations, 6 types)
- [x] Simple economy (per-district supply/demand)
- [x] One playable procedural city block
- [x] Basic heat system (6-tier wanted)

### Phase 2: Empire Systems -- COMPLETE (All 10 Steps)

**Status: 100% COMPLETE | ~139 scripts**

- [x] Full production chain (CraftingStation, 6 types, risk events)
- [x] Worker hiring and management (DealerAI, CookAI, GuardAI, WorkerManager, RecruitmentManager)
- [x] Property purchase + upgrade trees
- [x] Territory control (district system with DistrictManager, DistrictDefinition, ProceduralDistrictGenerator)
- [x] 6-tier heat + police AI (patrols, investigation, pursuit, raids)
- [x] Witness system + evidence degradation
- [x] Phone UI empire hub (Map, Contacts, Products, Finances, Messages)
- [x] Game flow manager + milestone tracking (17 milestones)
- [x] GameBalanceConfig (50+ tuning values, 3 difficulty presets)
- [x] PerformanceMonitor (FPS, memory, NavMesh agent budget, frame time analysis)
- [x] Save V2 (4D reputation, session stats, milestones, district state, full worker data)
- [ ] Signature system (batch forensics) -- DEFERRED to Phase 3
- [ ] 5D heat (multi-dimensional) -- DEFERRED to Phase 3 (current: single-axis 6-tier)
- [ ] Money laundering pipeline (full 5-step) -- DEFERRED to Phase 3
- [ ] Rival factions (AI organizations) -- DEFERRED to Phase 4
- [ ] Investigation graph (evidence linking, RICO) -- DEFERRED to Phase 4

#### Phase 2 Step Completion Log

| Step | Description | Scripts | Status |
|------|-------------|---------|--------|
| 1-5 | Core empire loop, economy, properties | 39 | COMPLETE |
| 5.5 | Spec v2.0 catch-up (4D rep, employee stats, price formula, events) | 4 modified | COMPLETE |
| 6 | Worker hiring (WorkerManager, DealerAI, CookAI, GuardAI, RecruitmentManager) | 10 new | COMPLETE |
| 7 | Police AI (PolicePatrolAI, HeatResponseManager, WitnessSystem, PropertyRaidSystem) | 5 new | COMPLETE |
| 8 | District system (ProceduralDistrictGenerator, DistrictManager, DistrictDefinition) | 4 new | COMPLETE |
| 9 | Phone UI (PhoneController, MapTab, ContactsTab, FinanceTab, ProductsTab, MessagesTab) | 6 new (1,905 lines) | COMPLETE |
| 10 | Integration + polish (GameFlowManager, GameBalanceConfig, PerformanceMonitor, SaveV2) | 3 new + 1 enhanced | COMPLETE |

### Phase 3-7: See `Docs/Architecture/NEXT_STEPS_ROADMAP.md`

---

## System Inventory (~139 Scripts)

| System | Scripts | Status |
|--------|---------|--------|
| Core State Machine | 7 | Complete |
| Controller Actions | 7 | Complete |
| Combat System | 9 | Complete |
| Camera System | 2 | Complete |
| Animation | 1 | Complete |
| Player | 2 | Complete |
| AI System | 7 | Complete |
| Network (offline stub) | 1 | Stub |
| Empire -- Crafting | 8 | Complete |
| Empire -- Dealing | 10 | Complete |
| Empire -- Economy | 4 | Complete |
| Empire -- Properties | 5 | Complete |
| Empire -- Employees | 10 | Complete |
| Empire -- Reputation | 1 | Complete |
| Empire -- Territory | 1 | Complete |
| World -- Police | 5 | Complete |
| World -- Districts | 4 | Complete |
| World -- NPCs | 3 | Complete |
| World -- Procedural | 2 | Complete |
| Stats | 1 | Complete |
| Inventory | 2 | Complete |
| UI / HUD | 6 | Complete |
| UI / Phone | 6 | Complete |
| Editor Tools | 10 | Complete |
| Utils | 4 | Complete |
| Save System | 1 | Complete |
| Core (GameFlow, Balance, Perf) | 3 | Complete |

---

## Script Count Projections

| Phase | New Scripts | Running Total | Status |
|-------|-------------|---------------|--------|
| Phase 0 (foundation) | -- | -- | COMPLETE |
| Phase 1 (core loop) | 62 | 62 | COMPLETE |
| Phase 2 Steps 1-5 | 39 | 101 | COMPLETE |
| Phase 2 Steps 5.5-10 | ~38 | ~139 | COMPLETE |
| Phase 3 (advanced empire) | ~25 | ~164 | NEXT |
| Phase 4 (world + multiplayer) | ~30 | ~194 | Planned |
| Phase 5 (content + polish) | ~20 | ~214 | Planned |
| Phase 6 (ship) | ~15 | ~229 | Planned |

---

## Development Phases Overview

### Phase 0: Foundation -- COMPLETE

Project scaffolding, engine setup, input system, URP configuration, assembly definitions, test scene.

### Phase 1: Core Loop -- COMPLETE

Full Souls-like melee combat, COD-like ranged combat, inventory, NPC dealing, crafting stations, basic economy, procedural city block, 6-tier wanted system.

### Phase 2: Empire Systems -- COMPLETE

Full production chain, worker hiring and autonomous operations, property management, territory control, police AI with patrols/investigation/pursuit/raids, witness system, phone UI empire hub, game flow management, balance configuration, performance monitoring, save system V2.

### Phase 3: Advanced Empire -- NEXT

Money laundering pipeline (5-step), signature and forensics system, advanced economy (full market simulation), rival faction AI, advanced police and investigation graph.

### Phase 4: World Expansion & Multiplayer

Multi-district city, global supply chain, multiplayer foundation (FishNet/Netcode), player organizations, territory wars, anti-cheat foundation.

### Phase 5: Content & Polish

Procedural music, advanced procedural generation, UI/UX polish (OnGUI -> UI Toolkit), accessibility pass, content pipeline (regions, NPCs, interiors, recipes, vehicles), HDRP migration.

### Phase 6: Ship

Scale testing (10K concurrent), live ops infrastructure, Early Access launch (Q3 2027), full launch (Q4 2027).

---

## Performance Targets

| Metric | Target | Minimum |
|--------|--------|---------|
| Frame Rate | 60 FPS (HDRP high) | 30 FPS (scalable) |
| Resolution | 4K (DLSS/FSR) | 1080p native |
| Load Time | <10 seconds | <20 seconds |
| Memory | <6 GB RAM | <8 GB |
| Server Tick | 4 Hz active zones | 1 Hz base |
| NPCs Per Shard | 50,000+ simulated | 10,000 minimum |
| Draw Calls (URP) | <1,500 batched | <3,000 |
| Physics Bodies | <500 active | <1,000 |
| NavMesh Agents | <200 active | <500 |
| Save File Size | <2 MB | <5 MB |

---

## Monetization (Fair & Player-First)

### Model: Premium with Cosmetic MTX

- **Base Game:** Premium purchase (no free-to-play)
- **Cosmetic Only:** Syndicate logos, vehicle skins, safehouse themes, character outfits
- **Expansion Packs:** New global regions, new city templates (not pay-to-win)
- **Battle Pass:** Cosmetic unlocks through play or purchase, no gameplay advantages
- **No loot boxes -- ever.** All purchases are direct and transparent.
- **No pay-to-win.** No purchasable gameplay advantages, stat boosts, or progression shortcuts.
- **No premium currency obfuscation.** If currency is used, it maps 1:1 to real cost.

### Revenue Streams

| Stream | Type | Timeline |
|--------|------|----------|
| Base Game (Steam) | One-time purchase | Early Access Q3 2027 |
| Cosmetic Store | Recurring | Launch + ongoing |
| Battle Pass (seasonal) | Recurring | Post-launch quarterly |
| Expansion Packs | One-time per DLC | Year 1+ |
| Console Launch (PS5/Xbox) | One-time purchase | Year 1 |
| Mobile Companion | Free with base game | Year 2 |

---

## Post-Launch Roadmap

| Timeframe | Content |
|-----------|---------|
| Year 1 | New global regions, advanced laundering mechanics, seasonal events (free updates) |
| Year 1+ | Console launch (PS5/Xbox Series X), expansion packs |
| Year 2 | Mobile companion app for managing remote facilities |
| Year 3 | VR War Room mode, modding API public release |

---

## Key Changes in v3.0 (from v2.0)

- Phase 2 upgraded from 70% to **100% COMPLETE** (all 10 steps done)
- Added complete step-by-step completion log for Phase 2
- Added full system inventory table (~139 scripts)
- Added script count projections table
- Expanded performance targets (draw calls, physics, NavMesh, save size)
- Expanded monetization section with revenue streams table
- Added post-launch roadmap timeline
- Pipeline clarified: URP (current) / HDRP (Phase 5 target)
- Phase 3 is now the active next milestone

---

## Related Documents

| Document | Path | Purpose |
|----------|------|---------|
| Game Design Document | `Docs/Design/GAME_DESIGN_DOCUMENT.md` | High-level design and market positioning |
| Criminal Ecosystem Spec | `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md` | Full 70-section detailed design |
| Viral Mechanics | `Docs/Design/VIRAL_MECHANICS_2026.md` | Streaming and social hooks |
| Next Steps Roadmap | `Docs/Architecture/NEXT_STEPS_ROADMAP.md` | Phase 3-6 execution plan |
| Gap Analysis | `Docs/Architecture/GAP_ANALYSIS.md` | Spec vs codebase mapping |
| Synty Asset List | `Docs/Architecture/SYNTY_ASSET_LIST.md` | Art pipeline and asset requirements |
| Phase 1 Execution Plan | `Docs/Architecture/PHASE_1_EXECUTION_PLAN.md` | Phase 1 build details |
| Phase 2 Masterclass Plan | `Docs/Architecture/PHASE_2_MASTERCLASS_PLAN.md` | Phase 2 build details |
| System Port Map | `Docs/Architecture/SYSTEM_PORT_MAP.md` | System dependencies and ports |

---

*CLOUT Build Specification v3.0 -- Criminal Ecosystem Simulator*
*SlicedLabs -- April 2026*
*"You are not shipping a game. You are shipping a criminal universe operating system."*
