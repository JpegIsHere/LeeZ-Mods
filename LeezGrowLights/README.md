# LeezGrowLights

A powered grow-light mod for **7 Days to Die V3.1.x** that enables enclosed/underground farming, accelerates crop growth while a qualifying LeeZ grow light is powered and switched on, and now supports per-placed-light colour selection on the development branch.

**Current development branch:** `dev/colour-system` (`v0.7.0-dev7`, `ModInfo.xml` version `0.7.0.0`)  
**Known-good colour code baseline:** `c7f70f408ba72d8657422caf131a9d2404e5457a`  
**Stable main branch:** `0.5.1.0`  
**Validated game build:** `V 3.1.0 (b14)`  
**Status:** core underground grow-light behaviour, progress-preserving electrical transitions, per-light colour persistence, save/reload colour restoration, and immediate live colour tinting are proven in-game. Dedicated-server/remote-client colour routing remains pending.

## What works now

Verified in-game:

- Six grow-light tiers (T1-T6).
- Normal vanilla electrical wiring and on/off behavior.
- Artificial sunlight for fully enclosed or underground farm plots.
- Exact **5x5 horizontal coverage** (radius 2 from the farm plot).
- Vertical coverage from **1 through 10 blocks above the farm plot**.
- Lamps at **11+ blocks above** are outside the effective range by design.
- Growth-speed multiplier is read from XML.
- Overlapping lights do not stack; the **highest active multiplier wins**.
- Power and switch state are checked from the V3.1 powered tile entity APIs.
- Crop placement/survival/growth light checks remain vanilla-controlled except that an active LeeZ light temporarily satisfies the crop's sunlight threshold.
- Harmony crop hooks and sunlight-substitution hooks load successfully on V3.1.0 b14.
- Mid-stage T6 `1x <-> 4x` transitions preserve earned progress and reschedule only remaining crop work.
- Mid-stage T4 `1x <-> 1.5x` transitions also reschedule proportionally.
- A per-light radial-menu colour command cycles through Blue, Green, Red, Purple, White and Yellow.
- Selected colour is stored on the placed block and survives save/quit/restart.
- Dev7 updates the visible lamp colour immediately while playing; a reload is no longer required.

## Grow-light tiers

| Tier | Wiring 101 unlock | Growth speed | 63-minute vanilla stage equivalent |
|---|---:|---:|---:|
| T1 | 10 | 1.2x | 52.5 min |
| T2 | 20 | 1.3x | ~48.46 min |
| T3 | 30 | 1.4x | 45 min |
| T4 | 45 | 1.5x | 42 min |
| T5 | 60 | 1.6x | 39.375 min |
| T6 | 75 | 4.0x | 15.75 min |

All tiers use the vanilla flat LED panel geometry and a provisional **10 W** power draw. On `dev/colour-system`, each placed light can be visually tinted independently.

## Coverage rules

A LeeZ light affects a farm plot only when all of the following are true:

1. The light is a LeeZ grow-light block with XML grow-light metadata.
2. It is powered.
3. It is switched on.
4. Its horizontal offset from the farm plot is at most 2 blocks on X and Z (5x5 total area).
5. Its block is between 1 and 10 blocks above the farm plot.

If more than one active grow light covers a crop, only the highest multiplier is applied.

## Underground farming

An active LeeZ light acts as an artificial sunlight source inside its coverage area. This allows a seed to be planted in a completely enclosed room without a skylight. When no qualifying active light covers the crop, vanilla light requirements apply normally.

The mod does **not** globally disable crop sunlight rules.

## Mid-stage power transitions

The development runtime contains a live-validated direct rescheduler. When the effective multiplier changes, it reads the crop's queued scheduled time, converts the remaining ticks back into equivalent vanilla work using the old multiplier, invalidates the old scheduled update, then schedules the remaining work using the new multiplier.

Conceptually:

```text
remaining vanilla work = remaining queued ticks * old multiplier
new remaining ticks    = remaining vanilla work / new multiplier
```

Live V3.1 tests have confirmed proportional transitions including T6 `1x <-> 4x` and T4 `1x <-> 1.5x`. Progress already earned before the transition stays earned; only future growth rate changes.

See [docs/STAGE_0_5_STATUS.md](docs/STAGE_0_5_STATUS.md) and [MIDSTAGE_TESTING.md](MIDSTAGE_TESTING.md) for the detailed evidence.

