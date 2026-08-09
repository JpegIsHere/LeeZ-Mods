# GrowLight model creation handoff

This document is the starting point for a separate ChatGPT project/chat named **GrowLight Model**. Its purpose is to replace the current vanilla LED-panel visual with an original LeeZ Grow Light model while preserving the already-working V3.1 gameplay/runtime behavior.

## Current project baseline

- Game target: **7 Days to Die V3.1.0 (b14)**
- Mod version: **0.7.0.2 / dev12**
- Source branch: **`dev/colour-system`**
- Validated dev12 source commit: **`1449cba3cb663ab0ae3ea78458e6e02e54b69f13`**
- Official tag: **`leezgrowlights-v0.7.0.2-v31`**
- Main reusable technical reference: `docs/V31_MODDING_REFERENCE.md`
- Regression history: `docs/DEV11_DEV12_REGRESSION_HANDOFF.md`

The gameplay/runtime work should be treated as the working baseline. The model project should initially change only the visual asset path and any model-specific offsets/collider setup required to make the custom prefab fit correctly.

## What the block uses today

All six grow-light tiers currently extend the vanilla powered ceiling light:

```xml
<property name="Extends" value="ceilingLight01_player" />
<property name="Model" value="@:Entities/Electrical/lightPanelLEDPrefab.prefab" />
<property name="ModelOffset" value="0,.545,0" />
<property name="HandleFace" value="Top" />
<property name="RequiredPower" value="10" />
```

The custom model will eventually replace the `Model` value (and possibly `ModelOffset`) while keeping the electrical/tile behavior inherited from `ceilingLight01_player` unless testing proves another change is necessary.

Do **not** casually replace the block class/base with a non-powered block just to display the custom mesh. The existing wiring, toggle, colour, brightness and power-draw fixes depend on the powered-light lifecycle.

## Runtime requirements the custom prefab must preserve

### 1. Renderable material/mesh hierarchy

`GrowLightColourVisual` calls `BlockEntityData.SetMaterialColor(colour)` on the live block entity. The custom prefab therefore needs a normal renderable hierarchy that the game's block material-colour path can affect.

If the custom model later uses a special emissive shader/material that does not respond correctly to this path, update the runtime deliberately rather than silently losing colour selection.

### 2. Unity Light component

The current runtime calls:

```csharp
transform.GetComponentsInChildren<Light>(true)
```

for the grow-light block entity.

Each discovered Unity `Light` receives:

- the selected grow-light colour;
- the selected brightness multiplier;
- LightLOD-safe intensity correction after vanilla updates.

Therefore the first custom prefab should contain **at least one suitable child Unity `Light` component**. Reusing the same overall light-component concept as the current vanilla LED panel is the lowest-risk path.

### 3. Brightness remains relative to the prefab's base intensity

The brightness system does not hard-code a replacement intensity. It captures the current/vanilla intensity as the baseline and applies a multiplier. This means the intensity chosen in the prefab matters: it becomes the base visual output before the player's Dim/Normal/Bright/Very Bright/Maximum setting is applied.

### 4. Ceiling placement/origin

The current block is a ceiling-mounted light with `HandleFace="Top"` and a current model offset of `0,.545,0`.

Do not assume the final custom FBX origin/pivot and scale are correct until the model has been placed in-game. The preferred end state is to build the prefab with a sensible pivot/origin so `ModelOffset` can stay simple (ideally zero or a small documented correction).

### 5. Collider/tag setup

Custom 7DTD Unity-prefab workflows commonly require the appropriate 7DTD tag/layer and collider arrangement on the prefab root. A current community report specifically found that putting the 7DTD tag and collider on the root GameObject and the FBX mesh as a child solved hit/collision problems.

Treat this as a workflow pattern to verify against V3.1 rather than an immutable API contract.

## Recommended creation workflow

### Phase A — concept before Blender

The new chat should first establish what the LeeZ Grow Light should actually look like.

Decide:

