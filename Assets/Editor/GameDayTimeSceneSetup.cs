#if UNITY_EDITOR
using GameDayTime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click wiring of <see cref="GameDayTimeManager"/> + <see cref="GameDayTimeUI"/> into a scene (Canvas, TMP, inspector refs).
/// </summary>
public static class GameDayTimeSceneSetup
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BestOneScenePath = "Assets/Scenes/bestOne.unity";

    [MenuItem("Tools/Game Day Time/Setup SampleScene (Day + Time HUD)")]
    public static void MenuSetupSampleScene() => RunSetup(SampleScenePath);

    [MenuItem("Tools/Game Day Time/Setup bestOne (Day + Time HUD)")]
    public static void MenuSetupBestOne() => RunSetup(BestOneScenePath);

    /// <summary>Called by Unity batch mode: <c>-executeMethod GameDayTimeSceneSetup.SetupSampleScene</c></summary>
    public static void SetupSampleScene() => RunSetup(SampleScenePath);

    public static void SetupBestOneScene() => RunSetup(BestOneScenePath);

    private static void RunSetup(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[GameDayTimeSceneSetup] Scene not found: {scenePath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        RemoveExistingDayTime();

        var mgrGo = new GameObject("GameDayTimeManager");
        RegisterCreated(mgrGo);
        var mgr = mgrGo.AddComponent<GameDayTimeManager>();

        var soMgr = new SerializedObject(mgr);
        soMgr.FindProperty("requireSleepToAdvanceDay").boolValue = false;
        soMgr.FindProperty("maxDay").intValue = 10;
        soMgr.FindProperty("startingDay").intValue = 1;
        soMgr.ApplyModifiedPropertiesWithoutUndo();

        BuildDayTimeUiHierarchy(mgr);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameDayTimeSceneSetup] Saved: {scenePath}");
    }

    private static void RemoveExistingDayTime()
    {
        var managers = Object.FindObjectsByType<GameDayTimeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in managers)
            DestroyGo(m.gameObject);

        var uis = Object.FindObjectsByType<GameDayTimeUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var u in uis)
            DestroyGo(u.gameObject);
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

    private static void BuildDayTimeUiHierarchy(GameDayTimeManager mgr)
    {
        var root = new GameObject("DayTimeUI", typeof(RectTransform));
        RegisterCreated(root);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var canvasGo = new GameObject("DayTimeUI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RegisterCreated(canvasGo);
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        var panel = new GameObject("DayTimePanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RegisterCreated(panel);
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -16f);
        prt.sizeDelta = new Vector2(520f, 0f);
        var v = panel.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        v.spacing = 2f;
        v.padding = new RectOffset(12, 12, 8, 8);
        var fit = panel.GetComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("DayTimeText", typeof(RectTransform), typeof(LayoutElement));
        RegisterCreated(textGo);
        textGo.transform.SetParent(panel.transform, false);
        var le = textGo.GetComponent<LayoutElement>();
        le.minHeight = 44f;
        le.preferredHeight = 44f;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Day 1 - 10:00 AM";
        tmp.fontSize = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        var ui = root.AddComponent<GameDayTimeUI>();
        var soUi = new SerializedObject(ui);
        soUi.FindProperty("manager").objectReferenceValue = mgr;
        soUi.FindProperty("dayTimeLineText").objectReferenceValue = tmp;
        soUi.FindProperty("useSingleLineDisplay").boolValue = true;
        soUi.FindProperty("createDefaultTextsIfMissing").boolValue = false;
        soUi.FindProperty("barAnchor").enumValueIndex = (int)DayTimeBarAnchor.TopCenter;
        soUi.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
