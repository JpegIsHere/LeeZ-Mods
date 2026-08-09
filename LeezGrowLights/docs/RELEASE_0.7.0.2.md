# LeezGrowLights 0.7.0.2 / dev12 release record

Official V3.1 release record for the brightness radial-menu label correction.

## Release identity

- Game target: **7 Days to Die V3.1.0 (b14)**
- ModInfo version: **0.7.0.2**
- Runtime label: **v0.7.0-dev12**
- Release tag: **`leezgrowlights-v0.7.0.2-v31`**
- Release name: **LeezGrowLights v0.7.0.2 - V3.1**
- Validated source commit: **`1449cba3cb663ab0ae3ea78458e6e02e54b69f13`**
- V3.1 build workflow run: **`31295389819`**

## What changed from 0.7.0.1

The Brightness radial-menu command now displays the **next brightness level that clicking will apply** instead of displaying the current level.

Expected cycle:

```text
Dim         -> Normal
Normal      -> Bright
Bright      -> Very Bright
Very Bright -> Maximum
Maximum     -> Dim
```

No brightness multiplier values, persistence encoding, colour behavior, crop behavior or power rules were intentionally changed by dev12.

## Retained validated dev11 fixes

- directly wired grow lights use **0 W** while their own toggle is OFF;
- switching ON restores the configured 10 W load;
- OFF-state live power is resynchronized after save/reload initialization;
- Color radial command uses a visible `tool` icon;
- Brightness radial command uses a visible `wrench` icon.

These dev11 behaviors were confirmed in game before dev11 was promoted.

## Build validation

GitHub Actions run `31295389819` completed successfully against a fresh V3.1 dedicated-server installation.

Successful stages included:

- SteamCMD install;
- 7DTD Dedicated Server install;
- V3.1 managed/Harmony reference discovery;
- light-intensity writer probe;
- block metadata capacity probe;
- Release DLL compile;
- artifact staging/upload.

Dev12's menu-label change was source-verified and CI-compiled before release. A separate dev12 post-release gameplay confirmation was not yet recorded when this file was written.

## Official assets

### Full mod ZIP

`LeezGrowLights-v0.7.0.2-dev12-v31.zip`

SHA256:

`637777f4be2f14b5439b76351cdde3713c0926940283505446d42c85f864946c`

### DLL

`LeezGrowLights.dll`

SHA256:

`dfe2046944362b18dac287cf04aa9dfdac843c3dcbd27c62cece9b9e733f4338`

## Expected drop-in layout

```text
Mods/
  LeezGrowLights/
    ModInfo.xml
    LeezGrowLights.dll
    LeezGrowLights.pdb   (may be included in full package)
    Config/
      blocks.xml
      Localization.csv
      progression.xml
      recipes.xml
```

The source repository intentionally does not track generated DLL/PDB files. Official compiled binaries are stored as GitHub Release assets, while all source, XML, probes, workflows and development evidence remain version-controlled.

## Related documentation

- `CHANGELOG.md`
- `TESTING.md`
- `docs/DEV11_DEV12_REGRESSION_HANDOFF.md`
- `docs/V31_MODDING_REFERENCE.md`
- `docs/RELEASE_0.7.0.1.md`
- `docs/API_VALIDATION_V3.1.md`
