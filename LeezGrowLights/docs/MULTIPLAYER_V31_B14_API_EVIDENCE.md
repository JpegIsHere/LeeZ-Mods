# V3.1.0 b14 multiplayer API evidence

Date captured: 2026-08-08  
Game: 7 Days to Die V3.1.0 (b14)

This document records the exact networking/RPC evidence gathered from the user's installed `Assembly-CSharp.dll`. It exists so future multiplayer work does not fall back to guessed APIs from older 7DTD versions.

## Assembly fingerprint

Installed assembly:

`C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\Assembly-CSharp.dll`

- MVID: `acb580d9-e1ab-497d-a8dc-47e47c1fc300`
- SHA256: `B13862E30D8B28F42B83FE6A36BF074D155A6C43164E7B0797A6E4F77BD7DEA3`
- Length: `11805696` bytes
- Probe generated: `2026-08-08T20:30:41+10:00`
- PowerShell: `5.1.19041.6456`
- CLR: `4.0.30319.42000`

The PowerShell 5.1 reflection probe could not load every newer metadata type (`ReadOnlyMemory`, `ReadOnlySpan`, newer interface metadata), but recovered 7257 loadable game types and enough IL/method information to identify the exact relevant networking path.

## ConnectionManager

`ConnectionManager` inherits `SingletonMonoBehaviour<ConnectionManager>`.

Confirmed properties:

- `IsClient`
- `IsServer`
- `IsSinglePlayer`

Confirmed client-to-server method:

`System.Void SendToServer(NetPackage _package, System.Boolean _flush)`

Other useful confirmed surface:

- `ClientInfo.SendPackage(NetPackage)`
- server-side `ConnectionManager.SendPackage(NetPackage, ...)` broadcast/filter overloads

## NetPackageManager

Confirmed methods/fields relevant to custom package discovery/mapping:

- `TPackage GetPackage()` / generic `GetPackage<TPackage>()`
- `GetPackageId(Type)`
- `AddPackageMapping(int, Type)`
- `knownPackageTypes`
- `SetupBaseMapping`
- `StartClient`
- `StartServer`
- `IdMappingsReceived`
- `ParsePackage`

This strongly matches the game's package-type mapping architecture. Runtime host startup with the custom `NetPackageGrowLightColourRequest` present succeeds; remote send/parse is the next dedicated-server gate.

## NetPackage base class

Confirmed:

- default constructor
- `GetLength()`
- `read(PooledBinaryReader)`
- `write(PooledBinaryWriter)`
- virtual `ProcessPackage(...)`
- package properties including channel/reliability/direction/sender/package ID

The PowerShell probe could not reflect the exact `ProcessPackage` parameter types because of the metadata loader issue. Compilation against the exact b14 assembly subsequently confirmed that this override is valid:

`public override void ProcessPackage(World world, GameManager callbacks)`

## PooledBinaryReader / PooledBinaryWriter compatibility note

The b14 types expose metadata involving newer Span APIs. Building the first candidate under the mod's .NET Framework 4.8 target caused:

`CS0518: Predefined type 'System.ReadOnlySpan<T>' is not defined or imported`

The safe compatibility fix keeps the exact `read(PooledBinaryReader)` / `write(PooledBinaryWriter)` override signatures, but binds primitive integer reads/writes through their `System.IO.BinaryReader` / `System.IO.BinaryWriter` base classes.

Fix commit:

`6874bc41dbed1a5efc5bbced8cbd1d85676a29ee`

## Block replication path

Confirmed exact method:

`GameManager.SetBlocksRPC(List<BlockChangeInfo> _changes, PlatformUserIdentifierAbs _persistentPlayerId)`

Probe IL shows that the method:

1. applies block changes;
2. obtains/sets up a `NetPackageSetBlock`;
3. if server, relays block changes to clients;
4. otherwise sends the package to the server.

### NetPackageSetBlock

Confirmed fields include:

- block change list
- local player that changed
- persistent player identifier

Confirmed methods include:

- `GetLength()`
- `read(...)`
- `write(...)`
- `Setup(PersistentPlayerData, List<BlockChangeInfo>, int)`
- `ProcessPackage(...)`

Probe IL for server processing shows sender validation, relay to clients, and block-change application.

## BlockChangeInfo / BlockValueRef

Confirmed `BlockChangeInfo` constructor forms include:

- `(BlockValueRef, BlockValue)`
- `(BlockValueRef, BlockValue, bool updateLight)`

Confirmed `BlockValueRef` constructors include:

- `BlockValueRef(Vector3i pos)`
- `BlockValueRef(int x, int y, int z)`

## Related block/light APIs

`BlockPoweredLight` exposes relevant methods including:

- `OnBlockActivated`
- `GetBlockActivationCommands`
- `OnBlockEntityTransformAfterActivated`
- `updateLightState`
- `OnBlockValueChanged`

The existing colour visual hooks use the transform/update-light paths. If authoritative replicated metadata reaches a remote client but the live visual does not refresh, `OnBlockValueChanged` is a candidate to investigate next. Do not patch it without first proving the replicated-state vs visual-refresh boundary.

## Current custom packet design

File:

`Source/Runtime/GrowLightColourNetwork.cs`

`NetPackageGrowLightColourRequest` carries only the block `Vector3i`.

The requesting client never dictates the resulting colour. The server reads its own current `BlockValue`, validates the LeeZ target, computes `GrowLightColourPalette.Next(current)`, and persists the change.

This is intentionally server-authoritative and minimizes trust in client-supplied state.

## Evidence boundary

Already proven:

- exact b14 compile of the custom `NetPackage` subclass;
- multiplayer host starts with custom package type present;
- request routing Harmony patches install;
- no immediate custom-package registration/startup failure.

Not yet proven:

- `GetPackage<NetPackageGrowLightColourRequest>()` succeeds on a true remote client after server/client mapping negotiation;
- custom request reaches `ProcessPackage` on a dedicated server;
- server persistence is replicated to the remote client;
- remote client live visual refreshes from the replicated state.

The next test must use a separate dedicated-server process plus a normal game client, even if both run on the same physical PC.
