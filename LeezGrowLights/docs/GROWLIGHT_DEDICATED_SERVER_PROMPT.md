# Growlight Dedicated Server

Copy everything below into a new ChatGPT chat.

---

I am continuing development of GitHub repo `JpegIsHere/LeeZ-Mods`, mod `LeezGrowLights`, branch `dev/colour-system`, for **7 Days to Die V3.1.0 b14**.

The immediate task is **Growlight Dedicated Server**: continue Multiplayer Light Sync using a **dedicated server process plus my normal game client on the same physical PC** so we can create a true remote-client test without needing a second computer.

## Read these repo files first

Read these in order before changing code:

1. `LeezGrowLights/docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`
2. `LeezGrowLights/docs/MULTIPLAYER_V31_B14_API_EVIDENCE.md`
3. `LeezGrowLights/docs/MULTIPLAYER_RUNTIME_EVIDENCE_2026-08-08.md`
4. `LeezGrowLights/TESTING.md`
5. `LeezGrowLights/docs/COLOUR_DEV7_HANDOFF.md`

Then fetch the current versions of:

- `LeezGrowLights/Source/Runtime/GrowLightColourNetwork.cs`
- `LeezGrowLights/Source/Runtime/GrowLightColourState.cs`
- `LeezGrowLights/Source/Runtime/GrowLightColourVisual.cs`
- `LeezGrowLights/Source/Harmony/GrowLightColourPatches.cs`
- `LeezGrowLights/tools/Install-MultiplayerLightSyncTest.ps1`

## Protected behavioural baseline

Treat commit:

`0a1967e94dcd153e8ad8ff40de545b8b9245903b`

as the known-good **v0.7.0-dev8 colour baseline**.

Do not redesign or destabilize its working single-player colour behaviour.

The branch contains later dev9 brightness work. **Brightness is not a prerequisite for this task and must not be added to multiplayer routing yet.** Work on multiplayer colour only.

## Current tested candidate

Functional multiplayer/menu candidate:

`49652867f399057411a107847bdfbc11ae5f2b27`

Installer pin commit:

`a2a056dc58e767e20b3a9f9977800849422b624c`

The branch will have newer documentation commits after these, so fetch the live branch before writing anything.

## What has already been proven

Do not repeat these investigations unless new evidence requires it.

### Exact b14 API probe

The user's exact installed `Assembly-CSharp.dll` was probed.

Fingerprint:

- MVID `acb580d9-e1ab-497d-a8dc-47e47c1fc300`
- SHA256 `B13862E30D8B28F42B83FE6A36BF074D155A6C43164E7B0797A6E4F77BD7DEA3`

Confirmed exact b14 APIs include:

- `ConnectionManager.IsClient`
- `ConnectionManager.IsServer`
- `ConnectionManager.IsSinglePlayer`
- `ConnectionManager.SendToServer(NetPackage _package, bool _flush)`
- `NetPackageManager.GetPackage<TPackage>()`
- package mapping machinery (`GetPackageId`, `AddPackageMapping`, `knownPackageTypes`, startup/mapping functions)
- valid custom override `ProcessPackage(World world, GameManager callbacks)` confirmed by exact local compilation
- `GameManager.SetBlocksRPC(List<BlockChangeInfo>, PlatformUserIdentifierAbs)`
- `NetPackageSetBlock` server relay/change path
- `BlockChangeInfo(BlockValueRef, BlockValue)` forms
- `BlockValueRef(Vector3i)`

Do not guess older-version networking APIs.

### Current multiplayer implementation

`NetPackageGrowLightColourRequest : NetPackage` exists.

The client sends **only the grow-light block position**.

The authoritative server:

1. reads the current server `BlockValue`;
2. validates the target is a LeeZ grow light;
3. gets the current authoritative colour;
4. computes `GrowLightColourPalette.Next(current)`;
5. persists via `GrowLightColourState.TrySet`;
6. logs the authoritative change.

A high-priority remote activation prefix sends the package before the existing local colour prefix rejects remote local mutation. This is deliberate: **do not add optimistic client state**.

### Exact-build compatibility issue already fixed

The first custom packet build encountered b14 `ReadOnlySpan` metadata issues under .NET Framework 4.8.

Fix commit:

`6874bc41dbed1a5efc5bbced8cbd1d85676a29ee`

Packet primitive reads/writes now bind through `System.IO.BinaryReader/BinaryWriter` while keeping the exact b14 override signatures.

The candidate now compiles successfully against the user's exact installed V3.1 b14 game assemblies.

### Single-player regression found and fixed

