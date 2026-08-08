# Testing status

Target game build: **7 Days to Die V3.1.0 (b14)**.

Current development branch: **`dev/colour-system`**  
Current runtime candidate: **v0.7.0-dev8**  
Known-good colour source baseline: **`0a1967e94dcd153e8ad8ff40de545b8b9245903b`**

Legend: `PASS` = observed in-game/build output; `STATIC VERIFIED` = source/XML/project structure proves the intended rule but exact gameplay still needs a live run; `IMPLEMENTED` = code path exists and is wired but the exact scenario still needs validation; `PENDING` = not complete.

For earlier Stage 0-5 history, see `docs/STAGE_0_5_STATUS.md` and `MIDSTAGE_TESTING.md`. For the current colour architecture, see `docs/COLOUR_DEV7_HANDOFF.md`. For the brightness candidate, see `docs/BRIGHTNESS_DEV8_HANDOFF.md`. Multiplayer routing remains documented in `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| V3.1 build/startup | PASS | Current development DLL builds against the installed V3.1 assemblies and loads as `LeezGrowLights 0.7.0.0` |
| runtime banner | PASS | dev8 reports `Loading V3.1 grow-light runtime candidate v0.7.0-dev8` |
| crop scheduling/tick hooks | PASS | Installed successfully during repeated colour-development runs |
| sunlight substitution hooks | PASS | Installed successfully during repeated colour-development runs |
| electrical transition hooks | PASS | Existing transition hooks continued loading during colour-development runs |
| removal hook | PASS (startup) | `BlockPowered.OnBlockRemoved` hook continued loading during colour-development runs |
| colour/brightness interaction hooks | PASS / IMPLEMENTED | colour activation is live-validated; brightness uses the same patched activation hierarchy but still needs a brightness-specific live run |
| colour/brightness visual hooks | PASS / IMPLEMENTED | `OnBlockEntityTransformAfterActivated` and `updateLightState` are live-validated for colour; brightness still needs live validation |

## Core grow-light behaviour

Previously validated and still present on the colour branch:

| Test | Status | Notes |
|---|---|---|
| vanilla wiring/on-off | PASS | powered LeeZ lights remain normal powered blocks |
| enclosed/underground farming | PASS | artificial-sunlight substitution works under active LeeZ coverage |
| 5x5 footprint | PASS | radius 2 horizontal coverage |
| vertical range 1..10 | PASS / STATIC VERIFIED | Y+10 was live-validated; source enforces the inclusive 1..10 range |
| highest active multiplier wins | PASS / IMPLEMENTED | existing overlap rule retained |
| T6 4x growth | PASS | live runtime validation |
| progress-preserving power transition | PASS | T6 and T4 proportional rescheduling live-validated |
| save/reload growth continuity | PASS | exercised during Stage 0-5 development |
| chunk unload/reload growth continuity | PASS | exercised by travelling thousands of blocks and returning |

See `docs/STAGE_0_5_STATUS.md` for detailed evidence and exact ratios.

## Colour system

| Test | Status | Notes |
|---|---|---|
| colour command visible | PASS | first fixed in dev2 |
| V3.1 command activation recognized | PASS | working path matches `_commandName:String`, not a numeric index |
| White/default state | PASS | legacy/uninitialised metadata maps to White |
| colour state advances | PASS | user observed cycling through colour names |
| Blue | PASS | included in successful live dev7/dev8 cycle |
| Green | PASS | included in successful live dev7/dev8 cycle |
| Red | PASS | included in successful live dev7 cycle |
| Purple | PASS | included in successful live dev7 cycle |
| White | PASS | included in successful live dev7 cycle |
| Yellow | PASS | included in successful live dev7 cycle |
| per-block persistence | PASS | dev6+ writes `BlockValue.meta2` through V3.1 block-change path |
| save/quit/restart persistence | PASS | light was left Green; after restart it rendered Green |
| visual colour after world load | PASS | persisted Green reapplied after restart |
| immediate live visual refresh | PASS | dev7 user result: lights cycle through the colours perfectly |
| normal electrical toggle still separate | PASS | colour uses block metadata; electrical state remains tile-entity toggle state |
| friendly radial-menu localization | PASS | dev8 displays `Grow light colour: Blue` and advances cleanly to `Grow light colour: Green` |
| dev8 menu + visual regression | PASS | user confirmed selecting once changes both Blue -> Green label and lamp immediately |
| remote-client colour authoring | PENDING | intentionally rejected until server routing exists |
| dedicated-server colour synchronization | PENDING | Multiplayer Light Sync remains unfinished |

## Brightness dev8 candidate

Brightness is implemented but has not yet been live-validated in V3.1. The current source stores colour + brightness together in `BlockValue.meta2`, preserves dev7 values `1..6`, and applies cosmetic intensity multipliers through the existing cached live block-entity path.

| Test | Status | Notes |
|---|---|---|
| brightness command visible | IMPLEMENTED | second LeeZ-only radial command using stable `growlightbrightness_<level>` tokens |
| friendly brightness localization | IMPLEMENTED | localization entries exist for Dim, Normal, Bright, Very Bright and Maximum |
| legacy/uninitialised default | STATIC VERIFIED | `meta2 = 0` decodes to White + Normal |
| dev7 compatibility | STATIC VERIFIED | `meta2 = 1..6` keep their existing colours at Normal brightness |
| Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal cycle | IMPLEMENTED | palette/state path exists; live radial cycle pending |
| immediate live brightness refresh | IMPLEMENTED | successful writes call `GrowLightColourVisual.TryApplyCached(...)` |
| colour preserved while changing brightness | STATIC VERIFIED | `WithBrightness` decodes/re-encodes the existing colour |
| brightness preserved while changing colour | STATIC VERIFIED | `WithColour` decodes/re-encodes the existing brightness bucket |
| brightness save/quit/restart | IMPLEMENTED | uses the same block-state persistence path as colour; live validation pending |
| no intensity compounding on repeated selections | STATIC VERIFIED | per-Unity-Light state tracks base intensity and last modded intensity |
| powered-light off/on restores selected brightness | IMPLEMENTED | visual postfix runs after powered-light state updates; live toggle test pending |
| relative intensity across tiers | IMPLEMENTED | candidate multiplies each child light's tracked vanilla/base intensity |
| crop growth/coverage/sunlight/tier behaviour unchanged | STATIC VERIFIED | brightness state is consumed only by the cosmetic visual layer |
| electrical power draw unchanged | STATIC VERIFIED | brightness does not mutate powered tile-entity state or power configuration |
| remote-client brightness authoring | PENDING | intentionally rejected until server routing exists |

Required live gate:

1. Confirm startup still reports `v0.7.0-dev8` with no Harmony exceptions.
2. Open a LeeZ grow light radial menu and confirm both colour and brightness commands are present and friendly/localized.
3. Starting from a legacy/dev7 light, confirm brightness initially shows Normal.
4. Cycle `Normal -> Bright -> Very Bright -> Maximum -> Dim -> Normal`; verify every step updates visibly without reload.
5. Select a non-White colour and change brightness several times; verify colour is preserved.
6. Select a non-Normal brightness and cycle colour; verify brightness is preserved.
7. Save, quit, restart; verify both colour and brightness restore.
8. Toggle the powered light off/on several times; verify the chosen brightness returns and does not progressively drift brighter or dimmer.
9. Repeat on at least two grow-light tiers; verify the multiplier remains relative to each tier's vanilla/base light intensity.
10. Verify crop growth speed, coverage, artificial sunlight, tier rules and power draw are unchanged.
11. Verify legacy/dev7 `meta2` values `1..6` restore the same colour at Normal brightness.

## Colour implementation milestones

- dev1: first runnable colour build; command missing.
- dev2: command visible; activation not recognized.
- dev3: activation diagnostics proved there is no numeric command index in the relevant V3.1 call.
- dev4: `_commandName` handling worked; persistence exposed an invalid `BlockChangeInfo` constructor assumption.
- dev5: runtime diagnostics exposed `BlockChangeInfo.blockValueRef`, `bChangeBlockValue`, and `blockValue`.
- dev6: `BlockValueRef` persistence worked; colour survived restart, but live visuals waited for a rebuild.
- dev7: cached live `BlockEntityData` and immediately reapplied the selected tint after each successful write. Live cycle passed.
- dev8: replaced the raw key-style colour menu with stable per-colour localization tokens. User confirmed `Grow light colour: Blue`, then a single selection advanced both menu and lamp to Green immediately. The same runtime candidate also contains the unvalidated brightness implementation documented above.

## Multiplayer Light Sync

Primary pending multiplayer work:

- route remote-client colour and brightness commands to the authoritative server;
- validate and persist visual-state changes server-side;
- propagate authoritative colour/brightness state to all clients;
- refresh each client's live lamp visual from replicated state;
- validate second-client observation, reconnect, and dedicated-server save/restart;
- preserve vanilla wiring/power and all existing crop behaviour.

See `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md` for the starting architecture and test gates.

## Later / release work

- Final regression sweep across Stage 0-5 after visual-state and multiplayer work.
- Long-duration sealed-room crop survival under normal play.
- Performance with dense farms / many lamps.
- Remove or gate temporary development diagnostics before a polished release.