- overall shape: square panel, rectangular bar/panel, industrial fixture, etc.;
- approximate footprint and thickness relative to one 7DTD block;
- ceiling mounting method/bracket;
- housing material/style;
- illuminated/emissive area;
- whether T1-T6 share one model or have visual tier differences;
- whether tier differences should be geometry, trim, decals, indicator lights or only item/UI names;
- whether the lamp should look plausible when powered off;
- desired visual language (vanilla-compatible, industrial, hydroponic, sci-fi, improvised, etc.).

A useful first deliverable is an orthographic concept sheet (front/top/side plus perspective) before detailed modeling starts.

### Phase B — Blender

Use Blender to create the mesh unless there is a reason to model directly in Unity.

Beginner-friendly sequence:

1. block out the large shapes;
2. establish scale against a known 7DTD reference rather than guessing;
3. set a deliberate ceiling-mount pivot/origin;
4. apply transforms before export;
5. add bevels/supporting geometry where needed;
6. keep geometry efficient enough for repeated placed blocks;
7. UV unwrap;
8. create/export textures;
9. export FBX for Unity.

Do not spend hours on fine detail before a simple greybox model has successfully appeared in-game at the correct size/orientation.

### Phase C — determine the exact V3.1 Unity version

**Do this before building an asset bundle.**

7DTD custom assets need a Unity editor version compatible with the game version. Older/current community guides explicitly recommend matching the game's Unity engine version and warn that incompatible editor versions can produce unreadable bundles.

For this V3.1 project, do **not** blindly install the Unity version listed for A21 tutorials.

Preferred verification:

1. launch the exact V3.1.0 b14 game;
2. open the game's `Player.log`;
3. find the line beginning with or containing `Initialize engine version:`;
4. record that exact Unity version in this document or a new model-build record;
5. install the matching editor through Unity Hub/archive where possible.

If the game log is supplied to ChatGPT, the new chat should read the exact version rather than guessing.

### Phase D — Unity prefab

Create a dedicated Unity project for the grow-light asset.

The first working prefab should be intentionally simple:

```text
LeeZGrowLightPrefab (root)
  ├─ collider / required 7DTD tag-layer setup
  ├─ GrowLightMesh (FBX child)
  └─ GrowLightLight (Unity Light child)
```

Possible later additions:

- separate lens/emissive mesh;
- mounting bracket;
- indicator LED;
- multiple Light components if genuinely useful;
- LOD meshes/components if required by testing.

Do not add complexity until the basic prefab loads, collides, switches and responds to colour/brightness correctly in game.

### Phase E — 7DTD asset bundle

The long-established 7DTD custom-asset workflow exports Unity prefabs as `.unity3d` asset bundles and loads the named prefab from the mod's `Resources` folder.

A commonly used XML shape is:

```xml
<property name="Shape" value="ModelEntity" />
<property name="Model" value="#@modfolder:Resources/LeeZGrowLights.unity3d?LeeZGrowLightPrefab" />
```

The exact filename and prefab name are case-sensitive in the established community workflow. Confirm the V3.1 syntax on a disposable test block before replacing every tier.

The public `7D2D/Templates-and-Utilities` repository currently contains `MultiPlatformExportAssetBundles.zip` plus tag-manager packages. These are useful starting references, but compatibility of a specific tag-manager package with **V3.1** must be verified before adopting it.

### Phase F — integrate one tier first

Do not change all six blocks at once.

Recommended first test:

1. copy the current `blocks.xml` to a development branch;
2. point only **T1** at the custom bundle/prefab;
3. launch V3.1;
4. spawn/place T1;
5. check scale/orientation/pivot;
6. check collision and block interaction;
7. wire it directly to a generator;
8. confirm ON = configured power and OFF = 0 W;
9. confirm the vanilla power toggle still works;
10. cycle all colours;
11. cycle all brightness levels;
12. confirm the custom Unity Light changes colour/intensity;
13. save/reload and retest;
14. only then migrate T2-T6.

## Model-specific regression checklist

A custom visual must not regress the already-working mod.

### Placement/geometry

