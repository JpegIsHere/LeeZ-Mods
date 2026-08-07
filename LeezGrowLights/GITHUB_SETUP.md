# LeeZ-Mods repository layout

This project is published inside the `LeeZ-Mods` monorepo under the `LeezGrowLights/` directory.

The mod folder itself remains installable: copy `LeezGrowLights/` from the repository into the game's `Mods/` directory, then build `LeezGrowLights/Source/LeezGrowLights.csproj` if a compiled DLL is not included in a release package.

Generated DLL/PDB files, local game assemblies and runtime logs are intentionally excluded from source control. A project license has not yet been selected.
