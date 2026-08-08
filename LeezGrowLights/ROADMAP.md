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

Dev8 live validation: `Grow light colour: Blue` displayed correctly; one selection advanced the label and visible lamp to Green immediately.

**Next active section: Multiplayer Light Sync.** See `docs/MULTIPLAYER_LIGHT_SYNC_HANDOFF.md`.

Also see `docs/COLOUR_DEV7_HANDOFF.md` and `TESTING.md` for the validated colour baseline.

### Deferred 0.7.x interaction: brightness

Requested follow-up feature: cosmetic player-controlled lamp brightness. This is intentionally deferred until Multiplayer Light Sync is addressed.

- [ ] Add a second radial command for brightness.
- [ ] Define a small brightness-level cycle (for example Dim, Normal, Bright, Very Bright, Maximum).
- [ ] Reuse the proven live block-entity cache and adjust child Unity `Light.intensity`.
- [ ] Persist brightness per placed lamp while preserving compatibility with existing dev8 colour values.
- [ ] Keep brightness independent of crop growth, coverage, artificial sunlight and electrical power draw.
- [ ] Validate brightness save/reload and immediate live updates.

## 0.8 — multiplayer and release hardening

- [ ] Dedicated server validation.
- [ ] Remote-client placement, colour and state tests.
- [ ] Performance test with dense farms / many lamps.
- [ ] Remove development-only logging.
- [ ] Final recipe/unlock/power-balance pass.
- [ ] Select a project license.
- [ ] Produce release ZIP containing runtime DLL + XML, excluding source-only tooling if desired.
