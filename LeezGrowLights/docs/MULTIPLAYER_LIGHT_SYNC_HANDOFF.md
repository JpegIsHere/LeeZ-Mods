# Multiplayer Light Sync handoff

Date: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)  
Source branch: `dev/colour-system`  
Protected known-good colour reference: `0a1967e94dcd153e8ad8ff40de545b8b9245903b` (`v0.7.0-dev8`)  
Current tested multiplayer/code candidate: `49652867f399057411a107847bdfbc11ae5f2b27`  
Installer pin commit: `a2a056dc58e767e20b3a9f9977800849422b624c`  
`ModInfo.xml` version: `0.7.0.0`

## Read this first

Continue with **Multiplayer Light Sync**. Do not redesign the colour system or destabilize the validated local behaviour.

Also read:

1. `TESTING.md`
2. `docs/COLOUR_DEV7_HANDOFF.md`
3. `docs/MULTIPLAYER_V31_B14_API_EVIDENCE.md`
4. `docs/MULTIPLAYER_RUNTIME_EVIDENCE_2026-08-08.md`
5. `docs/GROWLIGHT_DEDICATED_SERVER_PROMPT.md`

## Current status

### Local/single-player colour: PASS

The local colour feature is validated end-to-end on V3.1.0 b14:

- radial colour entry is visible;
- the radial label now names the **next colour that will actually be applied**;
- one selection advances the visible lamp and persisted state together;
- all six colours remain available;
- live refresh is immediate;
- save/reload behaviour from the colour baseline remains protected;
- vanilla light toggle/wiring behaviour remains separate.

A regression was found during multiplayer-candidate testing: the menu was advertising the current colour while the activation handler intentionally applied `GrowLightColourPalette.Next(current)`. Example: menu `Blue` caused `Blue -> Green`. This was fixed in `49652867f399057411a107847bdfbc11ae5f2b27` by advertising the next colour in the menu while leaving the actual state transition unchanged.

### Multiplayer host startup: PASS

The current candidate was loaded in a multiplayer-host session on V3.1.0 b14. The log showed:

- `LeezGrowLights` loaded successfully;
- colour interaction hooks armed;
- colour visual refresh hooks armed;
- `Grow-light multiplayer colour request routing armed on 4 method(s).`;
- `NET: Starting server protocols`;
- LiteNetLib server started;
- Steam server started;
- EOS P2P server started;
- Steam GameServer login succeeded;
- public lobby creation succeeded.

No grow-light `NetPackage`, package-ID, Harmony, or multiplayer-routing exception was found in that host run.

This proves that the custom packet class can exist in the assembly and the host can start networking without an immediate registration/startup failure. It does **not** yet prove a remote client can send the custom package successfully.

## Exact V3.1.0 b14 API evidence

The local probe was run against the user's installed:

`7DaysToDie_Data/Managed/Assembly-CSharp.dll`

Fingerprint:

- MVID: `acb580d9-e1ab-497d-a8dc-47e47c1fc300`
- SHA256: `B13862E30D8B28F42B83FE6A36BF074D155A6C43164E7B0797A6E4F77BD7DEA3`
- assembly length: `11805696`

Important exact findings:

- `ConnectionManager` exposes `IsClient`, `IsServer`, `IsSinglePlayer`.
- Client send method: `ConnectionManager.SendToServer(NetPackage _package, bool _flush)`.
- `ClientInfo.SendPackage(NetPackage)` exists.
- `NetPackageManager.GetPackage<TPackage>()` exists.
- `NetPackageManager` contains package mapping/startup machinery including `GetPackageId`, `AddPackageMapping`, `knownPackageTypes`, `SetupBaseMapping`, `StartClient`, `StartServer`, `IdMappingsReceived`, and `ParsePackage`.
- `NetPackage` has default constructor, `read(PooledBinaryReader)`, `write(PooledBinaryWriter)`, `GetLength()`, and `ProcessPackage(...)`.
- Exact compilation against b14 confirmed the override shape used by the candidate: `ProcessPackage(World world, GameManager callbacks)`.
- `GameManager.SetBlocksRPC(List<BlockChangeInfo>, PlatformUserIdentifierAbs)` exists.
- Vanilla `SetBlocksRPC` changes local blocks, creates/uses `NetPackageSetBlock`, broadcasts when server, and sends to server when client.
- `NetPackageSetBlock` validates sender identity on the server, relays to clients, and applies block changes.
- `BlockChangeInfo` constructors include forms taking `BlockValueRef` + `BlockValue`.
- `BlockValueRef(Vector3i)` exists.

