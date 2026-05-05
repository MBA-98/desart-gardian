using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDayTime
{
    public enum DayTimeBarAnchor
    {
        TopCenter,
        TopRight
    }

    /// <summary>
    /// Binds UI text to <see cref="GameDayTimeManager"/> via events (no per-frame Update).
    /// Leave references empty to spawn default labels at the top of the screen at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class GameDayTimeUI : MonoBehaviour
    {
        [SerializeField]
        private GameDayTimeManager manager;

        [Tooltip("Single line, e.g. \"Day 1 - 08:30 AM\" — preferred HUD layout.")]
        [SerializeField]
        private TMP_Text dayTimeLineText;

        [SerializeField]
        private TMP_Text dayText;

        [SerializeField]
        private TMP_Text timeText;

        [Tooltip("When true, dayTimeLineText shows combined day + time; separate labels optional.")]
        [SerializeField]
        private bool useSingleLineDisplay = true;

        [Tooltip("If text fields are missing, create them automatically at runtime.")]
        [SerializeField]
        private bool createDefaultTextsIfMissing = false;

        [Tooltip("Use 24-hour display (e.g. 20:45) instead of 12-hour AM/PM.")]
        [SerializeField]
        private bool use24HourFormat;

        [SerializeField]
        private string dayFormat = "Day {0}";

        [SerializeField]
        private float fontSize = 28f;

        [SerializeField]
        private DayTimeBarAnchor barAnchor = DayTimeBarAnchor.TopCenter;

        private void Awake()
        {
            if (manager == null)
                manager = FindFirstObjectByType<GameDayTimeManager>();

            if (!createDefaultTextsIfMissing)
                return;

            if (useSingleLineDisplay && dayTimeLineText == null)
                BuildSingleLineDefault();
            else if (!useSingleLineDisplay && (dayText == null || timeText == null))
                BuildDefaultLabels();
        }

        // Runtime UI build: overlay canvas + top bar + one TMP row (DayTimeText).
        private void BuildSingleLineDefault()
        {
            Transform contentParent;

            if (GetComponentInParent<Canvas>() == null)
            {
                var canvasGo = new GameObject("DayTimeUI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
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

                contentParent = CreateTopBar(canvasGo.transform, barAnchor);
            }
            else
            {
                contentParent = CreateTopBar(transform, barAnchor);
            }

            dayTimeLineText = CreateTmpRow(contentParent, "DayTimeText", fontSize + 6f);
            ApplyFontDefaults(dayTimeLineText);
            ApplyReadability(dayTimeLineText);
        }

        private void BuildDefaultLabels()
        {
            Transform contentParent;

            if (GetComponentInParent<Canvas>() == null)
            {
                var canvasGo = new GameObject("DayTimeUI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
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

                contentParent = CreateTopBar(canvasGo.transform, barAnchor);
            }
            else
            {
                contentParent = CreateTopBar(transform, barAnchor);
            }

            if (dayText == null)
                dayText = CreateTmpRow(contentParent, "DayLabel", fontSize + 4f);
            if (timeText == null)
                timeText = CreateTmpRow(contentParent, "TimeLabel", fontSize);

            ApplyFontDefaults(dayText);
            ApplyFontDefaults(timeText);
            ApplyReadability(dayText);
            ApplyReadability(timeText);
        }

        private static Transform CreateTopBar(Transform parent, DayTimeBarAnchor anchor)
        {
            var panel = new GameObject("DayTimePanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            if (anchor == DayTimeBarAnchor.TopRight)
            {
                prt.anchorMin = new Vector2(1f, 1f);
                prt.anchorMax = new Vector2(1f, 1f);
                prt.pivot = new Vector2(1f, 1f);
                prt.anchoredPosition = new Vector2(-16f, -16f);
            }
            else
            {
                prt.anchorMin = new Vector2(0.5f, 1f);
                prt.anchorMax = new Vector2(0.5f, 1f);
                prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -16f);
            }

            prt.sizeDelta = new Vector2(520f, 0f);

            var v = panel.GetComponent<VerticalLayoutGroup>();
            v.childAlignment = anchor == DayTimeBarAnchor.TopRight ? TextAnchor.UpperRight : TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.spacing = 2f;
            v.padding = new RectOffset(12, 12, 8, 8);

            var fit = panel.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            return panel.transform;
        }

        private static TMP_Text CreateTmpRow(Transform parent, string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = size + 8f;
            le.preferredHeight = size + 8f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void ApplyFontDefaults(TMP_Text tmp)
        {
            if (tmp == null)
                return;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
        }

        // Dark outline + white fill so text stays readable on bright or dark backgrounds.
        private static void ApplyReadability(TMP_Text tmp)
        {
            if (tmp == null)
                return;
            tmp.enableAutoSizing = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
        }

        private void OnEnable()
        {
            if (manager == null)
                manager = FindFirstObjectByType<GameDayTimeManager>();

            if (manager != null)
            {
                manager.DayNumberChanged += OnDayNumberChanged;
                manager.ClockDisplayChanged += OnClockDisplayChanged;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.DayNumberChanged -= OnDayNumberChanged;
                manager.ClockDisplayChanged -= OnClockDisplayChanged;
            }
        }

        private void OnDayNumberChanged(int _)
        {
            RefreshAll();
        }

        private void OnClockDisplayChanged()
        {
            RefreshAll();
        }

        // Pushes strings to TMP: combined line and/or split labels from manager.CurrentDay + clock time.
        private void RefreshAll()
        {
            if (manager == null)
                return;

            // If you prefer a persistent, scene-authored UI, assign TMP references in the inspector and keep
            // createDefaultTextsIfMissing disabled.

            if (useSingleLineDisplay && dayTimeLineText != null)
            {
                string timeStr = use24HourFormat
                    ? GameDayTimeManager.FormatTime24Hour(manager.CurrentTimeMinutesFromMidnight)
                    : GameDayTimeManager.FormatTime12Hour(manager.CurrentTimeMinutesFromMidnight);
                string daySegment = manager.MaxDay > 0
                    ? $"Day {manager.CurrentDay}/{manager.MaxDay}"
                    : $"Day {manager.CurrentDay}";
                dayTimeLineText.text = $"{daySegment} - {timeStr}";
            }

            if (dayText != null)
                dayText.text = manager.MaxDay > 0
                    ? $"Day {manager.CurrentDay}/{manager.MaxDay}"
                    : string.Format(dayFormat, manager.CurrentDay);

            if (timeText != null)
            {
                timeText.text = use24HourFormat
                    ? GameDayTimeManager.FormatTime24Hour(manager.CurrentTimeMinutesFromMidnight)
                    : GameDayTimeManager.FormatTime12Hour(manager.CurrentTimeMinutesFromMidnight);
            }
        }

        /// <summary>Immediate refresh from the manager (e.g. after sleep) if events did not fire same frame.</summary>
        public void RefreshFromManager()
        {
            RefreshAll();
        }
    }
}
