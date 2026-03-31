# CLOUT — Criminal Ecosystem Simulator 2026
# Full-Stack Masterclass Specification

> **Version 2.0** | March 2026
> Target Release: Q4 2027 (Early Access Q3 2026)
> Engineering Team: 8–14 devs (small AAA indie)

---

## Core Philosophy

**You are not shipping a game. You are shipping a criminal universe operating system.**

Every line of code, every database edge, every AI decision serves one question:

> *"What would actually happen if thousands of ambitious criminals, adaptive law enforcement, and greedy NPCs all pursued their own interests inside the same living economy?"*

The answer will be different on every server, every month, forever.

---

## 1. Product Vision

### Core Fantasy

You begin as a nobody with a burner phone and $500 in a procedurally generated 2026 Bay Area. Five years of real-time play later you command a transnational syndicate that can topple city governments, crash commodity markets, or trigger federal task forces — or you get dismantled by an AI detective who finally connected your signature batch to a laundered Cayman shell company.

### Unique Selling Points

- Fully persistent world with 10,000+ concurrent players per shard (cloud-scaled)
- No hand-crafted missions — every "quest" is emergent from interacting systems
- Investigation AI that learns player meta-strategies and adapts in real time
- True player-driven global black-market economy (prices collapse or skyrocket based on real player wars)
- Psychological and social layer: loyalty, fear, greed, and betrayal simulated at graph level

### Target Audience

- 18–45, hardcore strategy/simulation fans (EVE, Factorio, Dwarf Fortress, Escape from Tarkov)
- Players who want spreadsheets in the best way — with cinematic low-poly visuals and real stakes

### Success Metrics

- 70%+ of players still active after 90 days
- Average session >45 min
- Player-created syndicates organically control >60% of any given city district within 6 months of launch

### Art & Tone

Stylized low-poly + physically based rendering (Unity 6 HDRP 2026). Neon-noir cyber-crime aesthetic. Dynamic day/night, weather, and seasonal events that directly modify risk/demand curves.

---

## 2. High-Level Architecture (2026 Tech Stack)

### Client

| Layer | Technology |
|-------|-----------|
| Engine | Unity 6.2+ (DOTS/ECS + Entities 1.3+ for massive NPC simulation) |
| Rendering | Hybrid: low-poly base + selective high-fidelity interiors (addressables + streaming) |
| Networking | Client-side prediction + server reconciliation (Netcode for GameObjects + custom ECS transport) |
| Graphics | Optional ray-traced reflections, DLSS 4 / FSR 4 support for high-end PCs |

### Server

| Layer | Technology |
|-------|-----------|
| Runtime | Authoritative .NET 9 / C# 13 microservices on Kubernetes (Azure AKS / AWS EKS) |
| Simulation | High-performance C# with Burst + Jobs |
| AI Services | Lightweight on-server ML models (ONNX runtime) + optional cloud inference for elite "FBI profiler" tier |

### Core Services (containerized, independently scalable)

1. **World Simulation Service** — tick 1 Hz base, 4 Hz for active zones
2. **Economy & Market Engine** — event-driven
3. **AI & Faction Decision Service** — behavior trees + utility AI + graph neural net lite
4. **Investigation Graph Service** — Neo4j + custom in-memory graph cache
5. **Multiplayer Session & Replication Service**
6. **Persistence & Event Sourcing Layer**
7. **Anti-Cheat & Validation Service** — server-authoritative + behavioral ML

### Cross-Service Communication

gRPC + Redis pub/sub for real-time events; Kafka for long-term simulation logs and replayability.

---

## 3. World System

### 3.1 Procedural City Generation (San Francisco 2026 Template)

- **Inputs**: seed + 12 city templates (SF, LA, NYC, Miami, etc.) + player count scaling
- **Algorithm**: Multi-octave 3D Worley + Perlin noise for zoning → graph-based road generation (A* + organic growth) → building cluster placement via Poisson disk sampling
- **Dynamic layers**: law enforcement heat map, gang influence map, economic value map, underground tunnel network (smuggling)
- **Interiors**: on-demand procedural generation for key facilities using modular prefabs + Houdini-style runtime rules

### 3.2 Global Regions

- **24 source regions** (Colombian highlands, Golden Triangle, Mexican labs, Eastern European chemical plants)
- **12 transit hubs** (Panama Canal routes, Pacific shipping lanes, dark-web crypto ports)
- **8 consumption megacities** (with their own procedural districts)

Each region carries: risk profile (0–100), operation cost multiplier, LE strength curve, demand elasticity function, seasonal modifiers, and geopolitical events (e.g., "Cartel War" spikes that players can trigger or exploit).

### 3.3 Climate & Disaster Simulation (New 2026 Layer)

Droughts raise precursor chemical prices; port strikes create smuggling windows. Weather directly affects gameplay risk/demand curves.

---

## 4. Player System

### Identity Model

