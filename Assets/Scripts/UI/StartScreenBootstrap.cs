using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Auto-creates a simple start screen UI if none exists in the scene.
/// Student-friendly: no prefab required, works on mobile/PC.
/// </summary>
public static class StartScreenBootstrap
{
    // Background image must be loadable at runtime, so put it under:
    // Assets/Resources/6835054333_aebc4c9781_b.jpg   (Texture Type: Sprite (2D and UI))
    private const string BackgroundSpriteResourceName = "6835054333_aebc4c9781_b";
    private const string BackgroundAssetPath = "Assets/6835054333_aebc4c9781_b.jpg";

#if STARTSCREEN_RUNTIME_AUTOBUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureStartScreen()
    {
        // If user already built one in the scene, don't duplicate.
        if (Object.FindFirstObjectByType<StartScreenController>() != null)
            return;

        // Create Canvas root.
        var canvasGo = new GameObject(
            "StartScreenCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(StartScreenController));

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Ensure an EventSystem exists so the button works on PC and mobile.
        EnsureEventSystem();

        // Full-screen panel.
        var panelGo = new GameObject("StartPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var panelImg = panelGo.GetComponent<Image>();
        var bg = Resources.Load<Sprite>(BackgroundSpriteResourceName);
        if (bg != null)
        {
            panelImg.sprite = bg;
            panelImg.color = Color.white;
            panelImg.type = Image.Type.Simple;
            // Fill the entire screen (simple + no preserveAspect).
            panelImg.preserveAspect = false;
        }
        else
        {
            // Fallback: if the image is in Resources but imported as Texture2D, build a Sprite at runtime.
            var tex = Resources.Load<Texture2D>(BackgroundSpriteResourceName);
            if (tex != null)
            {
                bg = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                panelImg.sprite = bg;
                panelImg.color = Color.white;
                panelImg.type = Image.Type.Simple;
                panelImg.preserveAspect = false;
            }
            else
            {
#if UNITY_EDITOR
                // Editor-only convenience: allow leaving the image in Assets/ without Resources.
                // (In builds, Resources/ is required unless you use Addressables.)
                bg = TryLoadEditorAssetSprite();
                if (bg != null)
                {
                    panelImg.sprite = bg;
                    panelImg.color = Color.white;
                    panelImg.type = Image.Type.Simple;
                    panelImg.preserveAspect = false;
                }
                else
#endif
                {
                    panelImg.color = new Color(0f, 0f, 0f, 0.82f);
                    Debug.LogWarning(
                        $"[StartScreenBootstrap] Background sprite not found via Resources.Load('{BackgroundSpriteResourceName}'). " +
                        $"Editor path tried: '{BackgroundAssetPath}'. " +
                        $"Fix for builds: move your image to 'Assets/Resources/{BackgroundSpriteResourceName}.jpg', " +
                        $"then set Texture Type = Sprite (2D and UI) and click Apply.");
                }
            }
        }
        panelImg.raycastTarget = true;

        // Title (TMP).
        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.7f);
        titleRt.anchorMax = new Vector2(0.5f, 0.7f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900f, 140f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Desert Guardian";
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            titleTmp.font = TMP_Settings.defaultFontAsset;

        // Play button (Image + Button + TMP label).
        var btnGo = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(320f, 92f);

        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.55f, 0.95f, 1f);
        btnImg.raycastTarget = true;

        var button = btnGo.GetComponent<Button>();
        button.targetGraphic = btnImg;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "Play";
        labelTmp.fontSize = 40f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            labelTmp.font = TMP_Settings.defaultFontAsset;

        // Wire controller fields (Initialize is required because Awake already ran when the component was created).
        var controller = canvasGo.GetComponent<StartScreenController>();
        controller.Initialize(panelGo, titleTmp, button, true);
        // player left null -> StartScreenController finds by tag "Player".
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// Optional: builds the start screen permanently in EDIT mode so it stays in the Hierarchy
    /// after exiting Play Mode. Run once from Tools menu below.
    /// </summary>
    [MenuItem("Tools/Desert Guardian/Create Start Screen In Scene")]
    private static void CreateStartScreenInEditMode()
    {
        if (Object.FindFirstObjectByType<StartScreenController>() != null)
        {
            Debug.Log("[StartScreenBootstrap] Start screen already exists in this scene.", null);
            return;
        }

        // Create Canvas root.
        var canvasGo = new GameObject(
            "StartScreenCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(StartScreenController));

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EnsureEventSystem();

        // Full-screen panel.
        var panelGo = new GameObject("StartPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var panelImg = panelGo.GetComponent<Image>();
        var bg = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);
        if (bg != null)
        {
            panelImg.sprite = bg;
            panelImg.color = Color.white;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = false;
        }
        else
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundAssetPath);
            if (tex != null)
            {
                bg = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                panelImg.sprite = bg;
                panelImg.color = Color.white;
                panelImg.type = Image.Type.Simple;
                panelImg.preserveAspect = false;
            }
            else
            {
                panelImg.color = new Color(0f, 0f, 0f, 0.82f);
                Debug.LogWarning(
                    $"[StartScreenBootstrap] Could not load '{BackgroundAssetPath}'. " +
                    $"Place the image there or under Resources as '{BackgroundSpriteResourceName}'.",
                    canvasGo);
            }
        }
        panelImg.raycastTarget = true;

        // Title (TMP).
        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.7f);
        titleRt.anchorMax = new Vector2(0.5f, 0.7f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900f, 140f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Desert Guardian";
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            titleTmp.font = TMP_Settings.defaultFontAsset;

        // Play button (Image + Button + TMP label).
        var btnGo = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(320f, 92f);

        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.55f, 0.95f, 1f);
        btnImg.raycastTarget = true;

        var button = btnGo.GetComponent<Button>();
        button.targetGraphic = btnImg;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "Play";
        labelTmp.fontSize = 40f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            labelTmp.font = TMP_Settings.defaultFontAsset;

        var controller = canvasGo.GetComponent<StartScreenController>();
        controller.Initialize(panelGo, titleTmp, button, true);

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Start Screen");
        Selection.activeGameObject = canvasGo;
        Debug.Log("[StartScreenBootstrap] Created StartScreenCanvas in the scene. Save the scene (Ctrl+S).", canvasGo);
    }
#endif

#if UNITY_EDITOR
    private static Sprite TryLoadEditorAssetSprite()
    {
        // UnityEditor is not available in builds.
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);
        if (sprite != null)
            return sprite;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundAssetPath);
        if (tex == null)
            return null;

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }
#endif

    private static void EnsureEventSystem()
    {
        // Using reflection-free types: only create if missing.
        // Unity UI creates these types in UnityEngine.EventSystems namespace, but we avoid hard refs here
        // by adding components by name to keep this file simple across Unity versions.
        if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // (No reflection wiring needed anymore.)
}

