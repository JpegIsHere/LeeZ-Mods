# Repository manifest

This project lives at `LeezGrowLights/` inside the `LeeZ-Mods` repository. Paths below are relative to the project directory.

## Runtime / mod files

- `ModInfo.xml`
- `Config/Localization.csv`
- `Config/blocks.xml`
- `Config/progression.xml`
- `Config/recipes.xml`
- `Source/LeezGrowLights.csproj`
- `Source/Harmony/Init.cs`
- `Source/Harmony/PatchInstaller.cs`
- `Source/Harmony/PlantGrowthPatches.cs`
- `Source/Harmony/SunlightSubstitutionPatches.cs`
- `Source/Properties/AssemblyInfo.cs`
- `Source/Runtime/GrowLightScanner.cs`
- `Source/Runtime/GrowthScheduleContext.cs`
- `Source/Runtime/LeezLog.cs`
- `Source/Runtime/PowerStateResolver.cs`

## Documentation / development support

- `README.md`
- `CHANGELOG.md`
- `CONTRIBUTING.md`
- `ROADMAP.md`
- `TESTING.md`
- `TEST_CHECKLIST.md`
- `GITHUB_SETUP.md`
- `docs/API_VALIDATION_V3.1.md`
- `docs/BUILDING.md`
- `docs/DECISIONS.md`
- `docs/RUNTIME_DESIGN.md`
- `docs/images/GrowLightExample.JPG`
- `docs/images/README.md`
- `docs/reference/README.md`
- `docs/reference/LeezGrowLights_ApiProbe_V3.1.part01.txt` through `part08.txt`
- `tools/Probe-7D2D-Api.ps1`

The monorepo root owns `.gitignore`, `.gitattributes`, and GitHub issue templates. Generated DLL/PDB files and full runtime logs are intentionally not tracked.