| Layer | Description |
|-------|------------|
| Public | Street name, social media presence |
| Criminal | Encrypted syndicate ID |
| Hidden | Reputation vectors (Fear / Respect / Reliability / Ruthlessness) as 4D vector updated via NPC interactions + graph analysis |

### Progression (Purely Systemic — No XP)

Milestones tied to:
- Network diameter (degrees of separation you control)
- Economic volume moved in last 30 days
- Territory control percentage (district + global)
- "Legend" score (media exposure + rival fear)

Unlocks are organic: higher Influence unlocks new laundering channels or advanced production tech trees.

---

## 5. Economy System (Masterclass Depth)

### Multi-Layer Market Formula

```
P(t) = P_base × (D(t) / S(t)) × (1 + E_r) × (1 + R_m) × M_s
```

Where:
- `D(t) / S(t)` — dynamic demand/supply ratio (updated every tick)
- `E_r` — price elasticity per region
- `R_m` — risk modifier from heat/investigation
- `M_s` — seasonal/geopolitical multiplier

### Signature System (Forensic-Level)

Every batch carries a 512-bit hidden vector (generated via seeded PRNG + operator skill + facility state). Law enforcement AI uses cosine similarity clustering to trace supply chains back to source facilities. Players can invest in "signature scrubbing" tech to reduce traceability (at cost of yield).

### Production Variability Equation

```
Output Quality = f(RawTier, EquipmentLevel, WorkerAvgSkill, HeatPenalty, RandomVariance + OperatorPersonalBonus)
```

### Product Lifecycle (7 Stages)

1. Raw sourcing → 2. Precursor acquisition → 3. Production/synthesis → 4. Quality control → 5. Packaging → 6. Distribution → 7. Street sale

Quality decay and signature propagation at every stage.

---

## 6. Production System

### Facility Upgrade Skill Trees

Upgrade paths form skill-tree style progressions:
- **"Ghost Lab" line** — reduces signature but increases power consumption (detectable by utilities)
- **"Industrial" line** — maximizes output but increases heat and explosion risk
- **"Boutique" line** — maximizes quality but limits batch size

### Risk Events

- Explosions (severity 0–1, scales damage/heat)
- Fume detection (neighbors notice)
- Power surges (utility company flags)
- Contamination (quality drops, health risk)

---

## 7. NPC Workforce System

### Utility AI + Loyalty Graph

NPC workforce uses lightweight utility AI with a loyalty graph. Betrayal probability:

```
P(betray) = (Greed + Fear - Loyalty + ExternalOfferValue) / CompartmentalizationFactor
```

### Worker Types

| Role | Function | Risk Factor |
|------|----------|-------------|
| Dealer | Autonomous street sales | Arrest, robbery, flip |
| Cook | Lab production | Explosion, contamination |
| Grower | Cultivation management | Detection, seizure |
| Guard | Property security | Combat death, bribery |
| Driver | Transport/delivery | Traffic stops, interception |
| Accountant | Money laundering | Audit trail, testimony |
| Lookout | Early warning system | Bribery, negligence |
| Enforcer | Territory enforcement | Murder charges, rivalry |

### Compartmentalization

Enforced mechanically — subordinates literally cannot see the full org chart. Information flows only through established edges in the network graph.

---

## 8. Network & Supply Chain System

### Directed Weighted Graph

Network system is a directed weighted graph where edges carry:
- Trust value (0–1)
- Debt balance
- Information-flow permissions

### War Room UI

Global supply chain visualized in-game as a node-link diagram the player can manipulate in a strategic "War Room" UI. Contracts are smart-contract-like agreements with escrow and penalties.

---

## 9. Money Laundering Pipeline

5-step pipeline with pattern-detection AI:

1. **Placement** — inject dirty cash into legitimate business
2. **Layering** — complex transactions across multiple entities
3. **Integration** — clean money re-enters as legitimate income
4. **Structuring** — break large amounts into sub-threshold deposits
5. **Verification** — AI flags "structuring" or "velocity anomalies" exactly like real financial crime units

---

## 10. Law Enforcement & Investigation AI (The Star of the Show)

### Investigation Graph (Neo4j + custom GNN-lite)

| Node Type | Examples |
|-----------|---------|
| Person | Player, NPC, informant |
| Location | Lab, safehouse, meeting point |
| Asset | Vehicle, property, bank account |
| Transaction | Cash deposit, purchase, transfer |
| Signature Batch | Product forensic fingerprint |

**Edges**: temporal, financial, communication, physical

### AI Profiler

- Centrality algorithms (PageRank + Betweenness) run every 30 in-game days to identify "persons of interest"
- FBI Profiler uses behavioral embedding vectors that evolve per player playstyle (aggressive vs. ghost vs. corporate)

### Heat System (5-Dimensional)

| Dimension | Threshold | Response |
|-----------|-----------|----------|
| Local PD | Low | Patrol increase, investigation |
| State | Medium | Task force, surveillance |
| Federal | High | Undercover ops, RICO charges |
| Rival | Variable | Territory wars, hits |
| Media | Variable | Public pressure, political response |