The original PowerShell reflection probe partially failed on newer `ReadOnlySpan`/interface metadata because PowerShell 5.1/.NET Framework could not load every metadata type, but it recovered 7257 loadable game types and enough IL/API evidence to identify the networking path.

## Current multiplayer implementation

File: `Source/Runtime/GrowLightColourNetwork.cs`

### Custom request packet

`public sealed class NetPackageGrowLightColourRequest : NetPackage`

Packet payload: **block position only**.

The client does not send a desired colour. The server:

1. receives the position;
2. requires an authoritative server connection;
3. reads the current block from the server world;
4. validates it is a LeeZ grow light using `GrowLightScanner.TryGetGrowLightCoverage`;
5. reads the authoritative current colour;
6. computes `GrowLightColourPalette.Next(current)`;
7. persists through `GrowLightColourState.TrySet`;
8. optionally refreshes the listen-server cached visual;
9. logs the authoritative change.

Invalid/non-LeeZ targets are rejected.

### Client routing

A separate high-priority Harmony prefix is installed for `OnBlockActivated` methods.

For a remote world and colour command only, it:

1. resolves the block position;
2. obtains `NetPackageManager.GetPackage<NetPackageGrowLightColourRequest>()`;
3. calls `ConnectionManager.SendToServer(package, true)`;
4. logs `Remote grow-light colour cycle request sent to server for ...`.

The existing local activation prefix remains in place and still suppresses remote local mutation. This is intentional: the new prefix sends first, then the old path prevents optimistic client-side state changes. Brightness remote routing remains deferred/unimplemented.

### b14 compiler compatibility fix

The first local build failed because member resolution over `PooledBinaryReader/PooledBinaryWriter` encountered b14's `ReadOnlySpan`-related metadata while the mod targets .NET Framework 4.8.

Fix commit: `6874bc41dbed1a5efc5bbced8cbd1d85676a29ee`.

The packet still overrides the exact b14 `read`/`write` methods, but primitive integer serialization binds through their `System.IO.BinaryReader` / `System.IO.BinaryWriter` base types. The corrected candidate compiled successfully against the user's installed b14 assemblies.

## Current colour/brightness storage caveat

The branch contains later dev9 brightness work after the protected dev8 colour baseline.

Current `GrowLightColourState.cs` stores a combined colour/brightness state in values `1..30`:

- low four bits in `BlockValue.meta2`;
- fifth bit in `BlockValue.meta3` raw bit `0x00200000`;
- legacy `1..6` still represent the original six colours at Normal brightness.

Do **not** make brightness a prerequisite for Multiplayer Light Sync. The current multiplayer work is colour-only. Treat `0a1967...` as the behavioural reference for colour.

## Persistence path to watch carefully

`GrowLightColourState.TrySet` currently tries persistence in this order:

1. a compatible direct `SetBlockRPC` path;
2. `SetBlockRPC(BlockChangeInfo)`;
3. fallback `GameManager.SetBlocksRPC(...)`.

Local logs on the current build show:

`Grow-light visual state persisted through WorldBase.SetBlockRPC(BlockChangeInfo _info).`

