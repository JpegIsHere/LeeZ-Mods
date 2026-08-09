# Testing status

Target game build: **7 Days to Die V3.1.0 (b14)**.

Current official release: **LeezGrowLights 0.7.0.2 / dev12**  
Current development branch: **`dev/colour-system`**  
Validated dev12 source commit: **`1449cba3cb663ab0ae3ea78458e6e02e54b69f13`**  
V3.1 CI run for that commit: **`31295389819`**

Legend:

- `PASS (LIVE)` = observed in game.
- `PASS (CI)` = successfully compiled/probed against a fresh V3.1 dedicated-server install.
- `PASS (STATIC)` = source/state logic directly proves the rule but the exact latest-build gameplay scenario has not been separately rerun.
- `PENDING` = not yet validated/complete.

For historical stages and architecture, see the documents in `docs/`. The most useful current summaries are `docs/DEV11_DEV12_REGRESSION_HANDOFF.md` and `docs/V31_MODDING_REFERENCE.md`.

## Build and startup

| Test | Status | Notes |
|---|---|---|
| Fresh V3.1 dedicated-server reference install | PASS (CI) | GitHub Actions installs app 294420 with SteamCMD |
| Managed/Harmony reference discovery | PASS (CI) | Finds V3.1 `Assembly-CSharp.dll` and TFP `0Harmony.dll` |
| Light-intensity writer probe | PASS (CI) | Completed in dev12 run 31295389819 |
| Block metadata capacity probe | PASS (CI) | Completed in dev12 run 31295389819 |
| Release DLL compile | PASS (CI) | dev12 compiled successfully against the real V3.1 references |
| Artifact staging/upload | PASS (CI) | dev12 DLL artifact produced successfully |
| Runtime banner/version | PASS (STATIC) | dev12 source reports `v0.7.0-dev12`; ModInfo/assembly are 0.7.0.2 |

## Core grow-light behavior

| Test | Status | Notes |
|---|---|---|
| Six grow-light tiers T1-T6 | PASS (LIVE) | Existing validated gameplay behavior |
| Vanilla wiring/on-off integration | PASS (LIVE) | LeeZ lights remain powered vanilla-style blocks |
| Enclosed/underground farming | PASS (LIVE) | Active LeeZ light substitutes for sunlight in coverage |
| 5x5 horizontal footprint | PASS (LIVE) | radius 2 |
| Vertical range 1..10 blocks | PASS (LIVE/STATIC) | source enforces inclusive range; live high-range checks performed during earlier stages |
| Lamps at 11+ blocks ignored | PASS (STATIC/LIVE) | boundary rule retained |
| Highest active multiplier wins | PASS (LIVE) | overlapping grow lights do not stack additively |
| T6 4x growth | PASS (LIVE) | validated during stage testing |
| T4 1.5x growth | PASS (LIVE) | validated during stage testing |
| Progress-preserving mid-stage power transitions | PASS (LIVE) | T6 `1x <-> 4x` and T4 `1x <-> 1.5x` validated |
| Save/reload growth continuity | PASS (LIVE) | exercised during earlier development |
| Chunk unload/reload growth continuity | PASS (LIVE) | exercised during earlier development |

See `docs/STAGE_0_5_STATUS.md` and `MIDSTAGE_TESTING.md` for detailed evidence.

## Colour system

| Test | Status | Notes |
|---|---|---|
| Colour radial command visible | PASS (LIVE) | validated during colour development |
| Friendly/localized colour labels | PASS (LIVE) | stable command tokens/localization validated |
| Blue/Green/Red/Purple/White/Yellow cycle | PASS (LIVE) | complete colour cycle validated |
| Per-block colour persistence | PASS (LIVE) | stored in `BlockValue.meta2` |
| Save/quit/restart restores colour | PASS (LIVE) | observed in game |
| Immediate live visual refresh | PASS (LIVE) | no chunk/world reload required |
| Colour preserved while changing brightness | PASS (LIVE) | validated in dev10 brightness testing |
| Brightness preserved while changing colour | PASS (LIVE) | validated in dev10 brightness testing |
| Electrical toggle remains independent | PASS (LIVE) | visual metadata is separate from tile toggle state |
| Color radial icon visible | PASS (LIVE) | dev11 user validation; `tool` icon |

