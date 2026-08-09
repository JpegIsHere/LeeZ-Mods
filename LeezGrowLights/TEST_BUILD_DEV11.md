# LeezGrowLights v0.7.0-dev11-test1

Regression test build based on the validated v0.7.0-dev10 brightness runtime.

Primary checks:

1. Direct generator -> grow light ON: configured 10 W draw.
2. Direct generator -> grow light OFF: 0 W draw.
3. Toggle the light ON again: configured 10 W draw restored.
4. Save/reload with the light OFF: live consumer remains 0 W after initialization.
5. Radial menu: Color command shows the built-in `tool` icon.
6. Radial menu: Brightness command shows the built-in `wrench` icon.
7. Existing colour and brightness cycling/persistence still behave as dev10.
