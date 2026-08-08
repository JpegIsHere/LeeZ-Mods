# Multiplayer runtime evidence — 2026-08-08

Game: 7 Days to Die V3.1.0 (b14)  
Branch: `dev/colour-system`  
Functional test candidate: `49652867f399057411a107847bdfbc11ae5f2b27`

This file preserves the relevant evidence from local build/runtime testing without committing full multi-megabyte game logs full of unrelated mod/game output.

## Build gate

First multiplayer packet candidate:

`3878d16df91e2f9d695adf145b0b7115a83755d1`

The exact local V3.1 b14 build initially failed in `GrowLightColourNetwork.cs` with `CS0518` errors stating `System.ReadOnlySpan<T>` was not defined/imported.

Cause: b14 `PooledBinaryReader/PooledBinaryWriter` metadata exposes Span-related overloads that .NET Framework 4.8 member resolution cannot fully load.

Fix:

`6874bc41dbed1a5efc5bbced8cbd1d85676a29ee`

Primitive packet reads/writes were bound through `System.IO.BinaryReader/BinaryWriter` while preserving the b14 `PooledBinaryReader/PooledBinaryWriter` override signatures.

Result: exact local rebuild against the installed b14 `Assembly-CSharp.dll` succeeded and the test DLL was installed.

## Multiplayer routing startup evidence

Relevant startup lines from the candidate run:

```text
[LeezGrowLights] Grow-light colour interaction armed on 7 method(s).
[LeezGrowLights] Grow-light colour visual refresh armed on 2 method(s).
[LeezGrowLights] Grow-light multiplayer colour request routing armed on 4 method(s).
[MODS] Initialized code in mod 'LeezGrowLights' in assembly LeezGrowLights
```

This confirms the new network routing installer executed successfully with no Harmony installation failure.

## Colour-menu regression found during safety testing

A local colour test exposed a one-step label mismatch.

Before the fix, the runtime log showed sequences like:

```text
Grow-light colour command exposed ... token 'growlightcolour_blue' (Blue).
Grow-light colour activation recognized: command='growlightcolour_blue'.
Grow-light live visual refresh applied ... as Green / Maximum.
Grow-light colour ... changed Blue -> Green.
```

Then:

```text
... 'growlightcolour_green' (Green)
... changed Green -> Red
```

and:

```text
... 'growlightcolour_red' (Red)
... changed Red -> Purple
```

Diagnosis: the menu token advertised the current state, while the activation path intentionally applied `GrowLightColourPalette.Next(current)`.

Fix commit:

`49652867f399057411a107847bdfbc11ae5f2b27`

The menu now advertises the next colour to be applied. The state-transition logic was not changed.

Post-fix local user validation: **perfect; menu name matches the actual colour**.

## Current local persistence evidence

Post-fix runtime log examples:

```text
Grow-light colour command exposed ... token 'growlightcolour_green' (Green).
Grow-light colour activation recognized: command='growlightcolour_green'.
Grow-light BlockValueRef created through BlockValueRef(Vector3i pos).
Grow-light BlockChangeInfo prepared with BlockValueRef for 579, 53, -639.
Grow-light visual state persisted through WorldBase.SetBlockRPC(BlockChangeInfo _info).
Grow-light live visual refresh applied at 579, 53, -639 as Green / Maximum.
Grow-light colour at 579, 53, -639 changed Blue -> Green.
```

Next activation:

```text
Grow-light colour command exposed ... token 'growlightcolour_red' (Red).
Grow-light colour activation recognized: command='growlightcolour_red'.
Grow-light visual state persisted through WorldBase.SetBlockRPC(BlockChangeInfo _info).
Grow-light live visual refresh applied at 579, 53, -639 as Red / Maximum.
Grow-light colour at 579, 53, -639 changed Green -> Red.
```

Important observation for multiplayer continuation: the current local path is choosing `WorldBase.SetBlockRPC(BlockChangeInfo)` rather than the `GameManager.SetBlocksRPC` fallback.

Do not change this pre-emptively. If dedicated-server testing proves server state changes but remote replication fails, this persistence-path choice becomes a primary investigation point.

## Multiplayer host startup gate

A multiplayer host session was started with the current candidate installed.

Relevant network startup lines:

```text
NET: Starting server protocols
NET: LiteNetLib server started
[Steamworks.NET] NET: Server started
[EOS-P2PS] Server started
```

Later:

```text
[Steamworks.NET] GameServer.LogOn successful
[Steamworks.NET] Trying to create Lobby (visibility: k_ELobbyTypePublic)
[Steamworks.NET] Lobby creation succeeded
[Steamworks.NET] Lobby entered
```

Result: **PASS**.

No `NetPackageGrowLightColourRequest`, unknown-package, package-ID, LeezGrowLights networking, or Harmony exception was found in the host run.

There were unrelated game/mod log warnings/errors (including Discord RPC noise and separate asset-bundle issues). They were not emitted by the grow-light multiplayer code and are not treated as a failed multiplayer-light-sync gate.

## What the host test proves

Proven:

- current DLL loads under b14;
- multiplayer networking starts with the custom `NetPackage` subclass present;
- the network routing Harmony prefix installs;
- there is no immediate package registration/startup crash.

Not proven:

- a true remote client obtains the custom package mapping;
- `NetPackageManager.GetPackage<NetPackageGrowLightColourRequest>()` succeeds remotely;
- `ConnectionManager.SendToServer(...)` carries the request to the server;
- server `ProcessPackage(...)` runs;
- authoritative state propagates back to remote clients;
- remote visual refresh occurs after replication.

## Next runtime gate

Use a dedicated server process plus the normal game client on the same PC.

Both processes must use the exact same LeezGrowLights candidate.

Remote client test:

1. join local dedicated server;
2. open LeeZ grow-light radial menu;
3. click colour once only;
4. collect both logs.

Expected client line:

```text
Remote grow-light colour cycle request sent to server for <position>.
```

Expected server lines:

```text
Server processing grow-light colour cycle request at <position>: <current> -> <next>.
...
Server-authoritative grow-light colour changed at <position>: <current> -> <next>.
```

Stop after proving or disproving that first request/receive/state gate. Do not immediately add visual-replication patches.
