# LeezGrowLights

A powered grow-light mod for **7 Days to Die V3.1.x** that supports enclosed/underground farming, tiered crop-growth acceleration, per-light colour selection, per-light brightness selection, normal powered-light wiring, and progress-preserving mid-stage power transitions.

**Current official release:** `0.7.0.2 / dev12`  
**Current source branch:** `dev/colour-system`  
**Validated game build:** `V3.1.0 (b14)`  
**Validated dev12 source commit:** `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`  
**Official tag:** `leezgrowlights-v0.7.0.2-v31`

The current code line includes the previously validated colour/brightness runtime plus the live-tested dev11 power/radial-icon fixes and the dev12 brightness-menu label correction.

## Current feature set

Verified during V3.1 development:

- Six grow-light tiers (T1-T6).
- Vanilla-style electrical wiring and on/off behavior.
- Artificial sunlight for enclosed/underground farm plots.
- Exact **5x5 horizontal coverage** (radius 2).
- Vertical coverage from **1 through 10 blocks above the farm plot**.
- Highest active multiplier wins when multiple LeeZ lights overlap.
- Progress-preserving mid-stage multiplier transitions.
- Per-placed-light colour selection.
- Per-placed-light brightness selection.
- Colour and brightness persistence through save/reload.
- Immediate live colour/brightness visual refresh.
- Brightness remains relative to each light's base/vanilla intensity.
- Directly wired grow lights draw **0 W while their own toggle is OFF** and restore their configured 10 W when ON.
- Visible built-in radial icons for Color (`tool`) and Brightness (`wrench`).
- Brightness radial label now advertises the **next level the click will apply**.

Full evidence status is maintained in [TESTING.md](TESTING.md).

## Grow-light tiers

| Tier | Wiring 101 unlock | Growth speed | 63-minute vanilla stage equivalent |
|---|---:|---:|---:|
| T1 | 10 | 1.2x | 52.5 min |
| T2 | 20 | 1.3x | ~48.46 min |
| T3 | 30 | 1.4x | 45 min |
| T4 | 45 | 1.5x | 42 min |
| T5 | 60 | 1.6x | 39.375 min |
| T6 | 75 | 4.0x | 15.75 min |

All current tiers use the vanilla flat LED panel visual and a configured **10 W** draw while switched on.

## Coverage rules

A LeeZ light affects a farm plot only when:

1. the block is a LeeZ grow light with the expected XML metadata;
2. it is powered;
3. it is switched on;
4. the farm plot is within 2 blocks on X and Z (5x5 total area);
5. the light is between 1 and 10 blocks above the farm plot.

If multiple active grow lights cover a crop, only the highest multiplier is used.

## Underground farming

An active LeeZ grow light can temporarily satisfy vanilla crop-light requirements inside its coverage area. This allows seeds/crops to operate in a sealed or underground room without globally disabling the game's sunlight rules.

When no qualifying active LeeZ light covers the crop, vanilla light requirements remain in control.

## Mid-stage growth transitions

The runtime preserves already-earned crop progress when the effective grow-light multiplier changes.

Conceptually:

```text
remaining vanilla work = remaining queued ticks * old multiplier
new remaining ticks    = remaining vanilla work / new multiplier
```

Live V3.1 testing confirmed transitions including T6 `1x <-> 4x` and T4 `1x <-> 1.5x`.

See:

- [MIDSTAGE_TESTING.md](MIDSTAGE_TESTING.md)
- [docs/STAGE_0_5_STATUS.md](docs/STAGE_0_5_STATUS.md)

## Colour system

Current colour cycle:

```text
Blue -> Green -> Red -> Purple -> White -> Yellow -> Blue
```

Legacy/uninitialised state defaults to White.

Colour is stored with the per-block visual state rather than in the powered tile's electrical toggle state. This keeps custom cosmetic state independent from normal wiring/on-off behavior.

The V3.1 activation handler is driven by a **string command name**. LeezGrowLights therefore uses stable custom command tokens such as `growlightcolour_<colour>` and localizes them through `Config/Localization.csv`.

## Brightness system

Current brightness levels:

```text
Dim -> Normal -> Bright -> Very Bright -> Maximum -> Dim
```

The radial selector advertises the **destination state**:

