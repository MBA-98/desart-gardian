using GameDayTime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD bar + optional numeric label bound to <see cref="PlayerHealth"/>,
/// plus Game Over overlay (assign in scene or built at runtime with the health bar).
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private TMP_Text healthText;

    [Header("Game Over")]
    [Tooltip("Shown when health reaches 0. If empty, a default overlay is created under the same Canvas as the health bar.")]
    [SerializeField]
    private GameObject gameOverRoot;

    [Tooltip("Optional: assign TMP for 'Game Over' text. If null but gameOverRoot is set, first TMP under root is used.")]
    [SerializeField]
    private TMP_Text gameOverText;

    [Tooltip("Optional: assign a Button for Play Again. If null, a default button is created with the runtime Game Over panel.")]
    [SerializeField]
    private Button playAgainButton;

    [Tooltip("If references are missing, builds a small bar at top-left in Play Mode.")]
    [SerializeField]
    private bool buildRuntimeHudIfMissing = true;

    [SerializeField]
    private bool anchorTopLeft = true;

    [Header("Health bar layout (auto-built panel + Apply menu)")]
    [Tooltip("Anchored position of HealthBarPanel. Top-left default: (24, -24). Top-center: use e.g. (0, -24).")]
    [SerializeField]
    private Vector2 healthBarAnchoredPosition = new Vector2(24f, -24f);

    [SerializeField]
    private Vector2 healthBarSizeDelta = new Vector2(280f, 36f);

    private bool _builtPlayAgainListener;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = PlayerHealth.FindOnPlayer();

        if (buildRuntimeHudIfMissing && fillImage == null)
            BuildRuntimeHud();
        else
            TryApplyHealthBarPanelLayout();

        EnsureGameOverUI();
        WirePlayAgainButton();
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = PlayerHealth.FindOnPlayer();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            playerHealth.OnDied += OnPlayerDied;
            OnHealthChanged(playerHealth.Current, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
            playerHealth.OnDied -= OnPlayerDied;
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (fillImage != null)
        {
            float t = max > 0.001f ? Mathf.Clamp01(current / max) : 0f;
            fillImage.fillAmount = t;
            fillImage.color = Color.Lerp(new Color(0.85f, 0.2f, 0.18f), new Color(0.25f, 0.85f, 0.35f), t);
        }

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnPlayerDied()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
            gameOverRoot.transform.SetAsLastSibling();
            return;
        }

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.transform.SetAsLastSibling();
        }
    }

    /// <summary>Used after auto-respawn at main home so the overlay does not stay visible.</summary>
    public void HideGameOverUI()
    {
        if (gameOverRoot != null)
            gameOverRoot.SetActive(false);
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }

    private void EnsureGameOverUI()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(false);
            if (gameOverText == null)
                gameOverText = gameOverRoot.GetComponentInChildren<TMP_Text>(true);
            if (playAgainButton == null)
                playAgainButton = gameOverRoot.GetComponentInChildren<Button>(true);
            return;
        }

        Canvas canvas = null;
        if (fillImage != null)
            canvas = fillImage.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null && buildRuntimeHudIfMissing)
        {
            BuildRuntimeHud();
            if (fillImage != null)
                canvas = fillImage.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
            return;

        var overlay = new GameObject("GameOverOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var dim = overlay.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(dim);
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;

        var content = new GameObject("GameOverContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(overlay.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(720f, 420f);
        var v = content.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 28f;
        v.padding = new RectOffset(24, 24, 24, 24);
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var textGo = new GameObject("GameOverText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textGo.transform.SetParent(content.transform, false);
        gameOverText = textGo.GetComponent<TextMeshProUGUI>();
        var titleLe = textGo.GetComponent<LayoutElement>();
        titleLe.preferredHeight = 120f;
        var trt = textGo.GetComponent<RectTransform>();
        trt.sizeDelta = new Vector2(680f, 120f);
        gameOverText.text = "Game Over";
        gameOverText.fontSize = 72f;
        gameOverText.fontStyle = FontStyles.Bold;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = new Color(1f, 0.35f, 0.3f, 1f);
        gameOverText.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            gameOverText.font = TMP_Settings.defaultFontAsset;
        gameOverText.outlineWidth = 0.35f;
        gameOverText.outlineColor = new Color32(0, 0, 0, 255);

        var btnGo = new GameObject("PlayAgainButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnGo.transform.SetParent(content.transform, false);
        var btnLe = btnGo.GetComponent<LayoutElement>();
        btnLe.preferredWidth = 280f;
        btnLe.preferredHeight = 56f;
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(280f, 56f);
        var btnImg = btnGo.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(btnImg);
        btnImg.color = new Color(0.2f, 0.55f, 0.95f, 1f);
        playAgainButton = btnGo.GetComponent<Button>();
        playAgainButton.targetGraphic = btnImg;

        var btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var btnLabelRt = btnLabelGo.GetComponent<RectTransform>();
        btnLabelRt.anchorMin = Vector2.zero;
        btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = Vector2.zero;
        btnLabelRt.offsetMax = Vector2.zero;
        var btnTmp = btnLabelGo.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Play Again";
        btnTmp.fontSize = 28f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        btnTmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            btnTmp.font = TMP_Settings.defaultFontAsset;

        gameOverRoot = overlay;
        gameOverRoot.SetActive(false);
    }

    private void WirePlayAgainButton()
    {
        if (playAgainButton == null && gameOverRoot != null)
            playAgainButton = gameOverRoot.GetComponentInChildren<Button>(true);
        if (playAgainButton == null || _builtPlayAgainListener)
            return;

        playAgainButton.onClick.RemoveListener(HandlePlayAgain);
        playAgainButton.onClick.AddListener(HandlePlayAgain);
        _builtPlayAgainListener = true;
    }

    private void HandlePlayAgain()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<PlayerMainHomeRespawn>(out var homeRespawn))
        {
            homeRespawn.RespawnNow();
            return;
        }

        if (gameOverRoot != null)
            gameOverRoot.SetActive(false);

        var dayMgr = FindFirstObjectByType<GameDayTimeManager>();
        dayMgr?.ResetToStartingDay();

        var score = FindFirstObjectByType<ScoreManager>();
        score?.ResetScore();

        foreach (var milestones in FindObjectsByType<TrashCollectionMilestones>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            milestones.ResetMilestoneTracking();

        if (playerHealth == null)
            playerHealth = PlayerHealth.FindOnPlayer();
        playerHealth?.Revive();

        if (player != null && player.TryGetComponent<PlayerGameplayLock>(out var locker))
            locker.RestoreGameplay();

        foreach (var dayUi in FindObjectsByType<GameDayTimeUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            dayUi.RefreshFromManager();

        foreach (var trashUi in FindObjectsByType<TrashCollectionUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            trashUi.RefreshHud();
    }

    /// <summary>Hierarchy: auto HUD is under this object → <c>PlayerHealthHUD_Canvas</c> → <c>HealthBarPanel</c>.</summary>
    [ContextMenu("Apply health bar layout (RectTransform)")]
    private void ContextApplyHealthBarLayout()
    {
        TryApplyHealthBarPanelLayout();
    }

    private void TryApplyHealthBarPanelLayout()
    {
        var panelRt = FindHealthBarPanelRect();
        if (panelRt == null)
            return;

        if (anchorTopLeft)
        {
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
        }
        else
        {
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
        }

        panelRt.anchoredPosition = healthBarAnchoredPosition;
        panelRt.sizeDelta = healthBarSizeDelta;
    }

    private RectTransform FindHealthBarPanelRect()
    {
        if (fillImage == null)
            return null;
        var t = fillImage.transform.parent?.parent;
        if (t == null || t.name != "HealthBarPanel")
            return null;
        return t.GetComponent<RectTransform>();
    }

    private void BuildRuntimeHud()
    {
        Transform panelParent;
        if (GetComponent<Canvas>() != null)
        {
            panelParent = transform;
        }
        else
        {
            var canvasGo = new GameObject("PlayerHealthHUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
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

        var panel = new GameObject("HealthBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        panel.transform.SetParent(panelParent, false);
        var rt = panel.GetComponent<RectTransform>();
        if (anchorTopLeft)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }

        rt.anchoredPosition = healthBarAnchoredPosition;
        rt.sizeDelta = healthBarSizeDelta;

        var bg = panel.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(bg);
        TrashHUDFactory.ConfigurePanelBackground(bg);
        bg.raycastTarget = false;

        var h = panel.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 10, 6, 6);
        h.spacing = 10f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = false;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        var le = panel.GetComponent<LayoutElement>();
        le.minWidth = 260f;
        le.preferredWidth = 280f;
        le.minHeight = 36f;

        var barBgGo = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        barBgGo.transform.SetParent(panel.transform, false);
        var barBg = barBgGo.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(barBg);
        barBg.color = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        barBg.raycastTarget = false;
        var barBgRt = barBgGo.GetComponent<RectTransform>();
        barBgRt.sizeDelta = new Vector2(180f, 22f);
        var barBgLe = barBgGo.GetComponent<LayoutElement>();
        barBgLe.preferredWidth = 180f;
        barBgLe.preferredHeight = 22f;
        barBgLe.flexibleWidth = 0f;

        var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(barBgGo.transform, false);
        fillImage = fillGo.GetComponent<Image>();
        TrashHUDFactory.AssignPanelSprite(fillImage);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = new Color(0.3f, 0.82f, 0.4f, 1f);
        fillImage.raycastTarget = false;
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var tmpGo = new GameObject("HealthText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        tmpGo.transform.SetParent(panel.transform, false);
        healthText = tmpGo.GetComponent<TextMeshProUGUI>();
        healthText.fontSize = 20f;
        healthText.fontStyle = FontStyles.Bold;
        healthText.color = new Color(0.95f, 0.96f, 1f);
        healthText.alignment = TextAlignmentOptions.MidlineLeft;
        healthText.raycastTarget = false;
        healthText.text = "100 / 100";
        if (TMP_Settings.defaultFontAsset != null)
            healthText.font = TMP_Settings.defaultFontAsset;
        healthText.outlineWidth = 0.22f;
        healthText.outlineColor = new Color32(0, 0, 0, 200);
        var tmpLe = tmpGo.GetComponent<LayoutElement>();
        tmpLe.preferredWidth = 90f;
        tmpLe.flexibleWidth = 0f;
    }
}
