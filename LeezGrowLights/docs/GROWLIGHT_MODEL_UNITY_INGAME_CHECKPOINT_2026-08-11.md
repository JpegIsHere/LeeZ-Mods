# LeeZ Grow Light T1 Unity / in-game checkpoint — 2026-08-11

This checkpoint supersedes the older Unity/in-game next-step state from 2026-08-10.

## Baseline

- Game: **7 Days to Die V3.1.0 b14**
- Mod runtime baseline: **LeezGrowLights v0.7.0.2 / dev12**
- Validated dev12 source commit: `1449cba3cb663ab0ae3ea78458e6e02e54b69f13`
- Unity editor: **2022.3.62f2** (`7670c08855a9`)
- Integration remains **T1-only**
- T2-T6 must remain unchanged
- No cosmetic/material/detail work yet

## Clean dev12 runtime verification

The clean installed test package was verified to load:

- mod folder `LeezGrowLights`
- `LeezGrowLights.dll`
- `ModInfo` version `0.7.0.2`
- runtime banner `v0.7.0-dev12`

The official dev12 DLL SHA-256 remains:

`dfe2046944362b18dac287cf04aa9dfdac843c3dcbd27c62cece9b9e733f4338`

## T1 placement / offset result

Offset testing established the correct T1 custom-prefab value:

`ModelOffset = 0,1,0`

Observed progression:

- `0,0,0`: fixture appeared around the bottom of the placement block
- `0,-1,0`: moved one block farther downward
- `0,1,0`: moved upward and mounted correctly against the ceiling

Placement, rotation, footprint and scale were reported good.

## Collider / interaction investigation

Initial custom-prefab builds had zero normal block interaction/damage.

With the root Box Collider enabled, projectiles/tools physically collided with the fixture but did not register the expected 7DTD block hit/activation behavior.

A build with the Box Collider genuinely disabled caused shots to pass through the fixture to the block behind. This confirmed the collider was physically present but the 7DTD interaction contract was incomplete.

The prefab root was then configured as:

- Tag: `T_Block`
- Layer: `Default`
- Box Collider: enabled
- Is Trigger: off
- Center: `0,-0.04,0`
- Size: `0.8,0.08,0.8`

Manually adding `T_Block` to the clean Unity project's TagManager did **not** fix the issue.

## Critical fix — V1 TagManager

The original Unity project `ProjectSettings/TagManager.asset` was backed up.

It was then replaced with the `TagManager.asset` from the public 7D2D **V1TagManager** package.

After reopening Unity and rebuilding the prefab as:

`LeeZGrowLights_T1_Test6.unity3d`

the T1 block interaction/hit/damage behavior worked in game.

Therefore, for this tested project, the known-good setup requires the V1 TagManager asset rather than a manually created `T_Block` tag in a stock Unity project.

## Known-good Test6

Prefab:

`LeeZGrowLight_T1_Prefab`

Asset bundle:

`LeeZGrowLights_T1_Test6.unity3d`

SHA-256:

`8f7850e4ce7f314067f173afae5b07d84359e433202a5a71598be585bf489477`

T1 model path:

`#@modfolder:Resources/LeeZGrowLights_T1_Test6.unity3d?LeeZGrowLight_T1_Prefab`

T1 offset:

`0,1,0`

T2-T6 remain on:

`@:Entities/Electrical/lightPanelLEDPrefab.prefab`

## Regression status

Passed:

- custom T1 bundle/prefab load
- T1 scale
- T1 orientation
- T1 top-centre mounting/pivot behavior
- T1 placement/rotation
- T1 ceiling position using `ModelOffset 0,1,0`
- collider / ray hit / selection / normal interaction / damage path after V1 TagManager fix
- structural support behavior was observed; unsupported T1 can fall/break

Still to test, in this order:

1. wiring
2. 10 W ON / 0 W OFF
3. toggle behavior
4. colour selection
5. brightness selection
6. save/reload persistence
7. crop-light behavior

Do not move to T2-T6 or cosmetic model work until the T1 regression is complete.