## Per-light colour system

The current colour runtime uses six colours:

`Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue`

Legacy/uninitialised lights default to White.

Colour is stored in `BlockValue.meta2`, while normal electrical toggle state remains in `TileEntityPoweredBlock.isToggled`. This preserves the powered tile entity and wiring model rather than replacing the block with a different tile-entity type.

V3.1 radial commands are identified by `_commandName:String`; the working colour handler therefore matches the dynamic `Grow light colour:` command string rather than assuming a numeric command index.

The visual path applies the selected tint to `BlockEntityData.SetMaterialColor` and child Unity `Light.color` components. Dev7 caches the live `BlockEntityData` by block position and repaints it immediately after each successful colour write.

Live validation on dev7 confirmed:

- the colour command cycles all configured colours;
- the saved colour survives quit/restart;
- the restored lamp displays the saved colour;
- live colour changes update the lamp immediately without restarting.

Known cosmetic issue: the radial menu can still render a key-style label such as `blockcommand_growlightcolour: Blue` even though `Config/Localization.csv` contains a friendly `blockcommand_growlightcolour` entry.

Remote-client colour authoring is intentionally blocked until proper server command routing is implemented, so multiplayer colour synchronization is **not** yet claimed.

See [docs/COLOUR_SYSTEM.md](docs/COLOUR_SYSTEM.md) and [docs/COLOUR_DEV7_HANDOFF.md](docs/COLOUR_DEV7_HANDOFF.md).

## Installation / build

Copy the `LeezGrowLights/` directory from this repository into your game `Mods/` folder so the game sees:

```text
7 Days To Die/
  Mods/
    0_TFP_Harmony/
    LeezGrowLights/
      Config/
      Source/
      ModInfo.xml
```

Build from a Visual Studio Developer Command Prompt:

```bat
cd /d "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
msbuild "Mods\LeezGrowLights\Source\LeezGrowLights.csproj" /p:Configuration=Release
```

The current development project references:

- `Assembly-CSharp.dll`
- `LogLibrary.dll`
- `UnityEngine.CoreModule.dll`
- TFP Harmony's `0Harmony.dll`

A successful Release build writes `LeezGrowLights.dll` into the mod root. The DLL/PDB are ignored by Git.

See [docs/BUILDING.md](docs/BUILDING.md) for custom paths and details.

## Current validation status

Confirmed during V3.1 in-game testing:

- crop scheduling and tick-rate Harmony hooks install;
- sunlight substitution hooks install;
- underground/enclosed planting works with an active grow light;
- tiered grow-light behaviour works in-game;
- direct mid-stage multiplier transitions preserve earned progress;
- save/reload and chunk-unload/reload growth continuity have been exercised during development;
- grow-light removal and transition plumbing have been added to the V3.1 powered-block path;
- colour command activation uses the actual V3.1 `_commandName` contract;
- colour state persists through the V3.1 `BlockChangeInfo`/`BlockValueRef` block-change path;
- save/quit/restart restores the selected colour;
- dev7 live visual tint refresh works immediately for colour cycling.

Still pending or intentionally deferred:

- dedicated-server / remote-client colour routing and synchronization;
- cleanup/fix of the key-style dynamic colour command label;
- final release-gate regression sweep across all earlier Stage 0-5 behaviours after colour work;
- removal or gating of temporary diagnostic logging before a polished release;
- optional cosmetic brightness interaction, which is the next planned development topic.

## Repository map

```text
Config/                    XML blocks, recipes, progression and localization
Source/                    C# runtime and MSBuild project
  Harmony/                 Harmony installation, crop/electrical/removal/colour hooks
  Runtime/                 scanning, power, growth scheduling, colour state and visual tint
docs/                      design, API validation, status and development handoffs
docs/COLOUR_SYSTEM.md      current colour architecture/status
docs/COLOUR_DEV7_HANDOFF.md detailed dev7 colour handoff and brightness starting point
docs/reference/            successful V3.1 API probe output
docs/images/               early design/reference screenshots
tools/                     V3.1 API probe scripts
CHANGELOG.md               version history
ROADMAP.md                 development stages
TESTING.md                 verified vs implemented vs pending test matrix
MIDSTAGE_TESTING.md        mid-stage transition test procedure/results
ModInfo.xml                7DTD mod metadata
```

## License

No project license has been selected yet.
