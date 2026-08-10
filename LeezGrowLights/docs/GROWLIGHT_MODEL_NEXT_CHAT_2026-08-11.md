# LeeZ Grow Lights — next-chat handoff for 2026-08-11

Use this together with:

- `LeezGrowLights/docs/GROWLIGHT_MODEL_PROGRESS.md`
- `LeezGrowLights/docs/GROWLIGHT_MODEL_UNITY_INGAME_CHECKPOINT_2026-08-10.md`

Branch: `dev/colour-system`

Latest Unity/in-game checkpoint commit at end of 2026-08-10 session: `cb7691097721a83034f3fb793356307d7493ed72`

## Copy/paste prompt for a new ChatGPT conversation

We are continuing my **LeeZ Grow Lights** custom 3D model/prefab work for **7 Days to Die V3.1.0 b14**.

Repository: `JpegIsHere/LeeZ-Mods`
Branch: `dev/colour-system`

Before giving me any instructions, read these two repo files and treat them as the source of truth:

1. `LeezGrowLights/docs/GROWLIGHT_MODEL_PROGRESS.md`
2. `LeezGrowLights/docs/GROWLIGHT_MODEL_UNITY_INGAME_CHECKPOINT_2026-08-10.md`

The second file contains the latest Unity and first in-game T1 smoke-test checkpoint. Its commit is `cb7691097721a83034f3fb793356307d7493ed72`.

Important working rules:

- I am a Blender/Unity beginner. Give me **one single explicit step at a time** and wait for me to confirm before continuing.
- Keep the first integration **T1-only**. Do not touch T2-T6 yet.
- Preserve all existing LeezGrowLights v0.7.0.2/dev12 behaviour.
- Do not add materials, textures, UVs, bevels, bolts, branding, vents, detailed diode meshes, emissive work or other cosmetic detail yet.
- Do not assume old A21/V1 Unity tags/layers apply to V3.1 without verification.

Current state at end of last session:

- Exact Unity editor: **2022.3.62f2**.
- T1 FBX import passed scale/orientation/hierarchy/top-centre pivot validation.
- T1 prefab `LeeZGrowLight_T1_Prefab` exists with root Box Collider and child Unity Light.
- T1 asset bundle `LeeZGrowLights_T1_Test.unity3d` was successfully built and is installed locally under the mod `Resources` folder.
- Local test `blocks.xml` points **T1 only** at that custom prefab; T1 still has old vanilla `ModelOffset 0,.545,0`.
- First in-game test successfully loaded the custom square T1 model with correct scale/orientation and visible Unity lighting, but it hangs too far below the ceiling because of that old offset.
- The first smoke test accidentally used an older installed **0.7.0.1/dev11-test1** DLL.
- Before stopping, I backed up the modified local mod folder as `LeezGrowLights_T1_Test_Backup`, replaced the installed `LeezGrowLights.dll` and `ModInfo.xml` with the official **0.7.0.2/dev12** release versions, preserved the custom T1 `blocks.xml` and asset bundle, and verified installed `ModInfo.xml` now says `<Version value="0.7.0.2" />`.
- **The game has not been relaunched since that dev12 replacement.**

Start from the exact next step in the latest checkpoint: launch V3.1.0 b14, stop at the **main menu**, and verify the new runtime log is actually loading **0.7.0.2/dev12** before we change `ModelOffset`. Do not change the offset until that verification passes.
