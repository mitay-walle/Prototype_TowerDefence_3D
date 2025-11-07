using TD.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace TD.Editor
{
    public class SetupGameHUD : EditorWindow
    {
        [MenuItem("TD/Automation/Setup Game HUD")]
        private static void Setup()
        {
            var hud = FindObjectOfType<GameHUD>();
            if (hud != null)
            {
                Debug.Log("[SetupGameHUD] GameHUD already exists, skipping setup.");
                return;
            }

            if (hud == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    var canvasGO = new GameObject("Canvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }

                var hudGO = new GameObject("GameHUD");
                hudGO.transform.SetParent(canvas.transform, false);
                hud = hudGO.AddComponent<GameHUD>();
                hudGO.AddComponent<CanvasGroup>();

                var rect = hudGO.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            CreateCurrencyDisplay(hud);
            CreateWaveDisplay(hud);
            CreateBaseHealthDisplay(hud);
            CreateStartWaveButton(hud);
            CreateGameOverPanel(hud);

            EditorUtility.SetDirty(hud);
            Debug.Log("[SetupGameHUD] GameHUD configured successfully!");
        }

        private static void CreateCurrencyDisplay(GameHUD hud)
        {
            var currencyGO = new GameObject("CurrencyText");
            currencyGO.transform.SetParent(hud.transform, false);

            var text = currencyGO.AddComponent<TextMeshProUGUI>();
            text.text = "Gold: 0";
            text.fontSize = 24;
            text.color = Color.yellow;
            text.alignment = TextAlignmentOptions.TopLeft;

            var rect = currencyGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(200, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("currencyText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void CreateWaveDisplay(GameHUD hud)
        {
            var waveGO = new GameObject("WaveText");
            waveGO.transform.SetParent(hud.transform, false);

            var text = waveGO.AddComponent<TextMeshProUGUI>();
            text.text = "Wave: 1/5";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Top;

            var rect = waveGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(200, 30);

            var so = new SerializedObject(hud);
            so.FindProperty("waveText").objectReferenceValue = text;
            so.ApplyModifiedProperties();

            var enemiesGO = new GameObject("EnemiesText");
            enemiesGO.transform.SetParent(hud.transform, false);

            var enemiesText = enemiesGO.AddComponent<TextMeshProUGUI>();
            enemiesText.text = "Enemies: 0";
            enemiesText.fontSize = 18;
            enemiesText.color = Color.white;
            enemiesText.alignment = TextAlignmentOptions.Top;

            var enemiesRect = enemiesGO.GetComponent<RectTransform>();
            enemiesRect.anchorMin = new Vector2(0.5f, 1);
            enemiesRect.anchorMax = new Vector2(0.5f, 1);
            enemiesRect.pivot = new Vector2(0.5f, 1);
            enemiesRect.anchoredPosition = new Vector2(0, -55);
            enemiesRect.sizeDelta = new Vector2(200, 30);

            so.FindProperty("enemiesText").objectReferenceValue = enemiesText;
            so.ApplyModifiedProperties();

            var progressGO = new GameObject("WaveProgressBar");
            progressGO.transform.SetParent(hud.transform, false);

            var slider = progressGO.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;

            var progressRect = progressGO.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 1);
            progressRect.anchorMax = new Vector2(0.5f, 1);
            progressRect.pivot = new Vector2(0.5f, 1);
            progressRect.anchoredPosition = new Vector2(0, -90);
            progressRect.sizeDelta = new Vector2(300, 20);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(progressGO.transform, false);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(progressGO.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = Color.green;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            so.FindProperty("waveProgressBar").objectReferenceValue = slider;
            so.ApplyModifiedProperties();
        }

        private static void CreateBaseHealthDisplay(GameHUD hud)
        {
            var healthGO = new GameObject("BaseHealthText");
            healthGO.transform.SetParent(hud.transform, false);

            var text = healthGO.AddComponent<TextMeshProUGUI>();
            text.text = "HP: 100/100";
            text.fontSize = 20;
            text.color = Color.green;
            text.alignment = TextAlignmentOptions.TopRight;

            var rect = healthGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(200, 40);

            var so = new SerializedObject(hud);
            so.FindProperty("baseHealthText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void CreateStartWaveButton(GameHUD hud)
        {
            var buttonGO = new GameObject("StartWaveButton");
            buttonGO.transform.SetParent(hud.transform, false);

            var button = buttonGO.AddComponent<Button>();
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);

            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(200, 50);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Start Wave";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var so = new SerializedObject(hud);
            so.FindProperty("startWaveButton").objectReferenceValue = button;
            so.FindProperty("startWaveButtonText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        private static void CreateGameOverPanel(GameHUD hud)
        {
            var panelGO = new GameObject("GameOverPanel");
            panelGO.transform.SetParent(hud.transform, false);

            var image = panelGO.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);

            var rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Game Over!";
            text.fontSize = 48;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, 100);
            textRect.sizeDelta = new Vector2(600, 100);

            var restartBtnGO = new GameObject("RestartButton");
            restartBtnGO.transform.SetParent(panelGO.transform, false);
            var restartBtn = restartBtnGO.AddComponent<Button>();
            var restartImage = restartBtnGO.AddComponent<Image>();
            restartImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var restartRect = restartBtnGO.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.5f);
            restartRect.anchorMax = new Vector2(0.5f, 0.5f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.anchoredPosition = new Vector2(0, 0);
            restartRect.sizeDelta = new Vector2(200, 50);

            var restartTextGO = new GameObject("Text");
            restartTextGO.transform.SetParent(restartBtnGO.transform, false);
            var restartText = restartTextGO.AddComponent<TextMeshProUGUI>();
            restartText.text = "Restart";
            restartText.fontSize = 24;
            restartText.color = Color.white;
            restartText.alignment = TextAlignmentOptions.Center;

            var restartTextRect = restartTextGO.GetComponent<RectTransform>();
            restartTextRect.anchorMin = Vector2.zero;
            restartTextRect.anchorMax = Vector2.one;
            restartTextRect.sizeDelta = Vector2.zero;

            var quitBtnGO = new GameObject("QuitButton");
            quitBtnGO.transform.SetParent(panelGO.transform, false);
            var quitBtn = quitBtnGO.AddComponent<Button>();
            var quitImage = quitBtnGO.AddComponent<Image>();
            quitImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var quitRect = quitBtnGO.GetComponent<RectTransform>();
            quitRect.anchorMin = new Vector2(0.5f, 0.5f);
            quitRect.anchorMax = new Vector2(0.5f, 0.5f);
            quitRect.pivot = new Vector2(0.5f, 0.5f);
            quitRect.anchoredPosition = new Vector2(0, -70);
            quitRect.sizeDelta = new Vector2(200, 50);

            var quitTextGO = new GameObject("Text");
            quitTextGO.transform.SetParent(quitBtnGO.transform, false);
            var quitText = quitTextGO.AddComponent<TextMeshProUGUI>();
            quitText.text = "Quit";
            quitText.fontSize = 24;
            quitText.color = Color.white;
            quitText.alignment = TextAlignmentOptions.Center;

            var quitTextRect = quitTextGO.GetComponent<RectTransform>();
            quitTextRect.anchorMin = Vector2.zero;
            quitTextRect.anchorMax = Vector2.one;
            quitTextRect.sizeDelta = Vector2.zero;

            panelGO.SetActive(false);

            var so = new SerializedObject(hud);
            so.FindProperty("gameOverPanel").objectReferenceValue = panelGO;
            so.FindProperty("gameOverText").objectReferenceValue = text;
            so.FindProperty("restartButton").objectReferenceValue = restartBtn;
            so.FindProperty("quitButton").objectReferenceValue = quitBtn;
            so.ApplyModifiedProperties();
        }
    }
}
