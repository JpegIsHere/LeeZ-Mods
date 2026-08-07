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

- [ ] Map the V3.1 world/block ticker API.
- [ ] Determine safe invalidation/rescheduling path for crop ticks.
- [ ] Preserve accumulated progress across ON -> OFF, OFF -> ON and lamp removal.
- [ ] Handle save/reload correctly.
- [ ] Keep behavior server-authoritative.

## 0.7 — colour system

Allowed colours: Blue, Green, Red, Purple, White, Yellow.

- [ ] Player colour-selection interaction/UI.
- [ ] Persist selected colour per placed light.
- [ ] Multiplayer synchronization.
- [ ] Tint the actual light component/panel effect at runtime.
- [ ] Preserve colour through save/reload and reconnect.

## 0.8 — multiplayer and release hardening

- [ ] Dedicated server validation.
- [ ] Remote-client placement and state tests.
- [ ] Performance test with dense farms / many lamps.
- [ ] Remove development-only logging.
- [ ] Final recipe/unlock/power-balance pass.
- [ ] Select a project license.
- [ ] Produce release ZIP containing runtime DLL + XML, excluding source-only tooling if desired.
