using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Leez Beans Mod - editor helper.
///
/// 1. Copies the Git-tracked OBJ/MTL from Source/Models into this Unity project.
/// 2. Creates three 7DTD-friendly prefab roots using the bean mesh.
/// 3. Adds a simple plant hit collider and applies the T_Mesh_B tag.
/// 4. Builds Resources/leezbeans.unity3d for StandaloneWindows64.
///
/// Use a Unity editor/toolchain version compatible with the targeted 7DTD build.
/// </summary>
public static class BuildLeezBeansBundle
{
    private const string ModelAsset = "Assets/LeezBeans/Models/leez_bean_plant.obj";
    private const string MaterialAsset = "Assets/LeezBeans/Models/leez_bean_plant.mtl";
    private const string PrefabFolder = "Assets/LeezBeans/Prefabs";

    private static readonly Stage[] Stages =
    {
        new Stage("LeezBeanSproutPrefab",   new Vector3(0.45f, 0.35f, 0.45f), 0.34f, 0.18f),
        new Stage("LeezBeanGrowingPrefab", new Vector3(0.75f, 0.65f, 0.75f), 0.62f, 0.24f),
        new Stage("LeezBeanMaturePrefab",  Vector3.one,                         0.92f, 0.30f),
    };

    [MenuItem("Leez Beans/1 - Prepare Source Model and Prefabs")]
    public static void Prepare()
    {
        EnsureTagExists();
        EnsureFolder("Assets/LeezBeans");
        EnsureFolder("Assets/LeezBeans/Models");
        EnsureFolder(PrefabFolder);

        string sourceModel = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Models/leez_bean_plant.obj"));
        string sourceMtl = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Models/leez_bean_plant.mtl"));
        string destModel = Path.GetFullPath(Path.Combine(Application.dataPath, "LeezBeans/Models/leez_bean_plant.obj"));
        string destMtl = Path.GetFullPath(Path.Combine(Application.dataPath, "LeezBeans/Models/leez_bean_plant.mtl"));

        if (!File.Exists(sourceModel))
            throw new FileNotFoundException("Bean OBJ source not found", sourceModel);
        if (!File.Exists(sourceMtl))
            throw new FileNotFoundException("Bean MTL source not found", sourceMtl);

        File.Copy(sourceModel, destModel, true);
        File.Copy(sourceMtl, destMtl, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAsset);
        if (model == null)
            throw new InvalidOperationException("Unity could not import " + ModelAsset);

        foreach (Stage stage in Stages)
            CreatePrefab(model, stage);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Leez Beans: source model imported and three growth-stage prefabs prepared.");
    }

    [MenuItem("Leez Beans/2 - Build Runtime AssetBundle")]
    public static void BuildBundle()
    {
        string[] prefabPaths = Stages
            .Select(s => PrefabFolder + "/" + s.Name + ".prefab")
            .ToArray();

        foreach (string path in prefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new FileNotFoundException("Missing prefab. Run Prepare first: " + path);
        }

        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Resources"));
        Directory.CreateDirectory(output);

        var build = new AssetBundleBuild
        {
            assetBundleName = "leezbeans.unity3d",
            assetNames = prefabPaths
        };

        BuildPipeline.BuildAssetBundles(
            output,
            new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);

        Debug.Log("Leez Beans: built " + Path.Combine(output, "leezbeans.unity3d"));
    }

    [MenuItem("Leez Beans/Build All")]
    public static void BuildAll()
    {
        Prepare();
        BuildBundle();
    }

    private static void CreatePrefab(GameObject model, Stage stage)
    {
        var root = new GameObject(stage.Name);
        try
        {
            root.tag = "T_Mesh_B";

            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(model);
            child.name = "BeanModel";
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = stage.Scale;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1; // Y axis
            collider.height = stage.ColliderHeight;
            collider.radius = stage.ColliderRadius;
            collider.center = new Vector3(0f, stage.ColliderHeight * 0.5f, 0f);
            collider.isTrigger = false;

            string path = PrefabFolder + "/" + stage.Name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void EnsureTagExists()
    {
        if (!InternalEditorUtility.tags.Contains("T_Mesh_B"))
        {
            throw new InvalidOperationException(
                "Required 7DTD Unity tag T_Mesh_B is missing. Import a current 7DTD TagManager.asset/template before building.");
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string name = Path.GetFileName(assetPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid Unity asset folder path: " + assetPath);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private sealed class Stage
    {
        public readonly string Name;
        public readonly Vector3 Scale;
        public readonly float ColliderHeight;
        public readonly float ColliderRadius;

        public Stage(string name, Vector3 scale, float colliderHeight, float colliderRadius)
        {
            Name = name;
            Scale = scale;
            ColliderHeight = colliderHeight;
            ColliderRadius = colliderRadius;
        }
    }
}
