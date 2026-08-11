# Runtime asset bundle

This directory is the runtime location for the Leez Beans Unity AssetBundle.

Expected final file:

- `leezbeans.unity3d`

Expected prefab names inside the bundle:

- `LeezBeanSproutPrefab`
- `LeezBeanGrowingPrefab`
- `LeezBeanMaturePrefab`

7 Days to Die model references use:

- `#@modfolder:Resources/leezbeans.unity3d?LeezBeanSproutPrefab`
- `#@modfolder:Resources/leezbeans.unity3d?LeezBeanGrowingPrefab`
- `#@modfolder:Resources/leezbeans.unity3d?LeezBeanMaturePrefab`

The XML currently inherits vanilla Super Corn visuals as a safe fallback so the gameplay layer can load before this binary bundle is built. Once the bundle is generated and tested, add the `Shape=ModelEntity` and `Model=...` properties documented in `Source/Unity/README.md` to the three crop stages in `Config/blocks.xml`.

Asset bundles are client-side assets. Every multiplayer client must install the mod files containing this bundle; XML can be server-pushed, Unity bundles cannot.
