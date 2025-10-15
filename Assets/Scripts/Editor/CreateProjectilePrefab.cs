using UnityEngine;
using UnityEditor;
using TD;

public static class CreateProjectilePrefab
{
    [MenuItem("TD/Automation/Create Projectile Prefab")]
    public static void CreatePrefab()
    {
        string prefabPath = "Assets/Prefabs/Projectile.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.LogWarning("[CreateProjectilePrefab] Projectile prefab already exists! Updating configuration...");
            UpdateExistingPrefab(prefabPath);
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject projectileGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileGO.name = "Projectile";
        projectileGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        Object.DestroyImmediate(projectileGO.GetComponent<Collider>());
        Object.DestroyImmediate(projectileGO.GetComponent<Rigidbody>());

        var projectile = projectileGO.AddComponent<Projectile>();
        SerializedObject so = new SerializedObject(projectile);
        so.FindProperty("Logs").boolValue = false;
        so.FindProperty("mode").enumValueIndex = (int)ProjectileMode.SphereCastWhileFlying;
        so.FindProperty("sphereRadius").floatValue = 0.3f;
        so.FindProperty("hasAreaDamage").boolValue = false;
        so.FindProperty("areaDamageRadius").floatValue = 2f;
        so.FindProperty("areaDamagePercent").floatValue = 0.5f;
        so.FindProperty("maxLifetime").floatValue = 5f;

        so.FindProperty("meshRenderer").objectReferenceValue = projectileGO.GetComponent<MeshRenderer>();
        so.ApplyModifiedProperties();

        var trail = projectileGO.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.8f, 0.3f);
        trail.endColor = new Color(1f, 0.3f, 0.1f, 0f);

        so.Update();
        so.FindProperty("trail").objectReferenceValue = trail;
        so.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileGO, prefabPath);
        Object.DestroyImmediate(projectileGO);

        AssignToProjectilePool(prefab);

        Debug.Log($"[CreateProjectilePrefab] ✓ Created Projectile prefab at {prefabPath}");
        Debug.Log($"[CreateProjectilePrefab] ✓ Assigned to ProjectilePool");

        Selection.activeObject = prefab;
    }

    private static void UpdateExistingPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var projectile = prefab.GetComponent<Projectile>();

        if (projectile != null)
        {
            SerializedObject so = new SerializedObject(projectile);
            so.FindProperty("mode").enumValueIndex = (int)ProjectileMode.SphereCastWhileFlying;
            so.FindProperty("sphereRadius").floatValue = 0.3f;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();

            Debug.Log("[CreateProjectilePrefab] ✓ Updated existing prefab configuration");
        }

        AssignToProjectilePool(prefab);
    }

    private static void AssignToProjectilePool(GameObject projectilePrefab)
    {
        var pool = Object.FindFirstObjectByType<ProjectilePool>();
        if (pool != null)
        {
            SerializedObject so = new SerializedObject(pool);
            so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            so.FindProperty("initialPoolSize").intValue = 50;
            so.FindProperty("maxPoolSize").intValue = 200;
            so.FindProperty("autoExpand").boolValue = true;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(pool);
        }
        else
        {
            Debug.LogWarning("[CreateProjectilePrefab] ProjectilePool not found in scene. Assign prefab manually.");
        }
    }
}
