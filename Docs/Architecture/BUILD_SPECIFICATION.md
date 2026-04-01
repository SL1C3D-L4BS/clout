# CLOUT — Build Specification v2.0

## Criminal Ecosystem Simulator | Full-Stack AAA-Indie Masterclass

**Project:** CLOUT — Criminal Logistics & Operations in Urban Territories
**Studio:** SlicedLabs
**Engine:** Unity 6.2+ | HDRP (high-end) / URP (scalable) dual pipeline
**Target Platforms:** PC (Steam) → Console (PS5/Xbox Series X) → Mobile Companion
**Multiplayer:** Persistent world shards (10,000+ concurrent) + Private sessions (1–4 co-op)
**Art Style:** Synty POLYGON Low-Poly + Selective PBR Interiors | Neon-Noir Aesthetic
**Target Release:** Early Access Q3 2027 | Full Launch Q4 2027
**Team Size Target:** 8–14 developers
**Document Version:** 2.0 — March 2026
**Author:** the_architect × Claude (AI-Assisted Architecture)

---

## Core Design Principles (Non-Negotiable)

1. **Simulation > Content** — If a feature cannot be expressed as interacting systems, it does not ship.
2. **Emergence > Handholding** — The game should feel like discovering criminal physics, not following a quest log.
3. **Consequences > Balance** — The world must be unfair sometimes. Choices have permanent weight.
4. **Player Agency > Scripted Drama** — Every major event must be traceable to player or AI decisions.
5. **Scalable Depth** — Systems must be understandable at 10 hours and still rewarding at 1,000 hours.

---

## Table of Contents

### Part I — Vision & Identity
1. Product Vision 2026
2. Core Game Identity
3. Core Game Loop
4. Art Direction & Tone

### Part II — Player Systems
5. Player Identity & Progression
6. Player Controller (Hybrid)
7. Combat Systems (Melee + Ranged)
8. Skill & Attribute Framework

### Part III — Empire Systems
9. Empire Management Architecture
10. Production & Cooking (Masterclass Depth)
11. Signature & Forensics System
12. Infrastructure & Facility System
13. Workforce & NPC Graph
14. Supply Chain & Logistics
15. Money Laundering Pipeline

### Part IV — World Systems
16. Procedural World Generation
17. Global Regions & Transit Network
18. Territory Control System
19. Open World Districts
20. Vehicle System
21. Property & Base System
22. Climate, Weather & Seasonal Events

### Part V — Economy & Markets
23. Multi-Layer Economy Simulation
24. Player-Driven Market Dynamics
25. Cryptocurrency & Dark Web Layer

### Part VI — Law Enforcement & Investigation
26. 5-Dimensional Heat System
27. Investigation Graph AI (The Star System)
28. FBI Profiler & Behavioral Analysis
29. Police, State, Federal Response Tiers
30. Undercover & Informant Systems

### Part VII — AI & Faction Systems
31. AI Architecture (Utility + BT + GNN)
32. NPC Personality & Loyalty Graph
33. AI Faction Simulation
34. Rival Gang AI & Territorial AI
35. Civilian Population Simulation

### Part VIII — Social & Political Systems
36. Reputation System (4D Vector)
37. Corruption & Political Influence
38. Fear vs Respect Dynamics
39. Alliance, Betrayal & Diplomacy
40. Media & Public Perception

### Part IX — Multiplayer & Persistence
41. Multiplayer Architecture (10K+ Shards)
42. Player Organizations & Syndicates
43. PvPvE Territory Wars
44. Persistence & Event Sourcing
45. Cross-Play & Spectator Systems

### Part X — Technical Architecture
46. High-Level Tech Stack
47. Client Architecture (Unity 6 DOTS/ECS)
48. Server Architecture (Microservices)
49. Database & Graph Architecture
50. Anti-Cheat & Security
51. Simulation Tick & Performance

### Part XI — UI/UX Systems
52. HUD & Diegetic Interface
53. Phone / Management Interface
54. Command Mode (Tactical View)
55. War Room (Strategic View)
56. Investigation Dashboard (Spectator)
57. Accessibility Design

### Part XII — Content & Polish
58. Audio Design & Procedural Music
59. Visual Effects & Post-Processing
60. Cinematic Replay System
61. Heist System (Endgame)
62. Procedural Crime Events
63. Permadeath & Legacy Mode

### Part XIII — Production
64. Development Phases (0–7)
65. Next Steps & Immediate Actions
66. Asset Pipeline & Synty Integration
67. Monetization (Fair & Player-First)
68. Post-Launch Roadmap
69. Performance Targets
70. Ethical & Community Guardrails

---

> **Full section content for each of the 70 sections is maintained in the master design document.**
> **This file serves as the canonical table of contents and specification index.**
> **For detailed section content, see: `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md`**

---

## Implementation Status vs Spec

See: `Docs/Architecture/GAP_ANALYSIS.md` for complete mapping of spec sections to codebase.

---

## Development Phases

### Phase 0: Foundation ✅ COMPLETE
- Merged Sharp Accent codebases (Souls-like + COD-like)
- SO architecture, assembly definitions
- URP pipeline configured
- Basic movement, camera, input
- Test scene with placeholder city block

### Phase 1: Core Loop ✅ COMPLETE
- Melee combat (attacks, combos, parry, block, backstab)
- Ranged combat (shooting, ADS, recoil, cover)
- Inventory and items
- NPC dealing mechanic
- Basic production (crafting stations, 6 types)
- Simple economy (per-district supply/demand)
- One playable procedural city block
- Basic heat system (6-tier wanted)

### Phase 2: Empire Systems 🟡 70% (Steps 1–5 of 10 Complete)
- [x] Full production chain (CraftingStation, 6 types, risk events)
- [ ] Signature system (batch forensics) — NOT STARTED
- [ ] Worker hiring and management (NPC graph) — TEMPLATE ONLY
- [x] Property purchase + upgrade trees
- [ ] Territory control (5 conquest methods) — SKELETON ONLY
- [ ] Full 5D heat system — 1D ONLY (Local PD)
- [ ] Police AI (patrol, investigate, chase, raid) — NOT STARTED
- [ ] Money laundering (3+ methods) — BASIC METHOD ONLY
- [x] Save system

### Phase 3–7: See `Docs/Architecture/NEXT_STEPS_ROADMAP.md`

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

---

## Monetization (Fair & Player-First)

- Cosmetic only: syndicate logos, vehicle skins, safehouse themes
- Expansion packs: new global regions (not pay-to-win)
- No loot boxes — ever
- Battle pass: cosmetic unlocks through play or purchase

---

*CLOUT Build Specification v2.0 — Criminal Ecosystem Simulator*
*SlicedLabs — March 2026*
*"You are not shipping a game. You are shipping a criminal universe operating system."*
