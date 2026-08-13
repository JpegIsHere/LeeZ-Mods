# LeeZ Grow Lights — next-chat prompt after T1 Test6

We are continuing my **LeeZ Grow Lights** custom 3D model/prefab work for **7 Days to Die V3.1.0 b14**.

Repository: `JpegIsHere/LeeZ-Mods`  
Branch: `dev/colour-system`

Before giving me instructions, read the latest versions of:

1. `LeezGrowLights/docs/GROWLIGHT_MODEL_PROGRESS.md`
2. `LeezGrowLights/docs/GROWLIGHT_MODEL_UNITY_INGAME_CHECKPOINT_2026-08-11.md`
3. `LeezGrowLights/docs/GROWLIGHT_MODEL_NEXT_CHAT_2026-08-11.md`

Treat the **2026-08-11 checkpoint as the latest source of truth** where it supersedes older checkpoint/next-step text.

Important working rules:

- I am a Blender/Unity beginner. Give me small, explicit chunks of instructions. If I get stuck, backtrack and make the steps more granular.
- Keep integration **T1-only**. Do not touch T2-T6.
- Preserve all existing LeezGrowLights **v0.7.0.2/dev12** functionality.
- Do not add materials, textures, UVs, bevels, bolts, branding, vents, detailed diode meshes, emissive work, or cosmetic detail yet.
- Do not move ahead to later regression stages until the current one is confirmed.
- Existing regression order is: collider/hit/select, wiring, 10W ON/0W OFF, toggle, colour, brightness, persistence, crop-light.

Current validated T1 state:

- Exact Unity editor: **2022.3.62f2**.
- Working prefab: `LeeZGrowLight_T1_Prefab`.
- Working bundle: `LeeZGrowLights_T1_Test6.unity3d`.
- T1 XML model path:
  `#@modfolder:Resources/LeeZGrowLights_T1_Test6.unity3d?LeeZGrowLight_T1_Prefab`
- T1 `ModelOffset` is **`0,1,0`** and mounts correctly against the ceiling.
- T2-T6 remain on the vanilla `@:Entities/Electrical/lightPanelLEDPrefab.prefab`.
- Prefab root uses:
  - Tag `T_Block`
  - Layer `Default`
  - Box Collider enabled
  - Is Trigger off
  - Collider Center `0,-0.04,0`
  - Collider Size `0.8,0.08,0.8`
- The crucial fix for zero interaction/hit/damage was replacing the Unity project's `ProjectSettings/TagManager.asset` with the **V1TagManager** version from the 7D2D Templates-and-Utilities package. Manually creating `T_Block` in the clean Unity project was not enough.
- After rebuilding Test6 with that TagManager, **T1 interaction/hit/damage works in game**.
- Placement, rotation, scale, mounting and collider/hit/select are therefore passed.
- Keep Test6 as the known-good T1 prefab baseline.

Start with the **next regression stage: wiring**. Do not alter the prefab or XML before testing the known-good Test6 wiring behavior.
