using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace TD.Editor
{
    public class ConfigureGameHUDElements
    {
        [MenuItem("TD/Automation/Configure HUD Elements")]
        private static void Configure()
        {
            var hud = Object.FindObjectOfType<GameHUD>();
            if (hud == null)
            {
                Debug.LogError("[ConfigureGameHUDElements] GameHUD not found!");
                return;
            }

            CleanupOldSliders(hud);

            ConfigureCurrencyText(hud);
            ConfigureWaveText(hud);
            ConfigureEnemiesText(hud);
            ConfigureBaseHealthText(hud);
            ConfigureStartWaveButton(hud);
            ConfigureGameOverPanel(hud);
            ConfigureMainHUDGroup(hud);

            EditorUtility.SetDirty(hud);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);

            Debug.Log("[ConfigureGameHUDElements] All HUD elements configured!");
        }

        private static void CleanupOldSliders(GameHUD hud)
        {
            var waveProgressBar = hud.transform.Find("WaveProgressBar");
            if (waveProgressBar != null)
            {
                Object.DestroyImmediate(waveProgressBar.gameObject);
            }

            var baseHealthBar = hud.transform.Find("BaseHealthBar");
            if (baseHealthBar != null)
            {
                Object.DestroyImmediate(baseHealthBar.gameObject);
            }
        }

        private static GameObject CreateOrGetUIObject(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.gameObject != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ConfigureMainHUDGroup(GameHUD hud)
        {
            var canvasGroup = hud.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = hud.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var so = new SerializedObject(hud);
            so.FindProperty("mainHUDGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureCurrencyText(GameHUD hud)
        {
            var obj = GameObject.Find("CurrencyText");
            if (obj == null) return;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = "Gold: 500";
            text.fontSize = 24;
            text.color = Color.yellow;
            text.alignment = TextAlignmentOptions.TopLeft;

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(300, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("currencyText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureWaveText(GameHUD hud)
        {
            var obj = GameObject.Find("WaveText");
            if (obj == null) return;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = "Wave: 1/5";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Top;

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(200, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("waveText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureEnemiesText(GameHUD hud)
        {
            var obj = GameObject.Find("EnemiesText");
            if (obj == null) return;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = "Enemies: 0";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Top;

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -65);
            rect.sizeDelta = new Vector2(200, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("enemiesText").objectReferenceValue = text;
            so.ApplyModifiedProperties();

            CreateWaveProgressBar(hud);
        }

        private static void CreateWaveProgressBar(GameHUD hud)
        {
            var sliderGO = CreateOrGetUIObject("WaveProgressBar", hud.transform);

            var slider = sliderGO.GetComponent<Slider>();
            if (slider == null) slider = sliderGO.AddComponent<Slider>();

            var rect = sliderGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -110);
            rect.sizeDelta = new Vector2(400, 20);

            var bgGO = CreateOrGetUIObject("Background", sliderGO.transform);
            var bgImage = bgGO.GetComponent<Image>();
            if (bgImage == null) bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var fillAreaGO = CreateOrGetUIObject("Fill Area", sliderGO.transform);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGO = CreateOrGetUIObject("Fill", fillAreaGO.transform);
            var fillImage = fillGO.GetComponent<Image>();
            if (fillImage == null) fillImage = fillGO.AddComponent<Image>();
            fillImage.color = Color.green;

            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;

            var so = new SerializedObject(hud);
            so.FindProperty("waveProgressBar").objectReferenceValue = slider;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureBaseHealthText(GameHUD hud)
        {
            var obj = GameObject.Find("BaseHealthText");
            if (obj == null) return;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = "Base HP: 100";
            text.fontSize = 24;
            text.color = Color.green;
            text.alignment = TextAlignmentOptions.TopRight;

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(300, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("baseHealthText").objectReferenceValue = text;
            so.ApplyModifiedProperties();

            CreateBaseHealthBar(hud);
            CreateHealthColorGradient(hud);
        }

        private static void CreateBaseHealthBar(GameHUD hud)
        {
            var sliderGO = CreateOrGetUIObject("BaseHealthBar", hud.transform);

            var slider = sliderGO.GetComponent<Slider>();
            if (slider == null) slider = sliderGO.AddComponent<Slider>();

            var rect = sliderGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -65);
            rect.sizeDelta = new Vector2(300, 20);

            var bgGO = CreateOrGetUIObject("Background", sliderGO.transform);
            var bgImage = bgGO.GetComponent<Image>();
            if (bgImage == null) bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var fillAreaGO = CreateOrGetUIObject("Fill Area", sliderGO.transform);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGO = CreateOrGetUIObject("Fill", fillAreaGO.transform);
            var fillImage = fillGO.GetComponent<Image>();
            if (fillImage == null) fillImage = fillGO.AddComponent<Image>();
            fillImage.color = Color.green;

            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;

            var so = new SerializedObject(hud);
            so.FindProperty("baseHealthBar").objectReferenceValue = slider;
            so.FindProperty("baseHealthFill").objectReferenceValue = fillImage;
            so.ApplyModifiedProperties();
        }

        private static void CreateHealthColorGradient(GameHUD hud)
        {
            var gradient = new Gradient();

            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1f);

            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);

            var so = new SerializedObject(hud);
            so.FindProperty("healthColorGradient").gradientValue = gradient;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureStartWaveButton(GameHUD hud)
        {
            var obj = GameObject.Find("StartWaveButton");
            if (obj == null) return;

            var button = obj.GetComponent<Button>();
            var image = obj.GetComponent<Image>();
            image.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(200, 60);

            var textObj = obj.transform.Find("Text");
            if (textObj == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(obj.transform, false);
                textObj = textGO.transform;
            }

            var text = textObj.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textObj.gameObject.AddComponent<TextMeshProUGUI>();

            text.text = "Start Wave";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var so = new SerializedObject(hud);
            so.FindProperty("startWaveButton").objectReferenceValue = button;
            so.FindProperty("startWaveButtonText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void ConfigureGameOverPanel(GameHUD hud)
        {
            var panel = GameObject.Find("GameOverPanel");
            if (panel == null) return;

            var image = panel.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0.9f);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var textObj = panel.transform.Find("Text");
            if (textObj == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(panel.transform, false);
                textObj = textGO.transform;
            }

            var text = textObj.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textObj.gameObject.AddComponent<TextMeshProUGUI>();

            text.text = "Game Over!";
            text.fontSize = 64;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, 100);
            textRect.sizeDelta = new Vector2(800, 100);

            var restartBtn = CreateButton(panel.transform, "RestartButton", "Restart", new Vector2(0, 0), new Color(0.2f, 0.8f, 0.2f));
            var quitBtn = CreateButton(panel.transform, "QuitButton", "Quit", new Vector2(0, -80), new Color(0.8f, 0.2f, 0.2f));

            panel.SetActive(false);

            var so = new SerializedObject(hud);
            so.FindProperty("gameOverPanel").objectReferenceValue = panel;
            so.FindProperty("gameOverText").objectReferenceValue = text;
            so.FindProperty("restartButton").objectReferenceValue = restartBtn;
            so.FindProperty("quitButton").objectReferenceValue = quitBtn;
            so.ApplyModifiedProperties();
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Color color)
        {
            var obj = parent.Find(name);
            GameObject btnGO;

            if (obj == null)
            {
                btnGO = new GameObject(name);
                btnGO.transform.SetParent(parent, false);
            }
            else
            {
                btnGO = obj.gameObject;
            }

            var button = btnGO.GetComponent<Button>();
            if (button == null) button = btnGO.AddComponent<Button>();

            var image = btnGO.GetComponent<Image>();
            if (image == null) image = btnGO.AddComponent<Image>();
            image.color = color;

            var rect = btnGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250, 60);

            var textObj = btnGO.transform.Find("Text");
            if (textObj == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btnGO.transform, false);
                textObj = textGO.transform;
            }

            var text = textObj.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textObj.gameObject.AddComponent<TextMeshProUGUI>();

            text.text = label;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return button;
        }
    }
}
