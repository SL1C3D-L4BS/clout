# CLOUT -- Phase 3 Completion Log

> Version 1.0 | April 2, 2026
> Status: IN PROGRESS (Step 11 of 15 complete)

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

## Upcoming Steps

| Step | Title | Status | Est. Scripts |
|------|-------|--------|-------------|
| 11 | Money Laundering Pipeline | **COMPLETE** | 5 delivered |
| 12 | Signature & Forensics System | UP NEXT | ~4-5 |
| 13 | Advanced Economy / Market Simulator | PLANNED | ~3-4 |
| 14 | Rival Faction AI | PLANNED | ~5-6 |
| 15 | Advanced Police & Investigation | PLANNED | ~4-5 |

---

*CLOUT Phase 3 Completion Log -- SlicedLabs -- April 2, 2026*
