# LeezGrowLights

A powered grow-light mod for **7 Days to Die V3.1.x** that enables enclosed/underground farming and accelerates crop growth while a qualifying LeeZ grow light is powered and switched on.

**Current development branch:** `0.5.3-dev2` (`ModInfo.xml` version `0.5.3.0`)  
**Stable main branch:** `0.5.1.0`  
**Validated game build:** `V 3.1.0 (b14)`  
**Status:** core underground grow-light system is proven in-game; the first progress-preserving mid-stage power-transition implementation is now in development testing.

## What works now

Verified on the stable/core runtime:

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

Implemented on `dev/midstage-reschedule-probe` in `0.5.3-dev2` and awaiting live validation:

- V3.1 `WorldBlockTicker` scheduled-entry lookup.
- `InvalidateScheduledBlockUpdate(position, blockID)` + `AddScheduledBlockUpdate(position, blockID, ticks)` rescheduling.
- Mid-stage conversion of remaining queued time back to equivalent vanilla work using the old effective multiplier.
- Re-scheduling only the remaining work using the new effective multiplier.
- Electrical transition hooks for direct toggle, received-power, propagated-power and disconnect paths on `PowerConsumerToggle`.
- Required `UnityEngine.CoreModule.dll` compile reference for the new `GameManager.Instance` transition path.

## Grow-light tiers

| Tier | Wiring 101 unlock | Growth speed | 63-minute vanilla stage equivalent |
|---|---:|---:|---:|
| T1 | 10 | 1.2x | 52.5 min |
| T2 | 20 | 1.3x | ~48.46 min |
| T3 | 30 | 1.4x | 45 min |
| T4 | 45 | 1.5x | 42 min |
| T5 | 60 | 1.6x | 39.375 min |
| T6 | 75 | 4.0x | 15.75 min |

All tiers currently use the vanilla flat white LED panel visual and a provisional **10 W** power draw.

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

The development branch now contains the first exact rescheduling candidate. When the effective multiplier changes, it reads the crop's queued scheduled time, converts the remaining ticks back into equivalent vanilla work using the old multiplier, invalidates the old scheduled update, then schedules the remaining work using the new multiplier.

Conceptually:

```text
remaining vanilla work = remaining queued ticks * old multiplier
new remaining ticks    = remaining vanilla work / new multiplier
```

This is intended to preserve progress already earned before an ON/OFF or tier transition. It is **implemented but not yet claimed as passed** until the v0.5.3-dev2 live tests are completed.

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

The v0.5.3 development project references:

- `Assembly-CSharp.dll`
- `LogLibrary.dll`
- `UnityEngine.CoreModule.dll`
- TFP Harmony's `0Harmony.dll`

A successful Release build writes `LeezGrowLights.dll` into the mod root. The DLL/PDB are ignored by Git.

See [docs/BUILDING.md](docs/BUILDING.md) for custom paths and details.

## Current validation status

Confirmed during V3.1 in-game testing:

- stable v0.5.1 project builds with zero warnings/errors on the test installation;
- mod DLL loads;
- crop scheduling and tick-rate Harmony hooks install;
- sunlight substitution hooks install;
- underground/enclosed planting works with an active grow light;
- T6 is detected active and applies a 4x multiplier;
- a lamp 10 blocks above a farm plot is detected as valid;
- tiered grow-light behavior works in-game;
- the live V3.1 ticker probe confirmed `WorldBase.GetWBT()`, `WorldBlockTicker.InvalidateScheduledBlockUpdate`, `AddScheduledBlockUpdate`, `scheduledTicksDict`, `WorldBlockTickerEntry.scheduledTime`, and `GameTimer` tick access.

Pending for v0.5.3-dev2:

- successful compile after adding `UnityEngine.CoreModule.dll` reference;
- live T6 ON -> OFF -> ON mid-stage rescheduling test;
- T1 -> T6 and T6 -> T1 transitions;
- overlapping-tier transition where the highest effective multiplier does not change;
- save/reload during partially accelerated growth;
- dedicated-server / remote-client validation;
- isolated Y+11 and X/Z radius-3 boundary tests;
- colour selection, persistence, synchronization and runtime tinting;
- removal or gating of temporary diagnostic logging before a polished release.

See [TESTING.md](TESTING.md) and [MIDSTAGE_TESTING.md](MIDSTAGE_TESTING.md) for the test matrix.

## Repository map

```text
Config/                    XML blocks, recipes, progression and localization
Source/                    C# runtime and MSBuild project
  Harmony/                 Harmony installation, crop hooks and electrical transition hooks
  Runtime/                 scanning, power checks, growth scheduling and ticker adapter
docs/                      design, API validation and development decisions
docs/reference/            successful V3.1 API probe output
docs/images/               early design/reference screenshots
tools/                     V3.1 API probe script
CHANGELOG.md               version history
ROADMAP.md                 next development stages
TESTING.md                 verified vs implemented vs pending test matrix
MIDSTAGE_TESTING.md        v0.5.3 mid-stage transition test procedure
ModInfo.xml                7DTD mod metadata
```

## License

No project license has been selected yet.
