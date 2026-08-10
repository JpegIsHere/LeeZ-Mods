# LeeZ Grow Light T1 Unity / in-game checkpoint — 2026-08-10

This checkpoint continues `GROWLIGHT_MODEL_PROGRESS.md` for the custom LeeZ Grow Lights 3D model work on branch `dev/colour-system`.

## Baseline that must not regress

- Game: **7 Days to Die V3.1.0 b14**.
- Mod baseline: **LeezGrowLights v0.7.0.2 / dev12**.
- Validated dev12 source commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`.
- Continue **T1-only** until the full T1 model/prefab regression passes.
- Preserve all existing LeeZ grow-light behaviour: wiring, ON/OFF switching, 10 W ON / 0 W OFF, colour selection/persistence, brightness selection/persistence, radial-menu icons, corrected next-brightness labels, crop-light substitution, tier crop multipliers, 5x5 horizontal coverage and 1..10 vertical coverage.
- Do **not** add materials, textures, UVs, bevels, bolts, branding, vents, detailed diode meshes or emissive work yet.
- User is a Blender/Unity beginner. Continue with **one explicit step at a time**.

## Unity editor / project

The exact Unity version confirmed from the user's V3.1.0 b14 runtime log is:

`Unity 2022.3.62f2` (`7670c08855a9`)

Unity Hub was used to install that exact editor version with no optional modules selected.

A minimum test project was created:

- Project: `LeeZGrowLight_T1_Test`
- Template: **3D (Built-In Render Pipeline)**
- Editor: **2022.3.62f2**

No A21/V1 TagManager package was imported. The public 7D2D templates repository did not provide a verified V3.1 TagManager package, so old tag/layer assumptions were deliberately not copied forward.

## T1 FBX import validation in Unity — passed

Imported local validated FBX:

`LeeZGrowLight_T1_Greybox.fbx`

Unity FBX Model import settings were left at the observed defaults:

- Scale Factor: `1`
- Convert Units: checked (`1cm (File) to 0.01m (Unity)` shown)
- Bake Axis Conversion: unchecked

The imported scene root `LeeZGrowLight_T1_Greybox` contained all 10 intended mesh children:

- `Centre_Panel`
- `Frame_Back`
- `Frame_Front`
- `Frame_Left`
- `Frame_Right`
- `Housing_Greybox`
- `LED1_Back`
- `LED1_Front`
- `LED1_Left`
- `LED1_Right`

The imported scene-instance transform showed Unity's FBX conversion values approximately:

- Position: `0, 0.5, 0`
- Rotation: `-89.98, 0, 0`
- Scale: `100, 100, 100`

These import conversion values were **not reset or normalized**.

A Unity 1x1x1 reference Cube was used to verify footprint. Top view confirmed the T1 fixture is approximately **0.80 x 0.80** inside the 1x1 reference block.

A Unity pivot test temporarily changed root X rotation by +20 degrees. The complete fixture hinged around the intended **top-centre mounting point**. Rotation was restored afterward. Unity scale/orientation/hierarchy/pivot validation therefore passed.

Validation scene saved as:

`Assets/Scenes/LeeZGrowLight_T1_Validation`

## Unity prefab — current test state

Created prefab root:

`LeeZGrowLight_T1_Prefab`

Root transform:

- Position `0,0,0`
- Rotation `0,0,0`
- Scale `1,1,1`

The imported FBX was parented beneath the prefab root. Its local Y position was changed from `0.5` to `0` so the validated FBX top-centre pivot aligns with the prefab root origin. A second 20-degree prefab-root X rotation test passed and was restored to 0.

### Root Box Collider

Final intended collider values were verified after correcting one data-entry mistake:

- Center: `0, -0.04, 0`
- Size: `0.8, 0.08, 0.8`

### Child Unity Light

Created child:

`GrowLightLight`

Transform:

- Position: `0, -0.08, 0`
- Rotation: `0,0,0`
- Scale: `1,1,1`

A Unity `Light` component was added because the LeeZ runtime uses child Unity lights. Light Type/Color/Intensity/Range were **not tuned** during this smoke test.

Prefab asset created under:

`Assets/Prefabs/LeeZGrowLight_T1_Prefab`

For this first smoke test the prefab root remained **Untagged / Default layer** because a V3.1-specific required tag/layer contract has not yet been verified.

## Asset-bundle build

The public 7D2D `MultiPlatformExportAssetBundles.cs` editor script was placed under `Assets/Editor`.

It compiled with a yellow obsolete-API warning (`CS0618` for `BuildPipeline.BuildAssetBundle`) but no red compile error. The obsolete warning was deliberately not "fixed" during this compatibility smoke test.

Using **Build Multi-Platform AssetBundle From Selection** on the T1 prefab successfully produced:

`LeeZGrowLights_T1_Test.unity3d`

The local test bundle was copied to:

`LeezGrowLights/Resources/LeeZGrowLights_T1_Test.unity3d`

The `.unity3d` file is currently local to the user's machine and is **not stored in this repository checkpoint**.

## Local T1-only XML smoke-test change

For the first in-game test, only T1's `Model` property in the installed/local test `Config/blocks.xml` was changed from the vanilla light-panel prefab to:

`#@modfolder:Resources/LeeZGrowLights_T1_Test.unity3d?LeeZGrowLight_T1_Prefab`