## Brightness system

| Test | Status | Notes |
|---|---|---|
| Brightness radial command visible | PASS (LIVE) | validated in dev10 |
| Brightness levels Dim/Normal/Bright/Very Bright/Maximum | PASS (LIVE) | cycling/visual behavior validated in dev10 |
| Immediate brightness visual refresh | PASS (LIVE) | validated |
| Brightness persistence across save/reload | PASS (LIVE) | validated in dev10 |
| Powered light OFF/ON restores selected brightness | PASS (LIVE) | validated in dev10 |
| Relative brightness across tiers | PASS (LIVE) | LightLOD-controlled intensity behavior validated |
| No progressive intensity compounding | PASS (LIVE) | validated in brightness release testing |
| Brightness radial icon visible | PASS (LIVE) | dev11 user validation; `wrench` icon |
| Menu label advertises the next brightness action | PASS (CI/STATIC) | dev12 uses `offeredBrightness = Next(current)` in both existing/new command paths |
| Latest dev12 label sequence separately rerun in game | PENDING | underlying cycle was already live-validated; only the label correction itself awaits a post-release live confirmation |

Expected dev12 label/action sequence:

```text
current Dim         -> menu Normal      -> click Normal
current Normal      -> menu Bright      -> click Bright
current Bright      -> menu Very Bright -> click Very Bright
current Very Bright -> menu Maximum     -> click Maximum
current Maximum     -> menu Dim         -> click Dim
```

## Dev11 power-draw regression

Original symptom: a grow light connected directly to a generator continued drawing 10 W while the grow light's own toggle was OFF.

| Test | Status | Notes |
|---|---|---|
| Direct generator -> grow light ON = 10 W | PASS (LIVE) | user validated dev11 test build |
| Grow light OFF = 0 W | PASS (LIVE) | user validated |
| ON again restores 10 W | PASS (LIVE) | user validated |
| Save/reload while OFF remains 0 W | PASS (LIVE) | user validated |
| Same fixes leave colour/brightness behavior intact | PASS (LIVE) | user reported test build worked perfectly |

Implementation: `Source/Harmony/GrowLightV31Fixes.cs`.

## Multiplayer

| Test | Status | Notes |
|---|---|---|
| Single-player/host colour authoring | PASS (LIVE) | authoritative local/server path works |
| Single-player/host brightness authoring | PASS (LIVE) | validated in brightness testing |
| Remote-client colour authoring | PENDING | intentionally rejected until authoritative routing is complete |
| Remote-client brightness authoring | PENDING | same limitation |
| Full dedicated-server visual synchronization | PENDING | see multiplayer handoff/evidence docs |

See:

- `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`
- `docs/MULTIPLAYER_V31_B14_API_EVIDENCE.md`
- `docs/MULTIPLAYER_RUNTIME_EVIDENCE_2026-08-08.md`

## Official releases relevant to current source

### 0.7.0.1 / dev11

Live-validated regression release containing:

- 0 W direct-wired OFF-state power fix;
- save/reload power resynchronization;
- Color radial icon;
- Brightness radial icon.

### 0.7.0.2 / dev12

Current official release. Adds the one-step brightness menu-label correction.

- Tag: `leezgrowlights-v0.7.0.2-v31`
- Source commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`
- CI run: `31295389819`
- Full ZIP SHA256: `637777f4be2f14b5439b76351cdde3713c0926940283505446d42c85f864946c`
- DLL SHA256: `dfe2046944362b18dac287cf04aa9dfdac843c3dcbd27c62cece9b9e733f4338`

See `docs/RELEASE_0.7.0.2.md`.

## Remaining release-quality work

- separate in-game confirmation of the dev12 label/action sequence;
- remote-client colour/brightness routing and synchronization;
- long-duration sealed-room crop survival testing under normal play;
- dense-farm / many-light performance testing;
- review diagnostic log volume before a future polished/stable milestone.
