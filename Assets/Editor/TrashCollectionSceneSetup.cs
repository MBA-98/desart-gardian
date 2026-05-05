#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds <c>TrashHUD_Canvas</c>, <c>TopHUDPanel</c>, <c>DayTargetText</c>, <c>ScoreText</c> and wires <see cref="TrashCollectionUI"/>.
/// </summary>
public static class TrashCollectionSceneSetup
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BestOneScenePath = "Assets/Scenes/bestOne.unity";

    [MenuItem("Tools/Trash Collection/Setup SampleScene (Polished Trash HUD + systems)")]
    public static void MenuSetupSampleScene() => RunSetup(SampleScenePath);

    [MenuItem("Tools/Trash Collection/Setup bestOne (Polished Trash HUD + systems)")]
    public static void MenuSetupBestOne() => RunSetup(BestOneScenePath);

    /// <summary>Batch: <c>-executeMethod TrashCollectionSceneSetup.SetupSampleScene</c></summary>
    public static void SetupSampleScene() => RunSetup(SampleScenePath);

    public static void SetupBestOneScene() => RunSetup(BestOneScenePath);

    private static void RunSetup(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[TrashCollectionSceneSetup] Scene not found: {scenePath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        RemoveExisting();

        var systems = new GameObject("TrashSystems");
        RegisterCreated(systems);
        var score = systems.AddComponent<ScoreManager>();
        var daily = systems.AddComponent<DailyTargetSystem>();

        var soScore = new SerializedObject(score);
        soScore.FindProperty("dailyTarget").objectReferenceValue = daily;
        soScore.ApplyModifiedPropertiesWithoutUndo();

        BuildPolishedHud(score, daily);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TrashCollectionSceneSetup] Saved polished HUD: {scenePath}");
    }

    private static void RemoveExisting()
    {
        foreach (var u in Object.FindObjectsByType<TrashCollectionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            DestroyGo(u.gameObject);

        foreach (var s in Object.FindObjectsByType<ScoreManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            DestroyGo(s.gameObject);

        foreach (var d in Object.FindObjectsByType<DailyTargetSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            DestroyGo(d.gameObject);
    }

    private static void DestroyGo(GameObject go)
    {
        if (go == null)
            return;
        if (Application.isBatchMode)
            Object.DestroyImmediate(go);
        else
            Undo.DestroyObjectImmediate(go);
    }

    private static void RegisterCreated(Object o)
    {
        if (!Application.isBatchMode)
            Undo.RegisterCreatedObjectUndo(o, o.name);
    }

    private static void BuildPolishedHud(ScoreManager score, DailyTargetSystem daily)
    {
        var canvasGo = new GameObject("TrashHUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RegisterCreated(canvasGo);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var crt = canvasGo.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        var panel = new GameObject("TopHUDPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        RegisterCreated(panel);
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -22f);
        prt.sizeDelta = new Vector2(440f, 0f);

        var panelImg = panel.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(panelImg);
        TrashHUDFactory.ConfigurePanelBackground(panelImg);

        var v = panel.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 14, 16);
        v.spacing = 6f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var fit = panel.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var lePanel = panel.GetComponent<LayoutElement>();
        lePanel.minWidth = 320f;
        lePanel.preferredWidth = 440f;

        var dayTmp = CreateTmpText(panel.transform, "DayTargetText", "Day 1  ·  0/20", 31f, FontStyles.Bold, new Color(0.98f, 0.98f, 1f), 0.28f);
        var scoreTmp = CreateTmpText(panel.transform, "ScoreText", "Total  0", 21f, FontStyles.Normal, new Color(0.78f, 0.82f, 0.88f), 0.22f);

        var ui = canvasGo.AddComponent<TrashCollectionUI>();
        var soUi = new SerializedObject(ui);
        soUi.FindProperty("scoreManager").objectReferenceValue = score;
        soUi.FindProperty("dailyTarget").objectReferenceValue = daily;
        soUi.FindProperty("dayTargetText").objectReferenceValue = dayTmp;
        soUi.FindProperty("scoreText").objectReferenceValue = scoreTmp;
        soUi.FindProperty("showTotalScore").boolValue = true;
        soUi.FindProperty("buildRuntimeHudIfMissing").boolValue = false;
        soUi.FindProperty("anchorTopRight").boolValue = false;
        soUi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI CreateTmpText(Transform parent, string name, string initial, float size, FontStyles style, Color color, float outline)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RegisterCreated(go);
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = size + 10f;
        le.preferredHeight = size + 10f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = initial;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.outlineWidth = outline;
        tmp.outlineColor = new Color32(0, 0, 0, 210);
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }
}
#endif
