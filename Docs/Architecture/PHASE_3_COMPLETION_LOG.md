# CLOUT -- Phase 3 Completion Log

> Version 1.1 | April 2, 2026
> Status: IN PROGRESS (Steps 11-12 of 15 complete)

---

## Overview

Phase 3 adds depth, consequences, and advanced simulation layers to the empire. The first step delivers a full money laundering financial simulation replacing the placeholder CashManager.Launder() method.

---

## Step 11: Money Laundering Pipeline -- COMPLETE

**Date:** April 2, 2026
**New Scripts:** 5 (+1 editor utility)
**New Lines:** 2,564 (new files) + ~60 (integration edits)
**Modified Files:** 3 existing scripts

### Files Created

| File | Type | Lines | Purpose |
|------|------|-------|---------|
| `Scripts/Empire/Economy/Laundering/LaunderingMethod.cs` | ScriptableObject + Enums + Data | 201 | 5 enums (LaunderingMethodType, FrontBusinessType, LaunderingStage, IRSInvestigationStage), 3 data structs (LaunderingBatch, LaunderingRecord, IRSTriggerRecord), LaunderingMethod SO with risk/speed/capacity/fee profiles |
| `Scripts/Empire/Economy/Laundering/FrontBusiness.cs` | MonoBehaviour | 347 | Front business component on properties. 5 business type profiles (Restaurant, AutoShop, Nightclub, Laundromat, CarWash). Simulated revenue, suspicion tracking, capacity management, renovation tiers (0-3), bookkeeper support, IRS exposure calculation, audit logic, full save/load |
| `Scripts/Empire/Economy/Laundering/IRSInvestigation.cs` | Serializable Class | 678 | 4-stage investigation state machine (Flag/Investigation/Audit/Seizure). 6 transaction triggers (large deposit, round numbers, structuring, velocity anomaly, pattern detection, cross-business correlation). Permanent attention floor from lifetime thresholds ($50K/$250K/$1M/$5M). Accountant diminishing returns. Seizure with fines, property confiscation, cascade. 7 EventBus events. Full save/load |
| `Scripts/Empire/Economy/Laundering/LaunderingManager.cs` | Singleton | 776 | Core orchestrator. 5-stage real-time pipeline (Placement/Layering/Integration/Cooling/Complete). Front business registration/seizure. Method validation with CLOUT rank gating. Daily tick with weekly velocity tracking. IRS integration. Accountant counting. 5 EventBus events. Full save/load |
| `Scripts/UI/Laundering/LaunderingUI.cs` | OnGUI | 562 | Full dashboard: cash overview, IRS attention meter with threshold markers, front business list with suspicion bars, active pipeline progress, new operation panel with method/amount/preview, audit panel with contest. Toggle: L key |
| `Scripts/Editor/TMPWarningFix.cs` | Editor Utility | 60 | Fixes Unity 6 TMP "No Font Asset" editor warning by pre-warming default font on startup |

### Files Modified

| File | Change |
|------|--------|
| `Scripts/Core/GameBalanceConfig.cs` | +12 laundering tuning values: IRS decay rate, accountant reduction, safe ratio, suspicion growth, IRS thresholds (flag/investigation/audit), audit duration, seizure fine rate, IRS heat penalty |
| `Scripts/Core/Interfaces.cs` | +2 PropertyType entries: Laundromat, CarWash |
| `Scripts/Empire/Employees/WorkerManager.cs` | +1 method: GetWorkerCountAtProperty(string, EmployeeRole) for accountant counting at front businesses |
| `Editor/Clout.Editor.asmdef` | +1 assembly reference: Unity.TextMeshPro |

### EventBus Events Added (12)

| Event | Publisher |
|-------|----------|
| `LaunderingStartedEvent` | LaunderingManager |
| `LaunderingCompletedEvent` | LaunderingManager |
| `LaunderingFailedEvent` | LaunderingManager |
| `FrontBusinessRegisteredEvent` | LaunderingManager |
| `FrontBusinessSeizedEvent` | LaunderingManager |
| `IRSStageChangedEvent` | IRSInvestigation |
| `IRSFlagEvent` | IRSInvestigation |
| `IRSInvestigationStartedEvent` | IRSInvestigation |
| `IRSAuditStartedEvent` | IRSInvestigation |
| `IRSAuditPassedEvent` | IRSInvestigation |
| `IRSSeizureEvent` | IRSInvestigation |
| `IRSAuditContestedEvent` | IRSInvestigation |

### Integration Points