Each dimension has different escalation curves and response patterns.

---

## 11. AI Factions & Alliances

AI factions run the exact same simulation loop as players but with different decision weights. They can form, dissolve, or be absorbed.

### Faction Behaviors

- Territory expansion/defense
- Supply chain management
- Worker recruitment and loyalty management
- Alliance negotiation and betrayal
- Economic warfare (price dumping, supply disruption)

---

## 12. Multiplayer Architecture

- Shards support 10k+ players + 50k+ AI agents
- Player organizations can merge, splinter, or go to war with real territorial line-of-sight battles
- Cross-play PC / Console
- Spectator / "God mode" for content creators to watch investigation graphs unfold live

---

## 13. Persistence & Simulation

### Simulation Tick

Base 1 Hz global, with dynamic up-sampling in active conflict zones (max 10 Hz). All changes are event-sourced for replay, rollback, and "what-if" scenario tools for designers.

### Data Layer

| Store | Purpose |
|-------|---------|
| PostgreSQL | Structured game state |
| Neo4j | Investigation + network graphs |
| S3 / Blob Storage | Event logs, replay data |

Full world state snapshot every 7 real days for sharding and new player onboarding.

### Security

Server-authoritative + behavioral ML anti-cheat (flags impossible supply chain speeds or loyalty flips). Rate limiting per economic action.

---

## 14. Client Dashboards

- Real-time network graph viewer (zoomable, filterable)
- Heat radar map
- Supply-chain visualizer with risk heat overlay
- Personal "Legend" timeline
- War Room strategic overview

---

## 15. Visual & Audio Design

- Procedural music that intensifies with heat
- Voice synthesis for NPC calls (ElevenLabs-style integration)
- Cinematic replay system using investigation graph data

---

## 16. Accessibility

- Full color-blind modes
- Screen-reader compatible dashboards
- One-button economy macros for motor impairments

---

## 17. Monetization (Fair & Player-First)

- **Cosmetic only**: syndicate logos, vehicle skins, safehouse themes
- **Battle-pass style "Expansion Pack"**: unlocks new global regions faster (not pay-to-win)
- **No loot boxes**

---

## 18. Ethical & Community Guardrails

- In-game "responsible crime" tips (satirical)
- Robust reporting + AI moderation for toxic player behavior
- Transparency reports on economy health

---

## 19. Core Design Principles (Non-Negotiable)

1. **Simulation > Content** — if a feature cannot be expressed as an interacting system, it does not ship
2. **Emergence > Handholding** — the game should feel like discovering criminal physics
3. **Consequences > Balance** — the world must be unfair sometimes
4. **Player Agency > Scripted Drama** — every major event must be traceable to player or AI decisions
5. **Scalable Depth** — systems must be understandable at 10 hours and still rewarding at 1,000 hours

---

## 20. How This Maps to Current Codebase

### Already Built (Phase 1–2, 101 scripts)

| Vision System | Current Implementation | Status |
|--------------|----------------------|--------|
| Player Identity | PlayerStateManager + ReputationManager | Foundation |
| Economy | CashManager (dirty/clean) + EconomyManager + TransactionLedger | Foundation |
| Production | CraftingStation (6 types) + ProductionManager + recipes | Foundation |
| Distribution | DealManager + CustomerAI + SupplierNPC | Foundation |
| Properties | PropertyManager + PropertyDefinition + ProceduralPropertyBuilder | Foundation |
| Workers | EmployeeDefinition (SO template) | Template only |
| Investigation | WantedSystem (6-tier heat) | Basic |
| Combat | Full melee + ranged Souls-like pipeline | Complete |
| AI | Utility theory + detection + patrol/chase/attack | Complete |
| World | Procedural city block + street grid + 8 building types | Foundation |

### Needs Building (Phase 3–6)

| Vision System | Gap | Phase |
|--------------|-----|-------|
| Worker AI (autonomous dealers, cooks, guards) | No runtime worker behavior | Phase 2 Step 6 |
| Police AI (patrol, investigate, pursue, arrest) | Placeholder wanted system | Phase 3 |
| Investigation Graph (Neo4j-style) | No graph data structure | Phase 4 |
| Signature System (forensic tracing) | No batch fingerprinting | Phase 3 |
| Multi-region supply chains | Single-zone only | Phase 4 |
| Money laundering pipeline | Basic launder() method | Phase 3 |
| Faction AI (rival cartels) | No rival empire simulation | Phase 4 |
| Network graph (trust, debt, info-flow) | No social graph | Phase 4 |
| War Room UI | No strategic overview | Phase 5 |
| Multiplayer (10k shards) | FishNet disabled, singleplayer | Phase 6 |
| Procedural city (full SF template) | Single test arena block | Phase 4 |
| Climate/disaster simulation | Not started | Phase 5 |
| Voice synthesis | Not started | Phase 6 |
| Behavioral anti-cheat | Not started | Phase 6 |

---

*CLOUT Criminal Ecosystem Simulator 2026 — SlicedLabs — v2.0 March 2026*
