# Dev11 -> Dev12 regression handoff

This document archives the V3.1 grow-light regression investigation completed on 2026-08-09 so the same evidence can be reused in future mods.

## Original in-game report

Two issues were reported after the validated colour/brightness build:

1. A grow light wired directly to a generator still consumed **10 W while the light's own toggle was OFF**. Wiring the light through a separate electrical switch avoided the issue.
2. The radial menu showed four slots, but the custom **Color** and **Brightness** slots had no visible icons.

After dev11 was validated and published, one additional minor UI issue was found:

3. The Brightness menu label described the **current** brightness instead of the **next brightness that clicking would apply**. For example, a menu entry showing `Maximum` would click through to `Dim` because the lamp was already at Maximum.

## Branch history

The first exploratory fix was made against the older `main` line and became draft PR #2. That branch was superseded once the validated colour/brightness runtime was identified on `dev/colour-system`.

The actual production fix was based on the validated dev10 colour/brightness runtime and developed on an isolated test branch before promotion.

Important rule for future work: when multiple development lines exist, identify the exact live-tested baseline before applying a regression fix.

## Power-draw root cause

V3.1 separates the powered light's visual/toggle state from the live electrical consumer load. The directly wired consumer could continue reserving `RequiredPower` while `PowerConsumerToggle.IsToggled` was false.

The fix does **not** change the XML/tile configured wattage. Instead, `GrowLightV31Fixes` synchronizes only the live consumer value:

```text
ON  -> configured required power (10 W for current LeeZ lights)
OFF -> 0 W
```

The synchronization is grow-light-only and runs after relevant toggle/load/deserialization lifecycle points.

Implementation:

- `Source/Harmony/GrowLightV31Fixes.cs`
- installed from `Source/Harmony/Init.cs`
- compiled by `Source/LeezGrowLights.csproj`

## Power lifecycle hooks used

The dev11 implementation attaches to the V3.1 power API at several lifecycle points so the value is repaired not only during a live click but also during initialization/reload:

- `PowerConsumerToggle.IsToggled` setter
- `TileEntityPowered.InitializePowerData`
- `PowerItem.SetValuesFromBlock`
- `PowerConsumerToggle.read`

When a live value changes, the implementation requests graph/tile refresh using the available root/tile change methods.

## Radial icon root cause and fix

The colour/brightness command builder created valid `BlockActivationCommand` entries but did not populate their icon member. V3.1 therefore displayed usable but visually blank radial slots.

Dev11 adds a final postfix over the powered-light command array and, for LeeZ grow lights only, fills blank icons:

- Color -> `tool`
- Brightness -> `wrench`

The postfix runs at `Priority.Last` so it observes the final command array after the colour/brightness command-generation postfix.

Implementation:

- `Source/Harmony/GrowLightV31Fixes.cs`
- command creation remains in `Source/Harmony/GrowLightColourPatches.cs`

## Dev11 in-game validation

The dev11 test build was validated in game before promotion. The user confirmed the test build worked perfectly.

Regression checks passed:

1. direct generator -> grow light ON = 10 W;
2. grow light OFF = 0 W;
3. ON again = 10 W;
4. save/reload while OFF remains 0 W;
5. Color radial icon is visible;
6. Brightness radial icon is visible;
7. existing colour/brightness behavior remained functional.

The test branch was then promoted to `dev/colour-system` and compiled again through the normal V3.1 GitHub Actions pipeline.

## Dev11 official release

- Version: `0.7.0.1`
- Runtime label: `v0.7.0-dev11`
- Tag: `leezgrowlights-v0.7.0.1-v31`
- Release name: `LeezGrowLights v0.7.0.1 - V3.1`

The older draft PR #2 was closed without merge because it was based on the wrong/older development line.

## Dev12 brightness-label root cause

`GrowLightColourPatches.ActivationCommandsPostfix` already calculated `offeredColour = Next(selectedColour)` for the Color command, meaning the menu advertised what the click would do.

Brightness differed: the command token was built from the **current brightness** even though the activation handler always advanced to `Next(current)`.

That created a one-step UI mismatch.

## Dev12 brightness-label fix

The command builder now calculates:

```csharp
GrowLightBrightness offeredBrightness = GrowLightBrightnessPalette.Next(brightness);
```

and uses `offeredBrightness` for both:

- an already-existing Brightness command; and
- a newly-created Brightness command.

Correct UI/action relationship:

```text
current Dim         -> hover says Normal      -> click applies Normal
current Normal      -> hover says Bright      -> click applies Bright
current Bright      -> hover says Very Bright -> click applies Very Bright
current Very Bright -> hover says Maximum     -> click applies Maximum
current Maximum     -> hover says Dim         -> click applies Dim
```

This changed only the menu label/token selection. The underlying brightness cycle/state code was already working.

## Dev12 build evidence

- Version: `0.7.0.2`
- Runtime label: `v0.7.0-dev12`
- Validated source commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`
- V3.1 CI run: `31295389819`
- CI result: success

The run completed:

- SteamCMD setup;
- fresh dedicated-server installation;
- V3.1 assembly discovery;
- light-intensity writer probe;
- block metadata capacity probe;
- Release DLL compilation;
- DLL staging;
- artifact upload.

The dev12 label correction was source-reviewed and CI-compiled against V3.1 before release. It had not yet received a separate post-release in-game confirmation at the time this handoff was written.

## Dev12 official release

- Tag: `leezgrowlights-v0.7.0.2-v31`
- Release: `LeezGrowLights v0.7.0.2 - V3.1`
- Release target commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`
- Full ZIP: `LeezGrowLights-v0.7.0.2-dev12-v31.zip`
- DLL: `LeezGrowLights.dll`
- Full ZIP SHA256: `637777f4be2f14b5439b76351cdde3713c0926940283505446d42c85f864946c`
- DLL SHA256: `dfe2046944362b18dac287cf04aa9dfdac843c3dcbd27c62cece9b9e733f4338`

## Files worth reusing in future mods

### Power and radial UI

- `Source/Harmony/GrowLightV31Fixes.cs`
- `Source/Harmony/GrowLightColourPatches.cs`
- `Source/Harmony/GrowLightColourInstaller.cs`
- `Source/Harmony/Init.cs`

### Per-block state and visuals

- `Source/Runtime/GrowLightColourState.cs`
- `Source/Runtime/GrowLightColourPalette.cs`
- `Source/Runtime/GrowLightColourVisual.cs`

### Electrical/growth transitions

- `Source/Harmony/PowerTransitionPatches.cs`
- `Source/Runtime/GrowLightTransitionRescheduler.cs`
- `Source/Runtime/PowerStateResolver.cs`

### API discovery/building

- `.github/workflows/build-leezgrowlights-v31.yml`
- `tools/Probe-7D2D-Api.ps1`
- `tools/Probe-ColourApi.ps1`
- `tools/Probe-ColourStorage.ps1`
- `docs/API_VALIDATION_V3.1.md`
- `docs/V31_MODDING_REFERENCE.md`

## Debugging lessons

- A UI toggle can be correct while a lower-level live resource/accounting value is wrong.
- A radial command can work even when its icon/label metadata is wrong; test action, label and icon separately.
- For cyclic selectors, label the **destination action**, not the current state.
- Preserve tested baselines and isolate fixes before promotion.
- Compile against fresh real game assemblies after every API-sensitive change.
- Keep release evidence (commit, CI run, tag and hashes) in the repo, not only in chat history.