```text
current Dim         -> menu offers Normal
current Normal      -> menu offers Bright
current Bright      -> menu offers Very Bright
current Very Bright -> menu offers Maximum
current Maximum     -> menu offers Dim
```

The underlying brightness state/persistence/LightLOD behavior was live-validated during the dev10 brightness work. The dev12 change specifically corrects the selector label to use `Next(current)`.

## Direct-wired OFF-state power fix

During dev11 testing, a directly wired LeeZ light could still reserve 10 W while the light's own toggle was OFF.

The final fix preserves the configured block/tile wattage and synchronizes only the live `PowerConsumerToggle` load:

```text
ON  -> configured watts
OFF -> 0 W
```

The fix also resynchronizes during power-data initialization/deserialization so the correct OFF-state load survives save/reload.

This behavior and the radial icon repair were live-tested successfully before dev11 was promoted.

See [docs/DEV11_DEV12_REGRESSION_HANDOFF.md](docs/DEV11_DEV12_REGRESSION_HANDOFF.md).

## Installation

For normal use, download the complete package from the GitHub Release tagged:

`leezgrowlights-v0.7.0.2-v31`

Expected game layout:

```text
7 Days To Die/
  Mods/
    0_TFP_Harmony/
    LeezGrowLights/
      ModInfo.xml
      LeezGrowLights.dll
      Config/
        blocks.xml
        Localization.csv
        progression.xml
        recipes.xml
```

Source files and development documents are intentionally not required in the installed release package.

## Building from source

The project targets .NET Framework 4.8 and references the real V3.1 game/Harmony assemblies.

From a Visual Studio Developer Command Prompt, an installed-game build can use:

```bat
cd /d "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
msbuild "Mods\LeezGrowLights\Source\LeezGrowLights.csproj" /p:Configuration=Release
```

Primary references:

- `Assembly-CSharp.dll`
- `LogLibrary.dll`
- `UnityEngine.CoreModule.dll`
- TFP `0Harmony.dll`

For repeatable validation, the repository's GitHub Actions workflow installs a fresh dedicated server and builds against those assemblies automatically:

- `.github/workflows/build-leezgrowlights-v31.yml`

See [docs/BUILDING.md](docs/BUILDING.md).

## Future-mod reference material

This project intentionally keeps the investigation notes and API probes because they are useful beyond this single mod.

Start here:

- [docs/V31_MODDING_REFERENCE.md](docs/V31_MODDING_REFERENCE.md) — reusable V3.1 Harmony/API lessons.
- [docs/DEV11_DEV12_REGRESSION_HANDOFF.md](docs/DEV11_DEV12_REGRESSION_HANDOFF.md) — exact power/radial/brightness-label bug history and fixes.
- [docs/RELEASE_0.7.0.2.md](docs/RELEASE_0.7.0.2.md) — official asset hashes, commit and CI evidence.
- [docs/API_VALIDATION_V3.1.md](docs/API_VALIDATION_V3.1.md) — earlier V3.1 API validation.
- [docs/COLOUR_SYSTEM.md](docs/COLOUR_SYSTEM.md) — colour architecture.
- [docs/BRIGHTNESS_DEV8_HANDOFF.md](docs/BRIGHTNESS_DEV8_HANDOFF.md) — brightness architecture history.
- [docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md](docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md) — future multiplayer work.
- `tools/` — reflection/API/storage/multiplayer probe scripts.
- `docs/reference/` — retained probe output and raw regression report.

A broader file index is maintained in [docs/REPOSITORY_MANIFEST.md](docs/REPOSITORY_MANIFEST.md).

## Current limitations

- Remote-client colour/brightness authoring and full dedicated-server visual synchronization are still future work.
- The dev12 label correction passed source review and the complete V3.1 CI pipeline; a separate post-release gameplay confirmation of that UI-only correction is not yet recorded in `TESTING.md`.
- Dense-farm performance and long-duration sealed-room survival testing can still be expanded.

## Repository policy for binaries

Generated DLL/PDB files are intentionally not committed to the source tree. Official compiled binaries and full drop-in ZIPs are stored as GitHub Release assets. Source, XML, CI workflows, probe tools, evidence and development notes remain version-controlled.

## License

No project license has been selected yet.