- [ ] Correct ceiling orientation.
- [ ] Correct scale.
- [ ] No obvious clipping into the ceiling.
- [ ] Collider can be hit/selected normally.
- [ ] Wire connection/use interaction remains practical.
- [ ] Model does not disappear unexpectedly at normal viewing distance.

### Power

- [ ] Direct generator -> grow light ON uses configured 10 W.
- [ ] Grow-light toggle OFF uses 0 W.
- [ ] ON again restores 10 W.
- [ ] Save/reload while OFF remains 0 W.

### Colour

- [ ] Color radial command/icon remains visible.
- [ ] All six colours still cycle.
- [ ] Visible model/light actually changes colour.
- [ ] Saved colour restores after reload.

### Brightness

- [ ] Brightness radial command/icon remains visible.
- [ ] Hover label advertises the next setting.
- [ ] Dim -> Normal -> Bright -> Very Bright -> Maximum -> Dim remains aligned with the click action.
- [ ] Brightness visibly affects the new Unity Light.
- [ ] Brightness remains independent of colour.
- [ ] Save/reload restores brightness.

### Crop behavior

- [ ] 5x5 coverage remains unchanged.
- [ ] Vertical range remains 1..10 blocks.
- [ ] Underground sunlight substitution still works.
- [ ] Tier multiplier remains correct.

## Useful existing files for the model chat

Start by reading these from this repository:

- `LeezGrowLights/Config/blocks.xml`
- `LeezGrowLights/Source/Runtime/GrowLightColourVisual.cs`
- `LeezGrowLights/Source/Runtime/GrowLightColourPalette.cs`
- `LeezGrowLights/Source/Runtime/GrowLightColourState.cs`
- `LeezGrowLights/Source/Harmony/GrowLightColourPatches.cs`
- `LeezGrowLights/Source/Harmony/GrowLightV31Fixes.cs`
- `LeezGrowLights/docs/V31_MODDING_REFERENCE.md`
- `LeezGrowLights/docs/DEV11_DEV12_REGRESSION_HANDOFF.md`
- `LeezGrowLights/TESTING.md`

## External starting references

These are starting points, not proof that every older instruction applies unchanged to V3.1.

- The Fun Pimps community tutorial: **Creating and Exporting Models from Unity for use in 7D2D**
  - `https://community.thefunpimps.com/threads/creating-and-exporting-models-from-unity-for-use-in-7d2d.4396/`
- 7D2D community templates/utilities repository:
  - `https://github.com/7D2D/Templates-and-Utilities`

The current public templates repository includes `MultiPlatformExportAssetBundles.zip`, `A21TagManager.zip` and `V1TagManager.zip`. Select tooling only after confirming it matches the exact V3.1 engine/project requirements.

## Suggested first message for the new ChatGPT chat

Create a new chat named **GrowLight Model** and paste the following:

> I want you to help me create the custom 3D model/prefab for my LeeZ Grow Lights mod for 7 Days to Die V3.1.0 b14. I am a beginner at Blender/Unity, so guide me one step at a time and do not skip setup details. The working mod source and all technical history are in GitHub repo `JpegIsHere/LeeZ-Mods`, branch `dev/colour-system`, under `LeezGrowLights/`. Start by reading `docs/GROWLIGHT_MODEL_CREATION_HANDOFF.md`, `docs/V31_MODDING_REFERENCE.md`, `Config/blocks.xml`, and `Source/Runtime/GrowLightColourVisual.cs`. Preserve the existing wiring, 0-W-off power fix, colour selection, brightness selection, persistence and crop behavior. Before telling me which Unity editor to install, help me determine the exact Unity engine version used by my V3.1.0 b14 installation from `Player.log`. I want to design the model first, then make a simple greybox in Blender, test scale/pivot in-game, create the Unity prefab with the necessary collider/tag and child Light component, export the asset bundle, integrate only T1 for testing, and expand to T2-T6 only after it works. Ask for screenshots/reference images whenever they would materially help.

## Success definition

The model phase is complete when an original LeeZ Grow Light prefab can replace the vanilla LED panel in-game while all existing dev12 behavior still passes the regression checklist above.
