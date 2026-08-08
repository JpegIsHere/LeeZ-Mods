# Testing status

Target game build: **7 Days to Die V3.1.0 (b14)**.

Current development branch: **`dev/colour-system`**  
Current runtime candidate: **v0.7.0-dev8**  
Known-good colour code baseline: **`0a1967e94dcd153e8ad8ff40de545b8b9245903b`**

Legend: `PASS` = observed in-game/build output; `STATIC VERIFIED` = source/XML/project structure proves the intended rule but exact gameplay still needs a live run; `IMPLEMENTED` = code path exists and is wired but the exact scenario still needs validation; `PENDING` = not complete.

For the detailed earlier Stage 0-5 history, see `docs/STAGE_0_5_STATUS.md` and `MIDSTAGE_TESTING.md`. For the colour-development handoff, see `docs/COLOUR_DEV7_HANDOFF.md`.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| V3.1 build/startup | PASS | Current development DLL builds against the installed V3.1 assemblies and loads as `LeezGrowLights 0.7.0.0` |
| runtime banner | PASS | dev8 reports `Loading V3.1 grow-light runtime candidate v0.7.0-dev8` |
| crop scheduling/tick hooks | PASS | Installed successfully during repeated colour-development runs |
| sunlight substitution hooks | PASS | Installed successfully during repeated colour-development runs |
| electrical transition hooks | PASS | Existing transition hooks continued loading during colour-development runs |
| removal hook | PASS (startup) | `BlockPowered.OnBlockRemoved` hook continued loading during colour-development runs |
| colour interaction hooks | PASS | activation-command and activation hierarchy hooks installed |
| colour visual hooks | PASS | `OnBlockEntityTransformAfterActivated` and `updateLightState` hooks installed |

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
| dedicated-server colour synchronization | PENDING | not yet claimed |

## Colour implementation milestones

- dev1: first runnable colour build; command missing.
- dev2: command visible; activation not recognized.
- dev3: activation diagnostics proved there is no numeric command index in the relevant V3.1 call.
- dev4: `_commandName` handling worked; persistence exposed an invalid `BlockChangeInfo` constructor assumption.
- dev5: runtime diagnostics exposed `BlockChangeInfo.blockValueRef`, `bChangeBlockValue`, and `blockValue`.
- dev6: `BlockValueRef` persistence worked; colour survived restart, but live visuals waited for a rebuild.
- dev7: cached live `BlockEntityData` and immediately reapplied the selected tint after each successful write. Live cycle passed.
- dev8: replaced the raw key-style colour menu with stable per-colour localization tokens. User confirmed `Grow light colour: Blue`, then a single selection advanced both menu and lamp to Green immediately.

## Remaining correctness / release tests

- Dedicated-server / remote-client colour command routing and synchronization.
- Final regression sweep across Stage 0-5 after colour work.
- Long-duration sealed-room crop survival under normal play.
- Performance with dense farms / many lamps.
- Remove or gate temporary development diagnostics before a polished release.

## Next optional feature

Player-controlled **brightness** is the next planned development topic. It should remain cosmetic and independent of crop growth, coverage and electrical power draw. The recommended starting design is recorded in `docs/COLOUR_DEV7_HANDOFF.md`.
