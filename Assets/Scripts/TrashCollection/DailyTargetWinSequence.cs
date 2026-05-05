using System.Collections;
using GameDayTime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// After the <b>final campaign day</b> (<see cref="GameDayTimeManager.MaxDay"/>), when the daily trash target
/// is completed, shows win UI then reloads the scene. Earlier days completing the target do not end the game.
/// </summary>
public class DailyTargetWinSequence : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Leave empty to find a DailyTargetSystem in the scene.")]
    private DailyTargetSystem dailyTarget;

    [SerializeField]
    [Tooltip("Leave empty to find GameDayTimeManager. Used with Max Day to know the last campaign day.")]
    private GameDayTimeManager dayTimeManager;

    [SerializeField]
    private float winDisplaySeconds = 10f;

    [SerializeField]
    private string winMessage = "YOU WIN";

    [SerializeField]
    [Tooltip("Canvas sort order — keep above normal HUD.")]
    private int overlaySortingOrder = 5000;

    [SerializeField]
    [Tooltip("Freeze gameplay during the win overlay (timeScale = 0). Timer still uses real seconds.")]
    private bool freezeTimeDuringWin = true;

    [Header("TMP visibility (if text is invisible: assign font here)")]
    [SerializeField]
    [Tooltip("Optional. If set, used first — fixes missing text when Resources.Load fails in a build.")]
    private TMP_FontAsset winFontOverride;

    [SerializeField]
    [Tooltip("Win line size (large = always readable on screen).")]
    private float winFontSize = 100f;

    private bool _sequenceStarted;
    /// <summary>Root holding two canvases: black bg (lower sort) + text (higher sort) so text never draws underneath.</summary>
    private GameObject _overlayRoot;
    private float _savedTimeScale = 1f;

    private void Awake()
    {
        if (dailyTarget == null)
            dailyTarget = FindFirstObjectByType<DailyTargetSystem>();
        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();
    }

    private void OnEnable()
    {
        if (dailyTarget != null)
            dailyTarget.ProgressChanged += OnDailyProgressChanged;
    }

    private void OnDisable()
    {
        if (dailyTarget != null)
            dailyTarget.ProgressChanged -= OnDailyProgressChanged;
    }

    private void OnDailyProgressChanged()
    {
        if (_sequenceStarted || dailyTarget == null || !dailyTarget.IsTargetComplete)
            return;

        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();

        // Win only on the last campaign day (e.g. day 10 of 10), not the first time you hit 20 trash.
        if (dayTimeManager == null || dayTimeManager.MaxDay <= 0)
            return;

        if (dailyTarget.CurrentDay != dayTimeManager.MaxDay)
            return;

        _sequenceStarted = true;
        StartCoroutine(WinThenReloadRoutine());
    }

    private IEnumerator WinThenReloadRoutine()
    {
        if (freezeTimeDuringWin)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        BuildWinOverlay();

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, winDisplaySeconds));

        if (freezeTimeDuringWin)
            Time.timeScale = Mathf.Approximately(_savedTimeScale, 0f) ? 1f : _savedTimeScale;

        var idx = SceneManager.GetActiveScene().buildIndex;
        if (idx >= 0)
            SceneManager.LoadScene(idx);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    private void BuildWinOverlay()
    {
        if (_overlayRoot != null)
            return;

        _overlayRoot = new GameObject("WinOverlayRoot");

        static void StretchToFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void ConfigureHudScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 1) Full-screen black — its own canvas with base sorting order.
        var bgCanvasGo = new GameObject("WinBgCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        bgCanvasGo.transform.SetParent(_overlayRoot.transform, false);
        StretchToFullScreen(bgCanvasGo.GetComponent<RectTransform>());
        var bgCanvas = bgCanvasGo.GetComponent<Canvas>();
        bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bgCanvas.sortingOrder = overlaySortingOrder;
        ConfigureHudScaler(bgCanvasGo.GetComponent<CanvasScaler>());

        var blackGo = new GameObject("BlackFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        blackGo.transform.SetParent(bgCanvasGo.transform, false);
        StretchToFullScreen(blackGo.GetComponent<RectTransform>());
        var blackImg = blackGo.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(blackImg);
        blackImg.color = Color.black;
        blackImg.raycastTarget = true;

        // 2) Text: separate Canvas + higher sortingOrder + last sibling so it never draws under the black layer.
        var textCanvasGo = new GameObject("WinTextCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        textCanvasGo.transform.SetParent(_overlayRoot.transform, false);
        textCanvasGo.transform.SetAsLastSibling();
        StretchToFullScreen(textCanvasGo.GetComponent<RectTransform>());
        var textCanvas = textCanvasGo.GetComponent<Canvas>();
        textCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        textCanvas.overrideSorting = true;
        textCanvas.sortingOrder = overlaySortingOrder + 10;
        ConfigureHudScaler(textCanvasGo.GetComponent<CanvasScaler>());

        var textGo = new GameObject("WinText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(textCanvasGo.transform, false);
        textGo.transform.SetAsLastSibling();
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(1800f, 600f);

        var tmp = textGo.GetComponent<TextMeshProUGUI>();

        // ① Font — without this, TMP often renders nothing (black screen only).
        var font = ResolveWinFont();
        if (font != null)
            tmp.font = font;
        else
            Debug.LogError("[DailyTargetWinSequence] No TMP font for win text. Assign Win Font Override on this component, or keep LiberationSans under a Resources folder.", this);

        // Order matters: set string after font when possible.
        tmp.text = winMessage;
        tmp.fontSize = winFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.alpha = 1f;
        tmp.outlineWidth = 0.35f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        tmp.ForceMeshUpdate(true);
        Canvas.ForceUpdateCanvases();
    }

    private TMP_FontAsset ResolveWinFont()
    {
        if (winFontOverride != null)
            return winFontOverride;

        var fromResources = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fromResources != null)
            return fromResources;

        return TMP_Settings.defaultFontAsset;
    }
}
