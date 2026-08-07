# Contributing / development workflow

## Build requirements

- 7 Days to Die V3.1.x installation.
- Visual Studio / MSBuild capable of targeting .NET Framework 4.8.
- TFP Harmony mod installed (`Mods/0_TFP_Harmony/0Harmony.dll`).

Build instructions are in [docs/BUILDING.md](docs/BUILDING.md).

## Change rules

- Keep gameplay balance values in XML when they are already represented there.
- Do not change the 5x5 / Y+1..Y+10 geometry without updating README, TESTING, ROADMAP and XML metadata together.
- Power-dependent growth must remain server-authoritative.
- Unknown power/tile shapes should fail safe rather than grant acceleration/sunlight.
- Do not claim mid-stage power-transition correctness until rescheduling/progress preservation is implemented and tested.
- Update `CHANGELOG.md` and `ModInfo.xml` together for versioned behavior changes.

## Before a release candidate

1. Build Release with zero errors.
2. Launch with EAC disabled.
3. Check startup log for all LeeZ Harmony hooks and no LeeZ exceptions.
4. Run the current matrix in `TESTING.md`.
5. Do not commit generated DLL/PDB or full game logs.
