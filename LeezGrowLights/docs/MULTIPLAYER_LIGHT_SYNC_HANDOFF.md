# Multiplayer Light Sync handoff

Date: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)  
Source branch: `dev/colour-system`  
Known-good colour source baseline: `0a1967e94dcd153e8ad8ff40de545b8b9245903b` (`v0.7.0-dev8`)  
`ModInfo.xml` version: `0.7.0.0`

## Starting point

The single-player/local colour feature is now validated end-to-end:

- friendly radial label, e.g. `Grow light colour: Blue`;
- one selection advances the label and visible lamp together;
- all six colours cycle;
- colour persists per placed lamp;
- save/quit/restart restores the saved colour;
- live colour refresh is immediate;
- normal powered-light toggle/wiring state remains independent.

Do not redesign or destabilize this path while adding multiplayer support.

## Current multiplayer limitation

`GrowLightColourPatches.ActivatedPrefix` intentionally rejects a remote world with:

`Remote grow-light colour request ignored until server command routing is enabled.`

This means remote clients cannot currently author colour changes. Dedicated-server colour synchronization has not been validated.

## Multiplayer Light Sync goal

Make colour changes server-authoritative and visible consistently to all connected clients while preserving the existing dev8 single-player/local behaviour.

Required behaviour:

1. A remote client can use the same colour radial command.
2. The client request is routed to the authoritative server rather than mutating local state directly.
3. The server validates that the targeted block is a LeeZ grow light and applies the next colour using the established block-state persistence path.
4. The authoritative block update reaches all clients through the game's normal replication/update mechanisms wherever possible.
5. Each client reapplies the received colour to the live `BlockEntityData`/Unity `Light` components.
6. Save/reload remains correct after multiplayer changes.
7. Vanilla light toggle/wiring/power behaviour remains untouched.

## Existing architecture to preserve

### Interaction

`Source/Harmony/GrowLightColourPatches.cs`

- V3.1 identifies the activation command by `_commandName:String`.
- dev8 uses stable command tokens such as `growlightcolour_blue` with per-colour localization entries.
- The activation handler remains backward-compatible with the earlier friendly text form.
- Vanilla command `light` is not intercepted.

### Persistence

`Source/Runtime/GrowLightColourState.cs`

- colour is stored in `BlockValue.meta2`;
- `0` = legacy/uninitialised => White;
- `1..6` = six colour values;
- persistence uses the V3.1 `BlockChangeInfo` + `BlockValueRef` block-change path;
- electrical state remains in `TileEntityPoweredBlock.isToggled`.

### Visual refresh

`Source/Runtime/GrowLightColourVisual.cs`

- applies `BlockEntityData.SetMaterialColor(colour)`;
- applies colour to child Unity `Light` components;
- caches live block entities by `Vector3i` using weak references;
- dev7 introduced `TryApplyCached(position, updatedValue)` for immediate local refresh.

For multiplayer, do not assume the requesting client's cache is the authoritative propagation mechanism. Prefer authoritative replicated block state first, then use the cache only to refresh the local rendered entity after the replicated state is observed.

## First investigation for the next chat

Before implementing:

1. Read this file, `TESTING.md`, and `docs/COLOUR_DEV7_HANDOFF.md`.
2. Fetch current `GrowLightColourPatches.cs`, `GrowLightColourState.cs`, `GrowLightColourVisual.cs`, and `GrowLightColourInstaller.cs` from the repo.
3. Inspect/probe the exact V3.1 networking/RPC APIs available for client-to-server block interaction or custom packages. Do not guess older-version APIs.
4. Identify the smallest server-authoritative request path that can carry at least block position plus requested colour/next-colour intent.
5. Keep the dev8 single-player code path as a regression baseline.

## Test gates

- Remote client sees the friendly colour menu.
- Remote client colour click reaches server.
- Server changes authoritative `meta2` state.
- Requesting client updates visually immediately or on replicated update.
- Second connected client sees the same colour without manual interaction.
- Server save/restart preserves the selected colour.
- Client reconnect receives/restores the authoritative colour.
- Power toggle/wiring remains normal.
- Growth-speed/coverage/artificial-sunlight behaviour remains unchanged.
- Invalid/non-LeeZ target requests are rejected safely.

## Deferred feature

Player-controlled brightness is intentionally deferred until Multiplayer Light Sync is addressed. The brightness idea remains documented in `ROADMAP.md` and the colour handoff notes.