- **CashManager** -- SpendDirty on pipeline entry, EarnClean on completion
- **TransactionLedger** -- OnDayEnd drives daily processing
- **PropertyManager** -- Front businesses attach to owned Property GameObjects; seizure removes ownership
- **GameBalanceConfig** -- All 14 laundering config values read from GameBalanceConfig.Active
- **WorkerManager** -- Accountant role functional (reduces IRS growth, accelerates decay)
- **WantedSystem** -- IRS criminal charges add heat via AddHeat()
- **EventBus** -- 12 new event structs for cross-system reactivity
- **ReputationManager** -- CLOUT rank gates laundering method availability

### Validation

- [x] Zero compilation errors
- [x] Zero warnings (TMP warning fixed)
- [x] LaunderingManager singleton initializes on scene load
- [x] Front businesses attach to eligible property types (Restaurant, AutoShop, Nightclub, Laundromat, CarWash)
- [x] 5-stage pipeline processes batches in real-time
- [x] IRS attention rises with suspicious activity and decays daily
- [x] Investigation stages transition at correct thresholds
- [x] LaunderingUI dashboard toggles with L key
- [x] GameBalanceConfig exposes all tuning values in Inspector
- [x] Accountant role reduces IRS attention via diminishing returns formula

---

## Step 12: Signature & Forensics System -- COMPLETE

**Date:** April 2, 2026
**New Scripts:** 5
**New Lines:** ~1,800 (new files) + ~120 (integration edits)
**Modified Files:** 4 existing scripts

### Files Created

| File | Type | Lines | Purpose |
|------|------|-------|---------|
| `Scripts/Forensics/BatchSignature.cs` | Serializable Class | 389 | 512-dimensional forensic fingerprint vector. Deterministic generation from facility seed, recipe hash, operator hash, equipment config + random variance. Cosine similarity computation (full, facility-only, recipe-only). Scrub noise injection. Robert Jenkins' integer hash. Full save/load |
| `Scripts/Forensics/SignatureDatabase.cs` | Singleton | 531 | Central evidence database. Signature clustering by facility origin (cosine similarity > 0.85). Daily degradation (unreliable after ~60 game days). Scrubbed signatures degrade 3x faster. FIFO eviction at 500 max. FindRelated() and FindFacilityOrigin() queries. 3 EventBus events. Full save/load |
| `Scripts/Forensics/ForensicLabAI.cs` | Singleton | 469 | Queue-based evidence processing pipeline (1 item/day capacity). Auto-intake from PropertyRaidedEvent and WorkerArrestedEvent. Processing times by source (1-7 days). Quality ranges by source. Facility identification with heat generation via WantedSystem. Full save/load |
| `Scripts/Forensics/SignatureScrubber.cs` | MonoBehaviour | 276 | CraftingStation equipment upgrade (3 levels). Level 1: -5% yield, noise 0.15, $2,500. Level 2: -12% yield, noise 0.30, $8,000. Level 3: -20% yield, noise 0.50, $25,000. Toggle on/off without losing level. Static InstallOnStation() factory. Full save/load |
| `Scripts/UI/Forensics/ForensicsUI.cs` | OnGUI | ~280 | Forensic Intelligence Dashboard: database overview, lab status, cluster intel, recent analysis results, scrubber equipment status. Threat level indicator. Toggle: F key |

### Files Modified

| File | Change |
|------|--------|
| `Scripts/Empire/Crafting/CraftingStation.cs` | +using Clout.Forensics/Economy. CompleteBatch() now generates BatchSignature, applies SignatureScrubber if present, passes signature to ProductInventory. CraftingResult struct gains `signature` field. New ResolvePropertyId() helper walks parent hierarchy. Publishes BatchSignatureCreatedEvent |
| `Scripts/Empire/Dealing/ProductInventory.cs` | +using Clout.Forensics. New signature tracking dictionary (_signatureMap). AddProduct() overload accepting BatchSignature. GetSignature() query method for downstream propagation |
| `Scripts/Empire/Dealing/DealManager.cs` | +using Clout.Forensics. ExecuteDeal() propagates signatures: snitching customers auto-submit to ForensicLabAI. Undercover buy chance (2% base + 8% heat scaling) captures product signatures from street deals |
| `Scripts/Core/GameBalanceConfig.cs` | +11 forensic tuning values: cluster threshold, degradation rate, max reliable days, scrubbed degradation multiplier, max signatures, daily capacity, link confidence threshold, heat per link, undercover buy base chance, undercover buy heat scaling |

### EventBus Events Added (4)

