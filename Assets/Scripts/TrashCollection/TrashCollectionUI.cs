using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top HUD: day + trash progress (and optional total). Wired to <see cref="ScoreManager"/> and <see cref="DailyTargetSystem"/>.
/// </summary>
public class TrashCollectionUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField]
    private ScoreManager scoreManager;

    [SerializeField]
    private DailyTargetSystem dailyTarget;

    [Header("UI (assign or leave empty to build at runtime)")]
    [SerializeField]
    private TMP_Text dayTargetText;

    [SerializeField]
    private TMP_Text scoreText;

    [Tooltip("Second line: lifetime trash collected.")]
    [SerializeField]
    private bool showTotalScore = true;

    [Tooltip("If no TMP refs, builds TrashHUD_Canvas / TopHUDPanel in Play Mode.")]
    [SerializeField]
    private bool buildRuntimeHudIfMissing = false;

    [Header("Layout (runtime build)")]
    [SerializeField]
    private bool anchorTopRight;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
        if (dailyTarget == null)
            dailyTarget = FindFirstObjectByType<DailyTargetSystem>();

        if (buildRuntimeHudIfMissing && dayTargetText == null)
            BuildPolishedHudRuntime();
    }

    private void Start()
    {
        // Re-bind after all Awakes so scene target (e.g. 20) shows on first frame, not an old placeholder.
        if (dailyTarget == null)
            dailyTarget = FindFirstObjectByType<DailyTargetSystem>();
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
        Refresh();
    }

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged += Refresh;
        if (dailyTarget != null)
            dailyTarget.ProgressChanged += Refresh;

        Refresh();
    }

    /// <summary>Refresh lines from managers (e.g. after Play Again).</summary>
    public void RefreshHud()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged -= Refresh;
        if (dailyTarget != null)
            dailyTarget.ProgressChanged -= Refresh;
    }

    /// <summary>Live HUD: reads day + progress from <see cref="DailyTargetSystem"/>, total from <see cref="ScoreManager"/>.</summary>
    private void Refresh()
    {
        if (dayTargetText == null)
            return;

        if (dailyTarget == null || scoreManager == null)
        {
            dayTargetText.text = "—";
            if (scoreText != null)
                scoreText.gameObject.SetActive(false);
            return;
        }

        // Primary line — quick read: Day 1 · 12/20
        dayTargetText.text =
            $"Day {dailyTarget.CurrentDay}  ·  {dailyTarget.ProgressToday}/{dailyTarget.TargetToday}";

        if (scoreText != null)
        {
            if (showTotalScore)
            {
                scoreText.gameObject.SetActive(true);
                scoreText.text = $"Points  {scoreManager.CurrentScore}";
            }
            else
            {
                scoreText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Creates overlay canvas (or panel under this Canvas), semi-transparent panel, styled TMP.</summary>
    private void BuildPolishedHudRuntime()
    {
        Transform panelParent;
        if (GetComponent<Canvas>() != null)
        {
            panelParent = transform;
        }
        else
        {
            var canvasGo = new GameObject("TrashHUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
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
            panelParent = canvasGo.transform;
        }

        var panel = CreatePanel(panelParent, "TopHUDPanel", anchorTopRight);
        dayTargetText = CreateStyledTmp(panel.transform, "DayTargetText", 31f, FontStyles.Bold, new Color(0.98f, 0.98f, 1f));
        scoreText = CreateStyledTmp(panel.transform, "ScoreText", 21f, FontStyles.Normal, new Color(0.78f, 0.82f, 0.88f));

        ApplyTmpReadability(dayTargetText, 0.28f);
        ApplyTmpReadability(scoreText, 0.22f);
    }

    private RectTransform CreatePanel(Transform parent, string name, bool topRight)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        if (topRight)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
        }

        rt.sizeDelta = new Vector2(420f, 0f);

        var img = panel.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(img);
        TrashHUDFactory.ConfigurePanelBackground(img);

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

        var le = panel.GetComponent<LayoutElement>();
        le.minWidth = 320f;
        le.preferredWidth = 440f;

        return rt;
    }

    private static TMP_Text CreateStyledTmp(Transform parent, string name, float size, FontStyles style, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = size + 10f;
        le.preferredHeight = size + 10f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = c;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.text = name.Contains("Score") ? "Points  0" : "Day 1  ·  0/20";
        var hudFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        if (hudFont != null)
            tmp.font = hudFont;
        return tmp;
    }

    private static void ApplyTmpReadability(TMP_Text tmp, float outline)
    {
        if (tmp == null)
            return;
        tmp.outlineWidth = outline;
        tmp.outlineColor = new Color32(0, 0, 0, 210);
    }
}

/// <summary>Shared sprite + colors for editor + runtime HUD panel.</summary>
public static class TrashHUDFactory
{
    private static Sprite s_WhiteSprite;

    public static void AssignPanelSprite(Image image)
    {
        if (image == null)
            return;
        if (s_WhiteSprite == null)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            s_WhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        }

        image.sprite = s_WhiteSprite;
        image.type = Image.Type.Simple;
    }

    public static void ConfigurePanelBackground(Image image)
    {
        if (image == null)
            return;
        image.color = new Color(0.07f, 0.08f, 0.11f, 0.86f);
        image.raycastTarget = false;
    }
}
