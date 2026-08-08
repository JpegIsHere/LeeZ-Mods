# Roadmap

## 0.5.x — stabilize the proven core

- [ ] Explicitly validate Y+11 is rejected.
- [ ] Explicitly validate X/Z radius 3 is rejected.
- [ ] Test overlapping tiers and highest-tier-wins behavior.
- [ ] Replace temporary diagnostic logging with a debug flag or remove it.
- [ ] Long-duration sealed-room survival/growth test.
- [ ] Test all tier stage timings against expected values.

## 0.6 — exact dynamic power transitions

Goal: power changes during an already-scheduled crop stage must affect only future growth time.

- [x] Map the V3.1 world/block ticker API.
- [x] Confirm safe invalidation/rescheduling path for crop ticks: `InvalidateScheduledBlockUpdate` + `AddScheduledBlockUpdate`.
- [x] Implement remaining-progress conversion/rescheduling for direct ON -> OFF and OFF -> ON transitions.
- [x] Live-validate T6 ON -> OFF -> ON tick expansion/reduction.
- [x] Live-validate proportional T4 `1x <-> 1.5x` transitions.
- [x] Preserve already-earned progress for direct lamp toggle transitions.
- [x] Handle save/reload correctly.
- [x] Validate chunk unload/reload.
- [ ] Live-validate T1 -> T6 and T6 -> T1 transitions as an isolated regression test.
- [ ] Validate overlapping tiers where the effective highest multiplier does not change.
- [ ] Complete final lamp-removal regression evidence on the latest runtime.
- [ ] Validate direct source power loss/restoration as an isolated regression test.
- [ ] Validate upstream relay-only propagation before claiming it supported.
- [ ] Keep behavior server-authoritative on dedicated server / remote client.

## 0.7 — colour system

Allowed colours: Blue, Green, Red, Purple, White, Yellow.

- [x] Create isolated colour-system development branch and palette/state foundation.
- [x] Probe/diagnose the actual V3.1 activation and persistence contracts.
- [x] Add player colour-selection radial interaction.
- [x] Persist selected colour per placed light in `BlockValue.meta2`.
- [x] Preserve colour through save/quit/restart.
- [x] Tint the panel/material and child Unity light components.
- [x] Apply colour changes immediately during live play (dev7 cached `BlockEntityData` refresh).
- [x] Fix the cosmetic key-style radial label; dev8 displays friendly `Grow light colour: <Colour>` text.
- [ ] Add multiplayer/server routing and synchronization for remote colour changes.

Known-good colour source baseline: `0a1967e94dcd153e8ad8ff40de545b8b9245903b` (`v0.7.0-dev8`).

Dev8 live colour validation: `Grow light colour: Blue` displayed correctly; one selection advanced the label and visible lamp to Green immediately.

Multiplayer Light Sync remains pending. See `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`.

Also see `docs/COLOUR_DEV7_HANDOFF.md` and `TESTING.md` for the validated colour baseline.

### 0.7.x interaction: brightness

Brightness is implemented on `dev/colour-system` as a **dev8 live-test candidate**. See `docs/BRIGHTNESS_DEV8_HANDOFF.md`.

- [x] Add a second radial command for brightness.
- [x] Define five brightness levels: Dim, Normal, Bright, Very Bright, Maximum.
- [x] Reuse the proven live block-entity cache and adjust child Unity `Light.intensity`.
- [x] Persist brightness per placed lamp while preserving compatibility with existing dev7 colour values.
- [x] Keep brightness independent in source from crop growth, coverage, artificial sunlight and electrical power draw.
- [ ] Live-validate the full brightness cycle and friendly radial localization.
- [ ] Live-validate immediate visual updates without reload.
- [ ] Live-validate colour/brightness preservation when changing either setting.
- [ ] Live-validate brightness save/quit/restart persistence.
- [ ] Live-validate powered-light off/on behaviour without intensity compounding or drift.
- [ ] Live-validate relative intensity multipliers on at least two grow-light tiers.
- [ ] Live-regression crop growth, coverage, artificial sunlight, tier rules and power draw.
- [ ] Validate legacy/dev7 `meta2` values `1..6` still restore the same colour at Normal brightness.

Brightness multipliers in the candidate are Dim `0.35x`, Normal `1.00x`, Bright `1.50x`, Very Bright `2.00x`, Maximum `3.00x`, relative to each Unity light's vanilla/base intensity.

Remote-client colour and brightness authoring remain blocked until server-authoritative routing is implemented.

## 0.8 — multiplayer and release hardening

- [ ] Dedicated server validation.
- [ ] Remote-client placement, colour, brightness and state tests.
- [ ] Performance test with dense farms / many lamps.
- [ ] Remove development-only logging.
- [ ] Final recipe/unlock/power-balance pass.
- [ ] Select a project license.
- [ ] Produce release ZIP containing runtime DLL + XML, excluding source-only tooling if desired.
