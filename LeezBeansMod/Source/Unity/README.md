# Leez Beans Unity asset build

The gameplay XML is usable without a custom bundle because it currently inherits the vanilla Super Corn visuals. This Unity source is the handoff for replacing those fallback visuals with the reviewed bean plant.

## Requirements

- A Unity editor/toolchain version compatible with the targeted 7 Days to Die release.
- A 7DTD-ready `TagManager.asset` containing `T_Mesh_B` in the expected tag setup.
- The source files already tracked at `../Models/leez_bean_plant.obj` and `../Models/leez_bean_plant.mtl`.

Custom Unity assets are not server-pushed by 7DTD. Every client needs the finished mod including the `Resources` bundle.

## Build

1. Create/open a Unity project in this `Source/Unity` folder with the correct editor version.
2. Keep `Assets/Editor/BuildLeezBeansBundle.cs` in the project.
3. Import the current 7DTD `TagManager.asset` into `ProjectSettings` if `T_Mesh_B` is missing.
4. In Unity choose **Leez Beans > Build All**.
5. The script imports the tracked OBJ/MTL, creates three prefab roots with capsule hit colliders, and writes `LeezBeansMod/Resources/leezbeans.unity3d`.
6. Test the prefabs in game before enabling the custom model references.

## Prefabs produced

- `LeezBeanSproutPrefab` - 35% height / 45% width baseline
- `LeezBeanGrowingPrefab` - 65% height / 75% width baseline
- `LeezBeanMaturePrefab` - full source-model scale

These are development defaults. The scale, collider dimensions, materials, leaf density and visible pods can all be revised as the art develops.

## Enable the custom bean models

After `Resources/leezbeans.unity3d` has been built and verified, add these two properties after `Extends` in each matching block in `Config/blocks.xml`:

### plantedLeezBeans1

```xml
<property name="Shape" value="ModelEntity" />
<property name="Model" value="#@modfolder:Resources/leezbeans.unity3d?LeezBeanSproutPrefab" />
```

### plantedLeezBeans2

```xml
<property name="Shape" value="ModelEntity" />
<property name="Model" value="#@modfolder:Resources/leezbeans.unity3d?LeezBeanGrowingPrefab" />
```

### plantedLeezBeans3Harvest and plantedLeezBeans3HarvestPlayer

```xml
<property name="Shape" value="ModelEntity" />
<property name="Model" value="#@modfolder:Resources/leezbeans.unity3d?LeezBeanMaturePrefab" />
```

Keep the root transform at `(0,0,0)` and root scale `(1,1,1)`. Scale/reposition the child mesh instead. The builder follows this convention.

## Art note

The current Git-tracked OBJ is a lightweight source mesh. The approved review render is the visual target: broad healthy leaves, natural green stems, and clearly visible hanging bean pods. A later art pass can replace the OBJ without changing any gameplay IDs or XML mechanics.