| Event | Publisher |
|-------|----------|
| `BatchSignatureCreatedEvent` | CraftingStation (via SignatureScrubber.cs definition) |
| `ForensicLinkEstablishedEvent` | SignatureDatabase |
| `ForensicEvidenceSubmittedEvent` | ForensicLabAI |
| `ForensicAnalysisCompleteEvent` | ForensicLabAI |

### Data Structures Added

| Type | Purpose |
|------|---------|
| `BatchSignature` | 512-dim vector fingerprint (facility/recipe/operator/variance segments) |
| `BatchSignatureSaveData` | Serialization struct for BatchSignature |
| `ForensicEntry` | Database entry with signature, source, quality, reliability, age |
| `EvidenceItem` | Lab queue item with signature, source, processing time, quality |
| `ForensicResult` | Analysis output with facility identification and confidence |
| `FacilityLink` | Facility origin match with confidence, match count, product ID |
| `SignatureCluster` | Grouped signatures by facility seed with member indices |
| `SimilarityResult` | Query result with raw/effective/facility/recipe similarity scores |
| `ScrubProfile` | Internal scrub level config (yield penalty, noise, cost) |
| `ScrubberSaveData` | Serialization for SignatureScrubber state |
| `EvidenceSource` | 6-value enum: RaidSeizure, ArrestEvidence, StreetBuy, InformantTip, TrashPull, WorkerBetrayal |

### Integration Points

- **CraftingStation** -- BatchSignature generated on every CompleteBatch(), scrubbed if scrubber present, attached to ProductStack
- **ProductInventory** -- Signature tracked per product stack, propagated on deal execution
- **DealManager** -- Signatures enter forensic pipeline via snitch/undercover buy mechanics
- **TransactionLedger** -- OnDayEnd drives daily lab processing and signature degradation
- **WantedSystem** -- Confirmed facility links generate heat via AddHeat()
- **CashManager** -- Scrubber upgrades require clean cash
- **PropertyRaidSystem** -- Raids auto-submit evidence to ForensicLabAI via EventBus
- **WorkerManager** -- Worker arrests auto-submit evidence if worker had critical info
- **GameBalanceConfig** -- All 11 forensic config values read from GameBalanceConfig.Active
- **EventBus** -- 4 new event structs for cross-system reactivity

### Key Design Decisions

1. **512-dimensional vectors** -- 4 segments (facility/recipe/operator/variance) enable partial matching. Facility-only similarity allows quick source identification without full vector comparison.
2. **Deterministic generation** -- Same facility + recipe + operator always produces similar signatures (minus random variance), enabling realistic forensic clustering.
3. **Scrubber yield tradeoff** -- Players sacrifice 5-20% output to reduce forensic exposure. Level 2 drops below cluster threshold, Level 3 effectively randomizes. Creates meaningful strategic decision.
4. **Evidence degradation** -- 60-day max reliability prevents permanent consequences from old evidence. Scrubbed signatures degrade 3x faster.
5. **Signature propagation chain** -- CraftingStation -> ProductInventory -> DealManager -> ForensicLabAI. Each handoff preserves the forensic fingerprint.
6. **Undercover buy mechanic** -- Heat-scaled chance (2-10%) that any street deal captures a signature for the forensic lab. Adds risk to high-heat dealing.

### Validation

- [x] BatchSignature generates deterministic 512-dim vectors from crafting parameters
- [x] Cosine similarity correctly computes full and partial (facility/recipe) similarity
- [x] SignatureDatabase clusters related signatures above 0.85 threshold
- [x] ForensicLabAI processes evidence queue with source-based timing
- [x] SignatureScrubber injects noise reducing traceability at 3 levels
- [x] CraftingStation generates and attaches signature on CompleteBatch()
- [x] ProductInventory tracks signatures per stack for downstream propagation
- [x] DealManager propagates signatures via snitch and undercover buy paths
- [x] ForensicsUI dashboard displays database, lab, clusters, results, scrubbers
- [x] GameBalanceConfig exposes all 11 forensic tuning values in Inspector
- [x] All save/load serialization implemented for forensic state persistence

---

## Upcoming Steps

| Step | Title | Status | Est. Scripts |
|------|-------|--------|-------------|
| 11 | Money Laundering Pipeline | **COMPLETE** | 5 delivered |
| 12 | Signature & Forensics System | **COMPLETE** | 5 delivered |
| 13 | Advanced Economy / Market Simulator | UP NEXT | ~3-4 |
| 14 | Rival Faction AI | PLANNED | ~5-6 |
| 15 | Advanced Police & Investigation | PLANNED | ~4-5 |

---

*CLOUT Phase 3 Completion Log -- SlicedLabs -- April 2, 2026*
