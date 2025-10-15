using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TD;

public static class QuickSetupWaveUI
{
    [MenuItem("TD/Automation/Quick Setup Wave UI")]
    public static void SetupWaveUI()
    {
        // Find Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[QuickSetupWaveUI] Canvas not found!");
            return;
        }

        // Find or create WaveUI GameObject
        GameObject waveUIGO = GameObject.Find("WaveUI");
        if (waveUIGO == null)
        {
            waveUIGO = new GameObject("WaveUI");
            waveUIGO.transform.SetParent(canvas.transform, false);
        }

        // Add WaveUI component
        WaveUI waveUI = waveUIGO.GetComponent<WaveUI>();
        if (waveUI == null)
        {
            waveUI = waveUIGO.AddComponent<WaveUI>();
        }

        // Find or create Start Wave Button
        GameObject buttonGO = GameObject.Find("StartWaveButton");
        if (buttonGO == null)
        {
            buttonGO = new GameObject("StartWaveButton");
            buttonGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = buttonGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 50);
            rt.sizeDelta = new Vector2(200, 60);

            Image img = buttonGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.2f);

            Button btn = buttonGO.AddComponent<Button>();

            // Create Text child
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textGO.AddComponent<Text>();
            text.text = "Start Wave";
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // Find or create Wave Info Text
        GameObject infoTextGO = GameObject.Find("WaveInfoText");
        if (infoTextGO == null)
        {
            infoTextGO = new GameObject("WaveInfoText");
            infoTextGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = infoTextGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(400, 80);

            Text text = infoTextGO.AddComponent<Text>();
            text.text = "Ready";
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // Assign references to WaveUI
        SerializedObject so = new SerializedObject(waveUI);
        so.FindProperty("startWaveButton").objectReferenceValue = buttonGO.GetComponent<Button>();
        so.FindProperty("waveInfoText").objectReferenceValue = infoTextGO.GetComponent<Text>();
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(waveUI);
        EditorUtility.SetDirty(canvas.gameObject);

        Debug.Log("[QuickSetupWaveUI] ✓ Wave UI setup complete!");
    }
}
