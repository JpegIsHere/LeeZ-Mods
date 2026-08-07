# Building the LeezGrowLights runtime DLL

Target: **7 Days to Die V3.1.x / .NET Framework 4.8**

## Recommended layout

```text
7 Days To Die/
  7DaysToDie_Data/Managed/Assembly-CSharp.dll
  Mods/
    0_TFP_Harmony/0Harmony.dll
    LeezGrowLights/
      Source/LeezGrowLights.csproj
```

## Build

From a Visual Studio Developer Command Prompt:

```bat
msbuild Mods\LeezGrowLights\Source\LeezGrowLights.csproj /p:Configuration=Release
```

The project writes `LeezGrowLights.dll` into the `LeezGrowLights` mod folder.

Custom paths:

```bat
msbuild LeezGrowLights.csproj /p:Configuration=Release /p:GameManagedPath="D:\7DTD\7DaysToDie_Data\Managed" /p:HarmonyPath="D:\7DTD\Mods\0_TFP_Harmony\0Harmony.dll"
```

## V0.5.1 API status

The successful V3.1 probe is archived as numbered parts under `docs/reference/`; see `docs/reference/README.md`.

It confirms the concrete powered/toggle APIs used by the runtime and confirms `BlockPlantGrowing.GetTickRate()`.

The source has compiled and loaded successfully on the exact V3.1 test installation. It is not yet production-complete because exact mid-stage re-scheduling, dedicated-server validation, and colour persistence/network sync remain pending.
