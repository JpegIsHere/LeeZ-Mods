# Repository manifest

This project lives at `LeezGrowLights/` inside the `LeeZ-Mods` repository. Paths below are relative to the project directory unless otherwise noted.

This manifest is intentionally broader than the runtime package. It identifies the files worth keeping as reference material for future 7 Days to Die mods.

## Current release baseline

- Game: `7 Days to Die V3.1.0 (b14)`
- Mod version: `0.7.0.2`
- Runtime label: `v0.7.0-dev12`
- Source branch: `dev/colour-system`
- Validated dev12 source commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`
- Official tag: `leezgrowlights-v0.7.0.2-v31`
- V3.1 CI run for release DLL: `31295389819`

## Next development phase: custom GrowLight model

- `docs/GROWLIGHT_MODEL_CREATION_HANDOFF.md` — primary starting point for the **GrowLight Model** chat/project.
- `docs/images/GrowLightExample.JPG` — existing visual/design reference image.

The model handoff records the current powered-light XML baseline, the runtime requirements for material tinting and child Unity `Light` components, a beginner Blender -> Unity -> asset-bundle workflow, the exact dev12 regression checklist, V3.1 Unity-version verification guidance, and a ready-to-paste starter prompt for a fresh ChatGPT chat.

## Runtime package files

These are the files needed to assemble the normal drop-in mod together with the compiled DLL:

- `ModInfo.xml`
- `Config/Localization.csv`
- `Config/blocks.xml`
- `Config/progression.xml`
- `Config/recipes.xml`

Generated `LeezGrowLights.dll`/PDB files are not tracked in source control. Official binaries and full ZIPs are kept as GitHub Release assets.

## C# project/build definition

- `Source/LeezGrowLights.csproj`
- `Source/Properties/AssemblyInfo.cs`

The project targets .NET Framework 4.8 and compiles against V3.1 `Assembly-CSharp.dll`, `LogLibrary.dll`, `UnityEngine.CoreModule.dll` and TFP `0Harmony.dll`.

## Harmony/runtime entry points

- `Source/Harmony/Init.cs`
- `Source/Harmony/PatchInstaller.cs`
- `Source/Harmony/PlantGrowthPatches.cs`
- `Source/Harmony/SunlightSubstitutionPatches.cs`
- `Source/Harmony/PowerTransitionPatches.cs`
- `Source/Harmony/BlockRemovalInstaller.cs`
- `Source/Harmony/BlockRemovalPatches.cs`
- `Source/Harmony/GrowLightColourInstaller.cs`
- `Source/Harmony/GrowLightColourPatches.cs`
- `Source/Harmony/GrowLightV31Fixes.cs`

`GrowLightV31Fixes.cs` is especially useful as a future reference for V3.1 powered-consumer synchronization and radial command icon repair.

## Runtime helpers / state systems

- `Source/Runtime/GrowLightScanner.cs`
- `Source/Runtime/GrowthScheduleContext.cs`
- `Source/Runtime/GrowLightTransitionRescheduler.cs`
- `Source/Runtime/TickerScheduleAccessor.cs`
- `Source/Runtime/PowerStateResolver.cs`
- `Source/Runtime/GrowLightColourNetwork.cs`
- `Source/Runtime/GrowLightColourPalette.cs`
- `Source/Runtime/GrowLightColourState.cs`
- `Source/Runtime/GrowLightColourVisual.cs`
- `Source/Runtime/LeezLog.cs`

## Primary project documentation

- `README.md` — current project overview and entry point.
- `CHANGELOG.md` — version/development history.
- `TESTING.md` — current evidence matrix through dev12.
- `TEST_CHECKLIST.md` — compact historical checklist.
- `MIDSTAGE_TESTING.md` — progress-preserving crop transition procedure/evidence.
- `ROADMAP.md` — development roadmap/history.
- `CONTRIBUTING.md`
- `GITHUB_SETUP.md`

## Current V3.1 reference documents

Start with these when creating another mod:

- `docs/GROWLIGHT_MODEL_CREATION_HANDOFF.md` — custom-model next phase and new-chat handoff.
- `docs/V31_MODDING_REFERENCE.md` — reusable V3.1 API/Harmony/build lessons.
- `docs/DEV11_DEV12_REGRESSION_HANDOFF.md` — complete 2026-08-09 power/radial/brightness-label investigation.
- `docs/RELEASE_0.7.0.2.md` — official dev12 commit, CI and asset hashes.
- `docs/API_VALIDATION_V3.1.md` — earlier V3.1 API validation notes.
- `docs/BUILDING.md` — local build information.
- `docs/DECISIONS.md` — architecture decisions.
- `docs/RUNTIME_DESIGN.md` — runtime design notes.
- `docs/STAGE_0_5_STATUS.md` — earlier core-feature evidence.

## Colour / brightness development history

- `docs/COLOUR_SYSTEM.md`
- `docs/COLOUR_DEV7_HANDOFF.md`
- `docs/BRIGHTNESS_DEV8_HANDOFF.md`
- `docs/RELEASE_0.7.0.1.md`
- `docs/RELEASE_0.7.0.2.md`

These files are useful because they show the sequence from API discovery, through persistence and live visual refresh, to the final radial UI/power regressions.

## Multiplayer research / future work

- `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`
- `docs/MULTIPLAYER_V31_B14_API_EVIDENCE.md`
- `docs/MULTIPLAYER_RUNTIME_EVIDENCE_2026-08-08.md`
- `docs/GROWLIGHT_DEDICATED_SERVER_PROMPT.md`

The current release does not claim complete remote-client colour/brightness authoring or full dedicated-server visual synchronization.

## Probe / validation tools

- `tools/Probe-7D2D-Api.ps1`
- `tools/Probe-ColourApi.ps1`
- `tools/Probe-ColourStorage.ps1`
- `tools/Probe-MultiplayerLightSync.ps1`
- `tools/Install-MultiplayerLightSyncTest.ps1`
- `tools/Validate-Stage0-5.ps1`

These tools are worth adapting for future mods rather than guessing game method signatures or metadata behavior.

## Raw reference/evidence material

- `docs/reference/README.md`
- `docs/reference/LeezGrowLights_ApiProbe_V3.1.part01.txt` through `part08.txt`
- `docs/reference/GrowLight_Debug_Report_2026-08-09.txt`
- `docs/images/GrowLightExample.JPG`
- `docs/images/README.md`

The raw debug report preserves the original dev11/dev12 symptoms before they were rewritten into the formal handoff.

## GitHub Actions

At repository root:

- `.github/workflows/build-leezgrowlights-v31.yml`

This is an important reusable reference. It installs a fresh 7DTD dedicated server, discovers real V3.1 references, probes the game API and compiles the DLL.

One-off release-publishing workflows used for dev11/dev12 were intentionally removed after publication so normal future pushes cannot accidentally republish an old release.

## Official release assets

Generated binaries are intentionally stored in GitHub Releases instead of committed to the repository history.

Current official full package:

- release tag: `leezgrowlights-v0.7.0.2-v31`
- ZIP: `LeezGrowLights-v0.7.0.2-dev12-v31.zip`
- ZIP SHA256: `637777f4be2f14b5439b76351cdde3713c0926940283505446d42c85f864946c`
- DLL: `LeezGrowLights.dll`
- DLL SHA256: `dfe2046944362b18dac287cf04aa9dfdac843c3dcbd27c62cece9b9e733f4338`

## Repository policy

The monorepo root owns `.gitignore`, `.gitattributes`, GitHub configuration and workflows.

Keep in Git:

- source code;
- XML/localization;
- build definitions;
- probe scripts;
- architecture/test/release evidence;
- small raw diagnostic/reference text.

Keep out of Git and in Releases/Actions artifacts:

- generated DLL/PDB files;
- packaged release ZIPs;
- copied game assemblies;
- full runtime logs unless a small excerpt is deliberately archived as evidence.

This keeps the repository useful for future mod development without bloating history or redistributing game binaries.