During testing, the radial menu said the current colour while activation applied the next colour, so for example:

- selecting displayed Blue gave Green;
- displayed Green gave Red;
- displayed Red gave Purple.

Fix commit:

`49652867f399057411a107847bdfbc11ae5f2b27`

The radial command now advertises the **next colour that will actually be applied**.

The user retested this and reported it is **perfect**.

Do not undo this fix.

### Multiplayer host startup gate already passed

A normal multiplayer-host session started successfully with the candidate.

Runtime evidence includes:

- colour interaction hooks armed;
- colour visual hooks armed;
- `Grow-light multiplayer colour request routing armed on 4 method(s).`;
- `NET: Starting server protocols`;
- LiteNetLib server started;
- Steam server started;
- EOS P2P server started;
- Steam GameServer login successful;
- public lobby creation successful.

No grow-light custom package / package-ID / Harmony / multiplayer-routing exception was found.

This proves startup only. It does **not** prove a real remote client's custom package maps/sends/processes successfully.

## Important persistence observation

Current local logs show colour state persistence choosing:

`WorldBase.SetBlockRPC(BlockChangeInfo _info)`

The exact b14 probe independently confirms `GameManager.SetBlocksRPC` + `NetPackageSetBlock` is a normal server/client replication path.

Do not pre-emptively rewrite persistence. First run the dedicated-server request gate. If server state changes but the remote client does not receive the update, then investigate which server persistence path ran and whether it actually broadcasts.

## Immediate task — do NOT write more networking code first

Set up and run the **first same-PC dedicated-server + client test**.

My normal game installation is under:

`C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die`

The working test mod is under:

`C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods\LeezGrowLights`

I need extremely simple, exact, step-by-step instructions. Give me one small stage at a time and wait for my output before moving to the next stage.

### Goal of the first dedicated-server test

Run a dedicated server process and the normal game client as separate peers on the same PC, both with the exact same `LeezGrowLights` build.

Then from the **remote game client**:

1. join the local dedicated server;
2. find a LeeZ grow light;
3. open the radial menu;
4. click the colour command **once only**;
5. collect both client and server logs.

Do not test brightness.

## Expected first-gate logs

Client should ideally log:

`Remote grow-light colour cycle request sent to server for <position>.`

The older remote suppression prefix may also log:

`Remote grow-light colour request ignored until server command routing is enabled.`

That warning is expected for this candidate if the high-priority prefix sent the packet first.

Server should ideally log:

`Server processing grow-light colour cycle request at <position>: <current> -> <next>.`

then a persistence-path line, then:

`Server-authoritative grow-light colour changed at <position>: <current> -> <next>.`

## Work one gate at a time

For this next session, separate these questions:

A. Can the dedicated server and normal client run simultaneously and connect locally?  
B. Does `GetPackage<NetPackageGrowLightColourRequest>()` work on the real remote client?  
C. Does the client log that the request was sent?  
D. Does the dedicated server receive/process it?  
E. Does authoritative server state change?  
F. Does the replicated state reach the client?  
G. Does the remote live visual refresh?

Do not skip directly to G.

If A fails, solve only dedicated-server/local-client setup first.

If B/C fails, inspect package mapping/GetPackage/SendToServer using exact b14 evidence; do not guess APIs.

If C succeeds but D fails, inspect package mapping/parsing on the server.

If D succeeds but E fails, inspect target validation/persistence.

If E succeeds but F fails, inspect the actual server persistence path. The confirmed `GameManager.SetBlocksRPC`/`NetPackageSetBlock` path is the strongest b14 reference.

If F succeeds but G fails, investigate the client-side block-value change/update callback. `BlockPoweredLight.OnBlockValueChanged` exists in b14 and is a possible hook, but do not patch it blindly.

## Safety constraints

- Preserve working single-player colour behaviour.
- Do not implement multiplayer brightness yet.
- Do not send desired colour from client; server computes next colour from authoritative state.
- Do not add optimistic client colour updates.
- Validate remote target is a LeeZ grow light.
- Keep vanilla power/toggle/wiring behaviour untouched.
- Keep crop growth/coverage/artificial-sunlight behaviour untouched.
- Work one testable step at a time.
- Make exact test installers/builds when runtime validation is required.
- Re-fetch `dev/colour-system` before every repo write; the branch has moved concurrently before.
- Never force-push or rewrite history.

Start by reading the listed repo docs/source and then help me launch the same-PC dedicated server in the safest/simple way. Do not make more code changes before the dedicated-server/client connection gate requires them.

---
