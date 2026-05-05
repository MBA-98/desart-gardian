#if UNITY_EDITOR
using GameDayTime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates a persistent, scene-authored HUD Canvas containing:
/// - DayTimeUI (GameDayTimeUI + TMP)
/// - TrashHUD (TrashCollectionUI + TMPs)
/// and wires references to the scene systems.
/// </summary>
public static class PersistentHudSceneSetup
{
    private const string BestOneScenePath = "Assets/Scenes/bestOne.unity";
    private const string PerfictScenePath = "Assets/Scenes/Perfict.unity";

    [MenuItem("Tools/HUD/Setup bestOne (Persistent DayTime + Trash HUD)")]
    public static void SetupBestOne() => RunSetup(BestOneScenePath);

    [MenuItem("Tools/HUD/Setup Perfict (Persistent DayTime + Trash HUD)")]
    public static void SetupPerfict() => RunSetup(PerfictScenePath);

    [MenuItem("Tools/HUD/Setup Current Scene (Persistent DayTime + Trash HUD)")]
    public static void SetupCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[PersistentHudSceneSetup] No active scene.");
            return;
        }

        RunSetupForOpenScene();

        // Persist changes when using "current scene".
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PersistentHudSceneSetup] Saved persistent HUD: {scene.path}");
    }

    private static void RunSetup(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[PersistentHudSceneSetup] Scene not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RunSetupForOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PersistentHudSceneSetup] Saved persistent HUD: {scenePath}");
    }

    private static void RunSetupForOpenScene()
    {
        // Ensure systems exist.
        var timeMgr = Object.FindFirstObjectByType<GameDayTimeManager>();
        if (timeMgr == null)
            timeMgr = new GameObject("GameDayTimeManager").AddComponent<GameDayTimeManager>();

        var score = Object.FindFirstObjectByType<ScoreManager>();
        var daily = Object.FindFirstObjectByType<DailyTargetSystem>();
        var spawner = Object.FindFirstObjectByType<TrashSpawnManager>();
        var trashRoot = GameObject.Find("TrashSystems");

        if (trashRoot == null)
            trashRoot = new GameObject("TrashSystems");

        if (daily == null)
            daily = trashRoot.GetComponent<DailyTargetSystem>() ?? trashRoot.AddComponent<DailyTargetSystem>();
        if (score == null)
            score = trashRoot.GetComponent<ScoreManager>() ?? trashRoot.AddComponent<ScoreManager>();
        if (spawner == null)
            spawner = trashRoot.GetComponent<TrashSpawnManager>() ?? trashRoot.AddComponent<TrashSpawnManager>();

        // Cleanup: remove common runtime-created HUD roots so we don't end up with duplicates.
        var runtimeTrashRoot = GameObject.Find("TrashHUD_Root");
        if (runtimeTrashRoot != null && runtimeTrashRoot.GetComponent<TrashCollectionUI>() != null)
            Object.DestroyImmediate(runtimeTrashRoot);

        // Ensure one HUD canvas.
        var canvasGo = GameObject.Find("HUDCanvas");
        if (canvasGo == null)
            canvasGo = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var crt = canvasGo.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        // DayTimeUI block.
        var dayTimeUiGo = GetOrCreate(canvasGo.transform, "DayTimeUI");
        var dayTimeUi = dayTimeUiGo.GetComponent<GameDayTimeUI>() ?? dayTimeUiGo.AddComponent<GameDayTimeUI>();

        var dayTimePanel = GetOrCreate(dayTimeUiGo.transform, "DayTimePanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var dayTimePanelRt = dayTimePanel.GetComponent<RectTransform>();
        dayTimePanelRt.anchorMin = new Vector2(0.5f, 1f);
        dayTimePanelRt.anchorMax = new Vector2(0.5f, 1f);
        dayTimePanelRt.pivot = new Vector2(0.5f, 1f);
        dayTimePanelRt.anchoredPosition = new Vector2(0f, -16f);
        dayTimePanelRt.sizeDelta = new Vector2(520f, 0f);

        var v1 = dayTimePanel.GetComponent<VerticalLayoutGroup>();
        v1.childAlignment = TextAnchor.UpperCenter;
        v1.childControlHeight = true;
        v1.childControlWidth = true;
        v1.childForceExpandHeight = false;
        v1.childForceExpandWidth = true;
        v1.spacing = 2f;
        v1.padding = new RectOffset(12, 12, 8, 8);

        var fit1 = dayTimePanel.GetComponent<ContentSizeFitter>();
        fit1.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit1.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var dayTimeText = GetOrCreateTmp(dayTimePanel.transform, "DayTimeText", 34f, FontStyles.Bold);
        dayTimeText.text = "Day 1 - 10:00 AM";

        // Wire GameDayTimeUI serialized fields.
        var soDayUi = new SerializedObject(dayTimeUi);
        soDayUi.FindProperty("manager").objectReferenceValue = timeMgr;
        soDayUi.FindProperty("dayTimeLineText").objectReferenceValue = dayTimeText;
        soDayUi.FindProperty("useSingleLineDisplay").boolValue = true;
        soDayUi.FindProperty("createDefaultTextsIfMissing").boolValue = false;
        soDayUi.ApplyModifiedPropertiesWithoutUndo();

        // TrashHUD block.
        var trashHudGo = GetOrCreate(canvasGo.transform, "TrashHUD");
        var trashUi = trashHudGo.GetComponent<TrashCollectionUI>() ?? trashHudGo.AddComponent<TrashCollectionUI>();

        var trashPanel = GetOrCreate(trashHudGo.transform, "TopHUDPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        var trashPanelRt = trashPanel.GetComponent<RectTransform>();
        trashPanelRt.anchorMin = new Vector2(0.5f, 1f);
        trashPanelRt.anchorMax = new Vector2(0.5f, 1f);
        trashPanelRt.pivot = new Vector2(0.5f, 1f);
        trashPanelRt.anchoredPosition = new Vector2(0f, -70f);
        trashPanelRt.sizeDelta = new Vector2(440f, 0f);

        var img = trashPanel.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(img);
        TrashHUDFactory.ConfigurePanelBackground(img);

        var v2 = trashPanel.GetComponent<VerticalLayoutGroup>();
        v2.padding = new RectOffset(20, 20, 14, 16);
        v2.spacing = 6f;
        v2.childAlignment = TextAnchor.UpperCenter;
        v2.childControlWidth = true;
        v2.childControlHeight = true;
        v2.childForceExpandWidth = true;
        v2.childForceExpandHeight = false;

        var fit2 = trashPanel.GetComponent<ContentSizeFitter>();
        fit2.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var le = trashPanel.GetComponent<LayoutElement>();
        le.minWidth = 320f;
        le.preferredWidth = 440f;

        var dayTargetTmp = GetOrCreateTmp(trashPanel.transform, "DayTargetText", 31f, FontStyles.Bold);
        dayTargetTmp.text = "Day 1  ·  0/20";
        ApplyOutline(dayTargetTmp, 0.28f);

        var scoreTmp = GetOrCreateTmp(trashPanel.transform, "ScoreText", 21f, FontStyles.Normal);
        scoreTmp.text = "Total  0";
        ApplyOutline(scoreTmp, 0.22f);

        var soTrashUi = new SerializedObject(trashUi);
        soTrashUi.FindProperty("scoreManager").objectReferenceValue = score;
        soTrashUi.FindProperty("dailyTarget").objectReferenceValue = daily;
        soTrashUi.FindProperty("dayTargetText").objectReferenceValue = dayTargetTmp;
        soTrashUi.FindProperty("scoreText").objectReferenceValue = scoreTmp;
        soTrashUi.FindProperty("showTotalScore").boolValue = true;
        soTrashUi.FindProperty("buildRuntimeHudIfMissing").boolValue = false;
        soTrashUi.FindProperty("anchorTopRight").boolValue = false;
        soTrashUi.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject = canvasGo;
    }

    private static GameObject GetOrCreate(Transform parent, string name, params System.Type[] components)
    {
        var child = parent.Find(name);
        GameObject go;
        if (child == null)
        {
            go = components.Length > 0 ? new GameObject(name, components) : new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = child.gameObject;
            foreach (var t in components)
            {
                if (t == typeof(Transform)) continue;
                if (go.GetComponent(t) == null)
                    go.AddComponent(t);
            }
        }

        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static TextMeshProUGUI GetOrCreateTmp(Transform parent, string name, float fontSize, FontStyles style)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
            if (go.GetComponent<TextMeshProUGUI>() == null)
                go.AddComponent<TextMeshProUGUI>();
            if (go.GetComponent<LayoutElement>() == null)
                go.AddComponent<LayoutElement>();
        }

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = fontSize + 10f;
        le.preferredHeight = fontSize + 10f;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return tmp;
    }

    private static void ApplyOutline(TMP_Text tmp, float outline)
    {
        tmp.outlineWidth = outline;
        tmp.outlineColor = new Color32(0, 0, 0, 210);
    }
}
#endif

