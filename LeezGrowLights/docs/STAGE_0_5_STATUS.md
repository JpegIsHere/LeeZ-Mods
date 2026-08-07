# Stage 0-5 implementation and validation status

Prepared: 2026-08-07 (Australia/Melbourne)

Acceptance basis: `LeezGrowLights — Stage-by-Stage Test Plan`, Stages 0 through 5.

Runtime candidate under review: `e4bffacff6ef18c4b6faad41439627c1c142e381`
Branch: `dev/midstage-reschedule-probe`
Package version: `0.5.3.0`
Runtime banner: `v0.5.3-dev4`
Target game build: `7 Days to Die V3.1.0 (b14)`

Status meanings:

- `PASS` — previously observed in the target game/build and already recorded in repository evidence.
- `STATIC VERIFIED` — source/XML/project structure proves the intended rule, but the exact gameplay scenario still needs a live run.
- `IMPLEMENTED` — code path is present and wired, but live behavior still needs validation.
- `PENDING LIVE` — requires the target game/server and cannot be established from repository inspection alone.

## Stage 0 — Freeze the test baseline

| ID | Status | Evidence / action |
|---|---|---|
| 0.1 | STATIC VERIFIED | Development branch identified. Runtime code candidate is `e4bffacff6ef18c4b6faad41439627c1c142e381`. |
| 0.2 | STATIC VERIFIED | `ModInfo.xml` remains `0.5.3.0`; startup banner is now `v0.5.3-dev4`. |
| 0.3 | STATIC VERIFIED | Repository validation target remains V3.1.0 (b14). Actual launch evidence is still required for this candidate. |
| 0.4 | PENDING LIVE | Dedicated test-world creation/backup must be done in the game environment before persistence tests. |
| 0.5 | IMPLEMENTED AS PROCESS | Use the evidence fields from the Stage-by-Stage plan for every live run: date, candidate SHA, game build, crop/farm/lamp coordinates, power state, expected/actual result, tick/log evidence and PASS/FAIL. |

## Stage 1 — Build and startup gate

A Stage 1 build blocker was found during the audit: this is an old-style explicit C# project and the project file did not compile `GrowLightTransitionRescheduler.cs` or `BlockRemovalPatches.cs`, even though the active source referenced the rescheduler. Candidate `e4bffac...` fixes the manifest and adds the removal installer source.

| ID | Status | Evidence / action |
|---|---|---|
| 1.1 | IMPLEMENTED; PENDING LIVE BUILD | `.csproj` now includes all transition/removal source files. A Release build against the installed b14 assemblies must still confirm 0 errors/warnings. |
| 1.2 | PENDING LIVE | Launch and DLL discovery must be checked with the dev4 candidate. |
| 1.3 | IMPLEMENTED; previous hooks PASS | Crop scheduling, tick-rate, update-context and sunlight hook installers remain in place. Re-run startup on dev4. |
| 1.4 | IMPLEMENTED; PENDING LIVE | Electrical installer still targets toggle, received-power, propagated-power and declaring `PowerItem.HandleDisconnect()`. Dev4 also installs physical-removal handling separately. Confirm startup logs show all expected hooks and no Harmony warning. |
| 1.5 | PENDING LIVE | Requires startup/runtime log scan on the target installation. |

## Stage 2 — Six-tier electrical and content smoke test

| ID | Status | Evidence / action |
|---|---|---|
| 2.1 | STATIC VERIFIED | `blocks.xml` defines `leezGrowLightT1` through `leezGrowLightT6`. Placement/rendering remains a live smoke test. |
| 2.2 | STATIC VERIFIED / PENDING LIVE | Every tier extends `ceilingLight01_player`; wiring behavior must still be re-smoked in game. |
| 2.3 | IMPLEMENTED / prior behavior PASS | Runtime activation rule is powered AND toggled. Re-run powered+ON, powered+OFF and no-power for every tier. |
| 2.4 | STATIC VERIFIED | All six blocks request 10 W. |
| 2.5 | STATIC VERIFIED / PENDING LIVE | Lamp-state diagnostics are change-gated by `LastLampStates`; verify runtime logs remain low-noise. |
| 2.6 | STATIC VERIFIED / PENDING LIVE | Six recipes, six localization entries/descriptions, and Wiring 101 progression entries exist. Crafting/unlock presentation remains a live smoke test. |

## Stage 3 — Coverage and underground sunlight boundaries

The current scanner uses a full square radius of 2 around the farm block and vertical offsets 1 through 10 inclusive. The same scanner is used by growth-speed lookup and sunlight replacement.

