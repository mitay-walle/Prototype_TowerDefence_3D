using TD.Monsters;
using TD.Voxels;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TD.MLAgents
{
    public static class EnemyVisualAssetGenerator
    {
        public static bool TryCreateSavedEnemyPrefab(
            GameObject visualBasePrefab,
            GameObject statSourcePrefab,
            int seed,
            int roleIndex,
            out GameObject generatedPrefab)
        {
#if UNITY_EDITOR
            return TryCreateSavedEnemyPrefabEditor(visualBasePrefab, statSourcePrefab, seed, roleIndex, out generatedPrefab);
#else
            generatedPrefab = null;
            return false;
#endif
        }

#if UNITY_EDITOR
        private const string GeneratedEnemyFolder = "Assets/Prefabs/Enemies/Generated";

        private static bool TryCreateSavedEnemyPrefabEditor(
            GameObject visualBasePrefab,
            GameObject statSourcePrefab,
            int seed,
            int roleIndex,
            out GameObject generatedPrefab)
        {
            generatedPrefab = null;
            if (visualBasePrefab == null || statSourcePrefab == null || seed == 0)
                return false;

            if (!EnsureFolder(GeneratedEnemyFolder))
                return false;

            var fileName = $"Enemy_ML_{seed}_{roleIndex:00}";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedEnemyFolder}/{fileName}.prefab");
            var cachedPath = $"{GeneratedEnemyFolder}/{fileName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(cachedPath) != null)
            {
                generatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cachedPath);
                return true;
            }

            var instance = PrefabUtility.InstantiatePrefab(visualBasePrefab) as GameObject;
            if (instance == null)
                return false;

            instance.name = fileName;
            if (!CopyGameplayStats(statSourcePrefab, instance))
            {
                Object.DestroyImmediate(instance);
                return false;
            }

            var visualGenerator = FindVisualGenerator(instance);
            if (visualGenerator == null)
            {
                Object.DestroyImmediate(instance);
                return false;
            }

            SetIntProperty(visualGenerator, "seed", seed);
            SetBoolProperty(visualGenerator, "generateOnAwake", true);
            visualGenerator.Generate();

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            if (savedPrefab == null)
                return false;

            var prefabContents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var contentsGenerator = FindVisualGenerator(prefabContents);
                if (contentsGenerator == null)
                    return false;

                SetIntProperty(contentsGenerator, "seed", seed);
                SetBoolProperty(contentsGenerator, "generateOnAwake", true);
                contentsGenerator.Generate();
                contentsGenerator.GenerateAndEmbed();
                EditorUtility.SetDirty(prefabContents);
                generatedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            AssetDatabase.SaveAssets();
            return generatedPrefab != null;
        }

        private static bool CopyGameplayStats(GameObject sourcePrefab, GameObject targetPrefab)
        {
            var sourceStats = sourcePrefab.GetComponent<MonsterStats>();
            var targetStats = targetPrefab.GetComponent<MonsterStats>();
            var sourceHealth = sourcePrefab.GetComponent<MonsterHealth>();
            var targetHealth = targetPrefab.GetComponent<MonsterHealth>();
            var sourceMove = sourcePrefab.GetComponent<MonsterMove>();
            var targetMove = targetPrefab.GetComponent<MonsterMove>();
            if (sourceStats == null || targetStats == null || sourceHealth == null || targetHealth == null ||
                sourceMove == null || targetMove == null || sourceStats.statsSO == null)
                return false;

            targetStats.statsSO = sourceStats.statsSO;
            EditorUtility.SetDirty(targetStats);
            CopySerializedProperty(sourceHealth, targetHealth, "maxHealth");
            CopySerializedProperty(sourceMove, targetMove, "baseSpeed");
            return true;
        }

        private static VoxelGenerator FindVisualGenerator(GameObject root)
        {
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                if (!(component is VoxelGenerator generator))
                    continue;

                var serialized = new SerializedObject(component);
                if (serialized.FindProperty("seed") != null && serialized.FindProperty("profile") != null)
                    return generator;
            }

            return null;
        }

        private static void SetIntProperty(Component component, string propertyName, int value)
        {
            var serialized = new SerializedObject(component);
            serialized.Update();
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolProperty(Component component, string propertyName, bool value)
        {
            var serialized = new SerializedObject(component);
            serialized.Update();
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopySerializedProperty(Object source, Object target, string propertyName)
        {
            var sourceSerialized = new SerializedObject(source);
            var targetSerialized = new SerializedObject(target);
            var sourceProperty = sourceSerialized.FindProperty(propertyName);
            var targetProperty = targetSerialized.FindProperty(propertyName);
            if (sourceProperty == null || targetProperty == null)
                return;

            targetSerialized.Update();
            targetProperty.floatValue = sourceProperty.floatValue;
            targetSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return true;

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name) || !AssetDatabase.IsValidFolder(parent))
                return false;

            AssetDatabase.CreateFolder(parent, name);
            return AssetDatabase.IsValidFolder(folder);
        }
#endif
    }
}
