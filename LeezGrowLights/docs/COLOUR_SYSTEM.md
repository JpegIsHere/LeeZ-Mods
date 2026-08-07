# Colour system development

Development branch: `dev/colour-system`

## Goal

Add per-placed-light colour selection without changing grow-light tier, power draw, coverage, or growth-speed behaviour.

Allowed colours, matching the existing XML metadata:

- Blue
- Green
- Red
- Purple
- White
- Yellow

Default colour: **White**.

## Current foundation

The branch now contains:

- `Runtime/GrowLightColourPalette.cs`
  - canonical six-colour enum/order;
  - White default;
  - parsing helper;
  - colour cycling helper;
  - Unity `Color` mapping for the eventual runtime tint.
- `Runtime/GrowLightColourState.cs`
  - thread-safe per-block-position colour state abstraction;
  - deliberately memory-backed only until the V3.1 persistence/network API is confirmed.
- `tools/Probe-ColourApi.ps1`
  - reflection probe targeting V3.1 `Assembly-CSharp.dll`;
  - reports activation-command, block interaction, tile-entity persistence, RPC/network, prefab/light and colour-related members.

The new runtime files are included in `LeezGrowLights.csproj` but are not yet connected to player interaction or visual rendering. Existing growth/power behaviour is unchanged.

## Why probe before wiring the feature

Colour selection must preserve the existing electrical tile entity and wiring, survive save/reload, and remain server-authoritative in multiplayer. Encoding colour by replacing the powered block or by guessing at unused block metadata could destroy wiring or corrupt unrelated block state.

The next implementation step therefore depends on exact V3.1.0 (b14) APIs from the installed game assembly rather than assumptions from older versions.

## Probe command

From PowerShell:

```powershell
cd "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods\LeezGrowLights\tools"
.\Probe-ColourApi.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
```

Default output:

```text
LeezGrowLights_ColourApiProbe.txt
```

## Next implementation gates

After the probe output is reviewed:

1. Add a player activation command/UI for colour selection.
2. Bind colour state to a V3.1 persistent placed-light data path.
3. Synchronize server-authoritative colour changes to clients.
4. Tint the placed panel/light component at runtime.
5. Reapply tint after chunk reload, save/reload and reconnect.
6. Add live tests for all six colours, wiring preservation and multiplayer synchronization.