T1 `ModelOffset` was deliberately left unchanged at:

`0,.545,0`

T2-T6 were untouched.

This local `blocks.xml` smoke-test change has **not** been committed to the repository because the referenced test bundle is also local-only.

## First in-game T1 model smoke test — major pipeline success

The game launched as **V3.1.0 b14** with no startup error naming the custom T1 bundle/prefab.

One T1 was placed on the underside of a normal ceiling in a disposable test world. No wiring or manual colour/brightness changes were made for this first visual placement test.

Observed result:

- The **custom square T1 prefab loaded successfully in game**.
- Orientation was correct: fixture underside faced downward.
- Scale/footprint was sensible and consistent with the intended ~0.80 x 0.80 block footprint.
- Greybox hierarchy/geometry appeared intact.
- The child Unity Light was visibly operating; the fixture produced purple/pink illumination during the smoke test.
- The fixture was **hanging too far below the ceiling**.

The ceiling gap is expected because the custom prefab root is already positioned at the fixture's top mounting surface while the old vanilla `ModelOffset 0,.545,0` is still being applied.

Therefore the likely next offset test is **T1 `ModelOffset = 0,0,0`**, but that change was intentionally deferred until the runtime version mismatch below was corrected.

## Runtime-log discovery: installed mod was stale dev11 during the first smoke test

Runtime log used during the first custom-model test:

`output_log_client__2026-08-10__21-11-26.txt`

Important lines/findings:

- `Initialize engine version: 2022.3.62f2 (7670c08855a9)`
- `Version: V 3.1.0 (b14)`
- The game loaded `LeezGrowLights.dll` from the installed Mods folder.
- Installed `ModInfo.xml` reported **0.7.0.1**.
- Runtime banner reported **v0.7.0-dev11-test1**.
- The runtime still recognized T1 in the world and logged grow-light state transitions.
- No error specifically naming `LeeZGrowLights_T1_Test.unity3d` or `LeeZGrowLight_T1_Prefab` was observed, consistent with the successful visible custom-prefab load.
- The log contained unrelated warnings/errors from the game/other mods; do not claim the full log was clean.

Because the model work must preserve the validated **0.7.0.2/dev12** baseline, the installed stale dev11 runtime had to be corrected before continuing the offset/regression tests.

## Installed test mod reconciled to dev12 — completed before stopping

The official release package was downloaded:

`LeezGrowLights-v0.7.0.2-dev12-v31.zip`

Before replacement, the entire installed/local modified mod folder was copied outside the game's Mods folder as:

`LeezGrowLights_T1_Test_Backup`

This preserves the current local T1 bundle and smoke-test XML edit.

From the official dev12 release, only the runtime/version files were replaced in the installed `LeezGrowLights` test folder:

- `LeezGrowLights.dll`
- `ModInfo.xml`

The custom local `Config/blocks.xml` and `Resources/LeeZGrowLights_T1_Test.unity3d` were deliberately preserved.

The installed `ModInfo.xml` was then opened and verified to contain:

`<Version value="0.7.0.2" />`

Repository comparison of official dev11 (`f0306061e01ddd8327d359c377a40570db465053`) to official dev12 (`1449cba3cb663ab0ae3ea78458e6e02e54b69f13`) showed no `Config/blocks.xml` change between those release commits, supporting preservation of the local T1 XML smoke-test edit while replacing the DLL/ModInfo.

**The game was not relaunched after this dev12 replacement before the session ended.**

## Exact next step for the next session

Continue one explicit step at a time.

1. Launch **7 Days to Die V3.1.0 b14** with the now-reconciled installed test copy.
2. Stop at the **main menu** first.
3. Verify the new runtime log reports **LeezGrowLights 0.7.0.2 / dev12** before changing any model offset.
4. Only after that verification, close the game completely and change **T1 only** `ModelOffset` from `0,.545,0` to an initial test value of **`0,0,0`**.
5. Relaunch and place one T1 under a ceiling to check flush/near-flush mounting.

Do not modify T2-T6. Do not add materials/detail work yet.

After the offset is corrected, continue the T1 regression in controlled stages: collider/hit/select behavior, wiring, 10 W ON / 0 W OFF, toggle behavior, colour, brightness, save/reload persistence, and crop-light behavior. If results become ambiguous, remember the user's test world contains many other mods and controlled isolation may be needed later.
