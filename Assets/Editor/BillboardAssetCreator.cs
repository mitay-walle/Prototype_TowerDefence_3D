using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TD.Towers;
using UnityEditor;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public sealed class BillboardAssetCreator : EditorWindow
{
    private const int ImageCount = 8;
    private const string DirectionCategory = "Direction";
    private const string DefaultDirectionRegex =
        @"(?:^|[-_. ])(?<direction>north[-_. ]?east|north[-_. ]?west|south[-_. ]?east|south[-_. ]?west|north|south|east|west|ne|nw|se|sw|n|s|e|w)(?:$|[-_. ])";
    private const string PreferencesPrefix = "TD.BillboardAssetCreator.";
    private const string SourceFolderPreference = PreferencesPrefix + "SourceFolder";
    private const string MaterialPreference = PreferencesPrefix + "Material";

    private static readonly string[] DirectionNames =
    {
        "East (+X)",
        "North-East",
        "North",
        "North-West",
        "West",
        "South-West",
        "South",
        "South-East"
    };

    private static readonly string[] DirectionLabels =
    {
        "East",
        "North-East",
        "North",
        "North-West",
        "West",
        "South-West",
        "South",
        "South-East"
    };

    private readonly List<Sprite>[] _matches = CreateMatchLists();

    private DefaultAsset _sourceFolder;
    private Material _spriteMaterial;
    private string _scanError;

    [MenuItem("TD/Art/Create Billboard Prefab From Sprites")]
    private static void OpenWindow()
    {
        var window = GetWindow<BillboardAssetCreator>("Sprite Billboard");
        window.minSize = new Vector2(500f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadPreferences();
        if (_sourceFolder == null)
        {
            TryUseSelectedFolder();
        }

        RefreshMatches();
    }

    private void OnDisable()
    {
        SavePreferences();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite billboard prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The tool scans this folder recursively and links each found Sprite below.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Source folder",
            _sourceFolder,
            typeof(DefaultAsset),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            SavePreferences();
            RefreshMatches();
        }

        if (GUILayout.Button("Refresh matches"))
        {
            RefreshMatches();
        }

        string folderPath = GetSourceFolderPath();
        if (_sourceFolder != null && string.IsNullOrEmpty(folderPath))
        {
            EditorGUILayout.HelpBox("Select a folder inside Assets.", MessageType.Error);
        }

        if (!string.IsNullOrEmpty(_scanError))
        {
            EditorGUILayout.HelpBox(_scanError, MessageType.Error);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            string.Concat("Matches (", GetMatchedDirectionCount(), "/", ImageCount, ")"),
            EditorStyles.boldLabel);

        for (int index = 0; index < ImageCount; index++)
        {
            DrawMatchLinks(index);
        }

        EditorGUILayout.Space(4f);
        EditorGUI.BeginChangeCheck();
        _spriteMaterial = (Material)EditorGUILayout.ObjectField(
            "Sprite material (optional)",
            _spriteMaterial,
            typeof(Material),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            SavePreferences();
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(!HasAllInputs()))
        {
            if (GUILayout.Button("Create prefab", GUILayout.Height(30f)))
            {
                CreateSpriteBillboardPrefab();
            }
        }
    }

    private void TryUseSelectedFolder()
    {
        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(selectedPath))
        {
            selectedPath = Path.GetDirectoryName(selectedPath);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                selectedPath = selectedPath.Replace("\\", "/");
            }
        }

        if (AssetDatabase.IsValidFolder(selectedPath))
        {
            _sourceFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(selectedPath);
        }
    }

    private void LoadPreferences()
    {
        string folderPath = EditorPrefs.GetString(SourceFolderPreference, string.Empty);
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            _sourceFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
        }

        string materialPath = EditorPrefs.GetString(MaterialPreference, string.Empty);
        if (!string.IsNullOrEmpty(materialPath))
        {
            _spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(
            SourceFolderPreference,
            GetSourceFolderPath() ?? string.Empty);
        EditorPrefs.SetString(
            MaterialPreference,
            _spriteMaterial == null ? string.Empty : AssetDatabase.GetAssetPath(_spriteMaterial));
    }

    private void DrawMatchLinks(int index)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(DirectionNames[index], GUILayout.Width(110f));

        if (_matches[index].Count == 0)
        {
            EditorGUILayout.LabelField("Not found", EditorStyles.miniLabel);
        }
        else
        {
            if (_matches[index].Count > 1)
            {
                EditorGUILayout.LabelField("Ambiguous:", GUILayout.Width(70f));
            }

            for (int matchIndex = 0; matchIndex < _matches[index].Count; matchIndex++)
            {
                Sprite sprite = _matches[index][matchIndex];
                GUIContent content = new GUIContent(
                    sprite.name,
                    AssetDatabase.GetAssetPath(sprite));
                if (GUILayout.Button(content, EditorStyles.linkLabel))
                {
                    Selection.activeObject = sprite;
                    EditorGUIUtility.PingObject(sprite);
                }

                if (matchIndex < _matches[index].Count - 1)
                {
                    EditorGUILayout.LabelField(",", GUILayout.Width(8f));
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshMatches()
    {
        for (int index = 0; index < ImageCount; index++)
        {
            _matches[index].Clear();
        }

        _scanError = null;
        string folderPath = GetSourceFolderPath();
        if (string.IsNullOrEmpty(folderPath))
        {
            Repaint();
            return;
        }

        Regex directionRegex;
        try
        {
            directionRegex = new Regex(
                DefaultDirectionRegex,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException exception)
        {
            _scanError = string.Concat("Invalid regex: ", exception.Message);
            Repaint();
            return;
        }

        bool hasDirectionGroup = false;
        foreach (string groupName in directionRegex.GetGroupNames())
        {
            if (string.Equals(groupName, "direction", StringComparison.Ordinal))
            {
                hasDirectionGroup = true;
                break;
            }
        }

        if (!hasDirectionGroup)
        {
            _scanError = "Regex must contain the named group (?<direction>...).";
            Repaint();
            return;
        }

        var seenSprites = new HashSet<int>();
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string textureGuid in textureGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    AddMatch(sprite, directionRegex, seenSprites);
                }
            }
        }

        string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { folderPath });
        foreach (string atlasGuid in atlasGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(atlasGuid);
            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
            if (spriteAtlas == null)
            {
                continue;
            }

            Sprite[] atlasSprites = new Sprite[spriteAtlas.spriteCount];
            int spriteCount = spriteAtlas.GetSprites(atlasSprites);
            for (int index = 0; index < spriteCount; index++)
            {
                AddMatch(atlasSprites[index], directionRegex, seenSprites);
            }
        }

        for (int index = 0; index < ImageCount; index++)
        {
            _matches[index].Sort(CompareSprites);
        }

        Repaint();
    }

    private void AddMatch(Sprite sprite, Regex directionRegex, HashSet<int> seenSprites)
    {
        if (sprite == null || !seenSprites.Add(sprite.GetInstanceID()))
        {
            return;
        }

        Match match = directionRegex.Match(sprite.name);
        if (!match.Success)
        {
            return;
        }

        Group directionGroup = match.Groups["direction"];
        if (!directionGroup.Success)
        {
            return;
        }

        int directionIndex = GetDirectionIndex(directionGroup.Value);
        if (directionIndex >= 0)
        {
            _matches[directionIndex].Add(sprite);
        }
    }

    private bool HasAllInputs()
    {
        if (!string.IsNullOrEmpty(_scanError))
        {
            return false;
        }

        for (int index = 0; index < ImageCount; index++)
        {
            if (_matches[index].Count != 1)
            {
                return false;
            }
        }

        return true;
    }

    private void CreateSpriteBillboardPrefab()
    {
        RefreshMatches();
        if (!TryGetUniqueSprites(out Sprite[] sprites, out string matchError))
        {
            EditorUtility.DisplayDialog("Cannot create prefab", matchError, "OK");
            return;
        }

        if (!TryValidateSprites(sprites, out string validationError))
        {
            EditorUtility.DisplayDialog("Cannot create prefab", validationError, "OK");
            return;
        }

        string prefabPath = EditorUtility.SaveFilePanelInProject(
            "Save Sprite Billboard Prefab",
            GetDefaultAssetName(),
            "prefab",
            "Select the location for the generated prefab.");

        if (string.IsNullOrEmpty(prefabPath))
        {
            return;
        }

        string directory = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
        string baseName = Path.GetFileNameWithoutExtension(prefabPath);
        string libraryPath = string.Concat(directory, "/", baseName, "_SpriteLibrary.spriteLib");

        if (!ConfirmOverwrite(prefabPath, libraryPath))
        {
            return;
        }

        DeleteExistingAsset(prefabPath);
        DeleteExistingAsset(libraryPath);

        SpriteLibraryAsset libraryAsset = null;
        GameObject temporaryRoot = null;
        bool libraryAssetCreated = false;
        bool prefabCreated = false;

        try
        {
            CreateSpriteLibraryFile(libraryPath, sprites);
            libraryAssetCreated = true;
            AssetDatabase.ImportAsset(libraryPath, ImportAssetOptions.ForceUpdate);
            libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            if (libraryAsset == null)
            {
                throw new InvalidOperationException(
                    string.Concat("Could not import SpriteLibrary asset: ", libraryPath));
            }

            temporaryRoot = CreatePrefabRoot(baseName, libraryAsset, sprites);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, prefabPath);
            if (prefabAsset == null)
            {
                throw new InvalidOperationException(
                    string.Concat("Could not create prefab: ", prefabPath));
            }

            prefabCreated = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log(
                string.Concat(
                    "[BillboardAssetCreator] Created prefab ",
                    prefabPath,
                    " with ",
                    ImageCount,
                    " sprites, two SpriteRenderers and a SpriteLibrary."),
                Selection.activeObject);
        }
        catch (Exception exception)
        {
            if (prefabCreated)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            if (libraryAssetCreated)
            {
                AssetDatabase.DeleteAsset(libraryPath);
            }

            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Prefab creation failed",
                exception.Message,
                "OK");
        }
        finally
        {
            if (temporaryRoot != null)
            {
                DestroyImmediate(temporaryRoot);
            }
        }
    }

    private bool TryGetUniqueSprites(out Sprite[] sprites, out string error)
    {
        sprites = new Sprite[ImageCount];
        error = null;

        for (int index = 0; index < ImageCount; index++)
        {
            if (_matches[index].Count != 1)
            {
                error = string.Concat(
                    DirectionNames[index],
                    " must have exactly one match. Current matches: ",
                    GetMatchSummary(index),
                    ".");
                return false;
            }

            sprites[index] = _matches[index][0];
        }

        return true;
    }

    private bool TryValidateSprites(Sprite[] sprites, out string error)
    {
        error = null;
        int frameWidth = Mathf.RoundToInt(sprites[0].rect.width);
        int frameHeight = Mathf.RoundToInt(sprites[0].rect.height);
        float pixelsPerUnit = sprites[0].pixelsPerUnit;
        Vector2 pivot = sprites[0].pivot;

        for (int index = 0; index < ImageCount; index++)
        {
            Sprite sprite = sprites[index];
            if (sprite.texture == null)
            {
                error = string.Concat("Sprite ", sprite.name, " has no source texture.");
                return false;
            }

            if (Mathf.RoundToInt(sprite.rect.width) != frameWidth ||
                Mathf.RoundToInt(sprite.rect.height) != frameHeight)
            {
                error = "All eight sprites must have the same pixel dimensions.";
                return false;
            }

            if (!Mathf.Approximately(sprite.pixelsPerUnit, pixelsPerUnit))
            {
                error = "All eight sprites must use the same Pixels Per Unit value.";
                return false;
            }

            if (!Mathf.Approximately(sprite.pivot.x, pivot.x) ||
                !Mathf.Approximately(sprite.pivot.y, pivot.y))
            {
                error = "All eight sprites must use the same pivot.";
                return false;
            }
        }

        if (frameWidth <= 0 || frameHeight <= 0)
        {
            error = "Sprite dimensions must be greater than zero.";
            return false;
        }

        if (pixelsPerUnit <= 0f)
        {
            error = "Pixels Per Unit must be greater than zero.";
            return false;
        }

        return true;
    }

    private static string CreateSpriteLibraryFile(string libraryPath, Sprite[] sprites)
    {
        var labels = new List<SpriteLibraryLabel>();
        for (int index = 0; index < ImageCount; index++)
        {
            labels.Add(new SpriteLibraryLabel(DirectionLabels[index], sprites[index]));
        }

        var categories = new[]
        {
            new SpriteLibraryCategory(DirectionCategory, labels)
        };
        return SpriteLibrarySourceAssetFactory.Create(libraryPath, categories);
    }

    private GameObject CreatePrefabRoot(string assetName, SpriteLibraryAsset libraryAsset, Sprite[] sprites)
    {
        var root = new GameObject(assetName);
        SpriteLibrary spriteLibrary = root.AddComponent<SpriteLibrary>();
        spriteLibrary.spriteLibraryAsset = libraryAsset;

        GameObject colorObject = new GameObject("Color");
        colorObject.transform.SetParent(root.transform, false);
        SpriteRenderer colorRenderer = colorObject.AddComponent<SpriteRenderer>();
        colorRenderer.sprite = sprites[0];
        colorRenderer.sharedMaterial = _spriteMaterial;
        SpriteResolver colorResolver = colorObject.AddComponent<SpriteResolver>();
        colorResolver.SetCategoryAndLabel(DirectionCategory, DirectionLabels[0]);

        GameObject shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(root.transform, false);
        SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = sprites[0];
        shadowRenderer.sharedMaterial = _spriteMaterial;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);
        shadowRenderer.sortingOrder = -1;
        SpriteResolver shadowResolver = shadowObject.AddComponent<SpriteResolver>();
        shadowResolver.SetCategoryAndLabel(DirectionCategory, DirectionLabels[0]);

        DirectionalSpriteBillboard controller = root.AddComponent<DirectionalSpriteBillboard>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("_colorResolver").objectReferenceValue = colorResolver;
        serializedController.FindProperty("_shadowResolver").objectReferenceValue = shadowResolver;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private string GetDefaultAssetName()
    {
        string folderPath = GetSourceFolderPath();
        if (!string.IsNullOrEmpty(folderPath))
        {
            string folderName = Path.GetFileName(folderPath.TrimEnd('/'));
            if (!string.IsNullOrEmpty(folderName) && !string.Equals(folderName, "Assets", StringComparison.Ordinal))
            {
                return string.Concat(folderName, "_SpriteBillboard");
            }
        }

        return "SpriteBillboard";
    }

    private string GetSourceFolderPath()
    {
        if (_sourceFolder == null)
        {
            return null;
        }

        string path = AssetDatabase.GetAssetPath(_sourceFolder);
        return AssetDatabase.IsValidFolder(path) ? path : null;
    }

    private int GetMatchedDirectionCount()
    {
        int count = 0;
        for (int index = 0; index < ImageCount; index++)
        {
            if (_matches[index].Count == 1)
            {
                count++;
            }
        }

        return count;
    }

    private string GetMatchSummary(int index)
    {
        if (_matches[index].Count == 0)
        {
            return "Not found";
        }

        if (_matches[index].Count == 1)
        {
            Sprite sprite = _matches[index][0];
            return string.Concat(sprite.name, "  [", AssetDatabase.GetAssetPath(sprite), "]");
        }

        return string.Concat("Ambiguous: ", GetMatchSummary(_matches[index]));
    }

    private static string GetMatchSummary(List<Sprite> sprites)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < sprites.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(sprites[index].name);
        }

        return builder.ToString();
    }

    private static int CompareSprites(Sprite left, Sprite right)
    {
        int pathComparison = string.Compare(
            AssetDatabase.GetAssetPath(left),
            AssetDatabase.GetAssetPath(right),
            StringComparison.OrdinalIgnoreCase);
        return pathComparison != 0
            ? pathComparison
            : string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetDirectionIndex(string direction)
    {
        string normalizedDirection = direction
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        switch (normalizedDirection)
        {
            case "e":
            case "east":
                return 0;
            case "ne":
            case "northeast":
                return 1;
            case "n":
            case "north":
                return 2;
            case "nw":
            case "northwest":
                return 3;
            case "w":
            case "west":
                return 4;
            case "sw":
            case "southwest":
                return 5;
            case "s":
            case "south":
                return 6;
            case "se":
            case "southeast":
                return 7;
            default:
                return -1;
        }
    }

    private static List<Sprite>[] CreateMatchLists()
    {
        var matches = new List<Sprite>[ImageCount];
        for (int index = 0; index < ImageCount; index++)
        {
            matches[index] = new List<Sprite>();
        }

        return matches;
    }

    private static bool ConfirmOverwrite(string prefabPath, string libraryPath)
    {
        bool prefabExists = AssetDatabase.LoadMainAssetAtPath(prefabPath) != null;
        bool libraryExists = AssetDatabase.LoadMainAssetAtPath(libraryPath) != null;

        if (!prefabExists && !libraryExists)
        {
            return true;
        }

        return EditorUtility.DisplayDialog(
            "Overwrite sprite billboard assets?",
            string.Concat(
                "The following assets will be replaced:\n",
                prefabPath,
                "\n",
                libraryPath),
            "Replace",
            "Cancel");
    }

    private static void DeleteExistingAsset(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
        {
            return;
        }

        if (!AssetDatabase.DeleteAsset(assetPath))
        {
            throw new InvalidOperationException(string.Concat("Could not replace asset: ", assetPath));
        }
    }
}