| ID | Status | Evidence / action |
|---|---|---|
| 3.1 | PENDING LIVE | Build sealed no-sunlight baseline room. |
| 3.2 | PASS regression anchor | Enclosed/underground planting with an active LeeZ light was previously validated. Re-run on dev4. |
| 3.3 | PENDING LIVE | Explicit switched-OFF sunlight rejection test. |
| 3.4 | PENDING LIVE | Explicit unpowered sunlight rejection test. |
| 3.5 | STATIC VERIFIED | X/Z offset 2 is inside the inclusive radius-2 scan. |
| 3.6 | STATIC VERIFIED; PENDING LIVE BOUNDARY | X/Z offset 3 is outside the scan. Record an explicit rejection run. |
| 3.7 | STATIC VERIFIED | X=2, Z=2 is included because both axes are scanned independently across the full square. |
| 3.8 | STATIC VERIFIED; PENDING LIVE BOUNDARY | Positions beyond the radius-2 edge/corner are excluded. |
| 3.9 | STATIC VERIFIED; PENDING LIVE BOUNDARY | Y+1 is included. |
| 3.10 | PASS regression anchor | Y+10 was previously live-validated. Re-run on dev4. |
| 3.11 | STATIC VERIFIED; PENDING LIVE BOUNDARY | Y+11 is outside the inclusive 1..10 loop. |
| 3.12 | STATIC VERIFIED; PENDING LIVE BOUNDARY | Same-Y/below-farm lamps are outside the minimum +1 rule. |
| 3.13 | STATIC VERIFIED; PENDING LIVE | Sunlight substitution returns false without active LeeZ coverage; open-air vanilla behavior should be rechecked. |
| 3.14 | STATIC VERIFIED; PENDING LIVE | Sunlight and speed both delegate to `ScanFarmFootprint`; explicit paired boundary runs still required. |

## Stage 4 — Tier multiplier and overlap validation

Configured multipliers remain T1=1.2x, T2=1.3x, T3=1.4x, T4=1.5x, T5=1.6x, T6=4.0x. The scanner stores only the greatest active multiplier, so overlapping lights do not multiply together.

| ID | Status | Evidence / action |
|---|---|---|
| 4.1 | STATIC VERIFIED; PENDING LIVE TIMING | T1 1.2x; 63-minute equivalent 52.5 min. |
| 4.2 | STATIC VERIFIED; PENDING LIVE TIMING | T2 1.3x; ~48.46 min. |
| 4.3 | STATIC VERIFIED; PENDING LIVE TIMING | T3 1.4x; 45 min. |
| 4.4 | PASS regression anchor | T4 1.5x proportional transition previously validated; 42-minute equivalent. |
| 4.5 | STATIC VERIFIED; PENDING LIVE TIMING | T5 1.6x; 39.375 min. |
| 4.6 | PASS regression anchor | T6 4x timing/rescheduling previously validated; 15.75-minute equivalent. |
| 4.7 | STATIC VERIFIED; PENDING LIVE OVERLAP | Scanner selects the maximum active multiplier; no stacking expression exists. |
| 4.8 | STATIC VERIFIED; PENDING LIVE | Transition apply exits when old and new effective multipliers are equal, so toggling a lower tier while T6 still wins should not reschedule. |
| 4.9 | STATIC VERIFIED; PENDING LIVE | Same maximum-selection rule applies to non-T6 pairs such as T1+T4. |

## Stage 5 — Mid-stage transition correctness

The shared transition engine captures the old effective multiplier for affected growing plants, re-scans after the transition, and reschedules only when the effective multiplier changes. The ticker conversion remains:

`new remaining ticks = old remaining ticks × old multiplier ÷ new multiplier`

Candidate `e4bffac...` additionally wires physical grow-light removal through the V3.1 `BlockPowered.OnBlockRemoved` path and adds all removal/transition sources to the explicit project compile list.

| ID | Status | Evidence / action |
|---|---|---|
| 5.1 | PASS regression anchor | T6 1x→4x previously live-validated; re-run on dev4. |
| 5.2 | PASS regression anchor | T6 4x→1x previously live-validated; re-run on dev4. |
| 5.3 | PASS regression anchor | T4 1x↔1.5x previously live-validated; re-run on dev4. |
| 5.4 | IMPLEMENTED; PENDING LIVE | T1→T6 uses old-effective→new-effective conversion; isolated live test required. |
| 5.5 | IMPLEMENTED; PENDING LIVE | T6→T1 uses the same conversion; isolated live test required. |
| 5.6 | IMPLEMENTED; PENDING LIVE | If T6 turns off and T4 remains, max selection changes 4x→1.5x and one reschedule is requested. |
| 5.7 | STATIC VERIFIED; PENDING LIVE | If T6 remains winner, equality guard skips rescheduling. |
| 5.8 | IMPLEMENTED; PENDING LIVE | Received/disconnect electrical paths are patched; direct source outage needs live proof. |
| 5.9 | IMPLEMENTED; PENDING LIVE | Restore-power path is wired; live proof required. |
| 5.10 | IMPLEMENTED; PENDING LIVE | `HandlePowerUpdate` is patched for propagated changes; upstream relay test required. |
| 5.11 | IMPLEMENTED IN DEV4; PENDING LIVE | Physical removal hook now captures/applies the shared transition path. Verify effective-lamp removal preserves progress. |
| 5.12 | IMPLEMENTED IN DEV4; PENDING LIVE | Equality guard should make lower-tier removal a no-op when a higher tier still wins. |
| 5.13 | PENDING LIVE / EXPLORATORY | Placement/connect acquisition is not claimed by this candidate; document behavior. |
| 5.14 | IMPLEMENTED; PENDING LIVE EVIDENCE | Rescheduler logs old/new multiplier and remaining ticks. Capture before/after tick evidence for every transition. |

## Next live run order

Run the dev4 Release build/startup gate first. If it is clean, prioritize: explicit X/Z+3 and Y+11 rejection; T1↔T6; lower-tier toggle under T6; source outage/restore; upstream relay propagation; effective-lamp removal; lower-tier removal under a higher winner. Keep the previously known-good T6 and T4 transitions as regressions on the same candidate.

No Stage 0-5 item that requires the actual game runtime is marked PASS solely from source inspection.