The exact b14 probe independently confirms `GameManager.SetBlocksRPC` + `NetPackageSetBlock` is a normal authoritative replication path. If the dedicated-server test proves the server changes state but remote clients do not receive it, inspect which persistence path the server used before changing code. Do not guess.

## Next gate: same-PC dedicated server + normal client

A second physical PC is not required.

Use a **dedicated server process on the same PC** plus the normal 7DTD game client. This creates a real remote-client world and exercises the exact `SendToServer` path.

Keep both server and client on the exact same LeezGrowLights build.

### First dedicated-server test only

Do not test brightness yet.

1. Start a local dedicated server with the current candidate installed.
2. Start the normal game client with the same candidate.
3. Connect the client to the local dedicated server.
4. On the remote client, open one LeeZ grow light radial menu.
5. Click the colour entry **exactly once**.
6. Capture both client and server logs around that click.

Expected client evidence:

`Remote grow-light colour cycle request sent to server for <position>.`

The existing remote-suppression prefix may also log:

`Remote grow-light colour request ignored until server command routing is enabled.`

That second warning is expected in this candidate because the high-priority network prefix sends first and the older prefix then prevents local mutation.

Expected server evidence:

- `Server processing grow-light colour cycle request at <position>: <current> -> <next>.`
- a persistence-path log from `GrowLightColourState`;
- `Server-authoritative grow-light colour changed at <position>: <current> -> <next>.`

### Stop after this gate

Do not immediately patch client visual replication if the click reaches the server. First establish separately:

A. client request sent;  
B. server package received;  
C. server authoritative state changed;  
D. normal replication reached client;  
E. remote live visual refreshed.

If A fails, inspect custom package mapping/GetPackage/SendToServer.

If A succeeds but B fails, inspect package registration/mapping and packet parsing.

If B succeeds but C fails, inspect target validation and persistence.

If C succeeds but D fails, inspect the server persistence path. The confirmed `GameManager.SetBlocksRPC`/`NetPackageSetBlock` path is the strongest known b14 replication reference.

If D succeeds but E fails, investigate the client callback after replicated block metadata is observed. `BlockPoweredLight.OnBlockValueChanged` exists in b14 and is a candidate hook, but do not patch it blindly; first confirm the callback/update flow from logs or IL.

## Remaining acceptance gates

- Remote client sees the friendly colour menu.
- Remote client colour click reaches server.
- Server validates LeeZ target and changes authoritative colour state.
- Requesting client receives the replicated state.
- Requesting client live visual matches the state.
- A second client sees the same colour without interacting.
- Server save/restart preserves the selected colour.
- Client reconnect restores authoritative colour.
- Vanilla power toggle/wiring remains normal.
- Crop growth/coverage/artificial-sunlight behaviour remains unchanged.
- Invalid/non-LeeZ request is rejected safely.

## Important commits from this session

- `96e0fcf21f37d014e83b9b6c52ecb21d211516e2` — add V3.1 multiplayer API probe.
- `3878d16df91e2f9d695adf145b0b7115a83755d1` — initial colour-only multiplayer request routing candidate.
- `187f611c00ef9a060ee57aee404e4bc52e3fd4a5` — add local exact-b14 build/install helper.
- `6874bc41dbed1a5efc5bbced8cbd1d85676a29ee` — fix b14 packet serialization compile compatibility.
- `49652867f399057411a107847bdfbc11ae5f2b27` — fix colour radial label to advertise the colour actually applied.
- `a2a056dc58e767e20b3a9f9977800849422b624c` — pin installer to the tested menu-fix candidate.

## Safety rules for continuation

- Work one testable gate at a time.
- Do not rewrite the protected `0a1967...` baseline.
- Do not force-push or rewrite branch history.
- Re-fetch the branch before writes because it has advanced during development.
- Do not implement multiplayer brightness until colour sync is proven.
- Do not add optimistic client colour state; authoritative replicated state comes first.
- Do not assume old 7DTD networking APIs; use the exact V3.1.0 b14 evidence above.
