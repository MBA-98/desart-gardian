using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameDayTime
{
    /// <summary>
    /// Bed interaction with <see cref="GameDayTimeManager"/>:
    /// - PC: prompt when in range + key (default E).
    /// - Mobile / Tablet: touch button when in range only.
    /// Button and text are not shown together; mode follows platform or settings.
    /// </summary>
    public class BedSleepInteractable : MonoBehaviour
    {
        public enum SleepUiPresentationMode
        {
            Auto,
            KeyboardPromptOnly,
            TouchButtonOnly
        }

        [Header("Reference")]
        [SerializeField]
        private GameDayTimeManager dayTimeManager;

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLogs;

        [Header("Player detection")]
        [FormerlySerializedAs("useTriggerAndKey")]
        [SerializeField]
        private bool useTrigger = true;

        [Tooltip("Player tag. Empty = any collider.")]
        [SerializeField]
        private string playerTag = "Player";

        [Header("UI mode")]
        [SerializeField]
        private SleepUiPresentationMode presentationMode = SleepUiPresentationMode.Auto;

#if UNITY_EDITOR
        [Tooltip("Editor only: force touch UI mode.")]
        [SerializeField]
        private bool forceTouchUiInEditor;

        [Tooltip("Editor only: force keyboard UI mode.")]
        [SerializeField]
        private bool forceKeyboardUiInEditor;
#endif

        [Header("Keyboard (PC)")]
        [SerializeField]
        private KeyCode sleepKey = KeyCode.E;

        [Tooltip("Format string; {0} is the key name.")]
        [SerializeField]
        private string keyboardPromptReadyFormat = "Press {0} to Sleep";

        [SerializeField]
        private string keyboardPromptWaitFormat = "Rest opens after the day ends (7 PM)";

        [Header("Touch (Mobile / Tablet)")]
        [SerializeField]
        private string mobileButtonReadyLabel = "Sleep";

        [SerializeField]
        private string mobileButtonWaitLabel = "Wait until evening";

        [Header("Custom UI (optional)")]
        [Tooltip("Root object toggled for the keyboard prompt, or leave empty and assign text only.")]
        [SerializeField]
        private GameObject customKeyboardPromptRoot;

        [SerializeField]
        private TMP_Text customKeyboardPromptText;

        [Tooltip("Full button root (preferably includes a Button).")]
        [SerializeField]
        private GameObject customMobileButtonRoot;

        [SerializeField]
        private Button customMobileSleepButton;

        [SerializeField]
        private TMP_Text customMobileButtonLabel;

        [Header("Auto generation")]
        [Tooltip("If no custom UI is assigned, a Canvas is created at runtime.")]
        [SerializeField]
        private bool autoCreateInteractionUi = true;

        [SerializeField]
        private int interactionCanvasSortOrder = 105;

        private bool _playerInRange;

        /// <summary>Skip TMP/layout work when <see cref="GameDayTimeManager.CanSleepInBedNow"/> did not change since last paint.</summary>
        private bool _lastPaintedCanSleep;
        private bool _hasPaintedSleepPromptOnce;

        private GameObject _runtimeRoot;
        private GameObject _keyboardBlock;
        private TMP_Text _runtimeKeyboardText;
        private GameObject _mobileBlock;
        private Button _runtimeMobileButton;
        private TMP_Text _runtimeMobileLabel;

        private bool UsesRuntimeHud => !HasCustomUi() && autoCreateInteractionUi;

        private void Awake()
        {
            if (dayTimeManager == null)
                dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();

            if (customMobileSleepButton != null)
                customMobileSleepButton.onClick.AddListener(OnMobileSleepButtonClicked);

            if (HasCustomUi())
            {
                SetKeyboardHostActive(false);
                SetMobileHostActive(false);
            }
            else if (autoCreateInteractionUi)
            {
                BuildRuntimeHud();
            }
        }

        private bool HasCustomUi()
        {
            return customKeyboardPromptRoot != null || customMobileButtonRoot != null
                                                   || customKeyboardPromptText != null
                                                   || customMobileSleepButton != null
                                                   || customMobileButtonLabel != null;
        }

        private void BuildRuntimeHud()
        {
            var canvasGo = new GameObject("BedSleep_InteractionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _runtimeRoot = canvasGo;
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = interactionCanvasSortOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            _keyboardBlock = new GameObject("KeyboardPrompt", typeof(RectTransform));
            _keyboardBlock.transform.SetParent(canvasGo.transform, false);
            var krt = _keyboardBlock.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(0.5f, 0f);
            krt.anchorMax = new Vector2(0.5f, 0f);
            krt.pivot = new Vector2(0.5f, 0f);
            krt.anchoredPosition = new Vector2(0f, 140f);
            krt.sizeDelta = new Vector2(900f, 56f);

            _runtimeKeyboardText = _keyboardBlock.AddComponent<TextMeshProUGUI>();
            _runtimeKeyboardText.alignment = TextAlignmentOptions.Center;
            _runtimeKeyboardText.fontSize = 32f;
            _runtimeKeyboardText.color = Color.white;
            _runtimeKeyboardText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                _runtimeKeyboardText.font = TMP_Settings.defaultFontAsset;

            _mobileBlock = new GameObject("MobileSleepButton", typeof(RectTransform));
            _mobileBlock.transform.SetParent(canvasGo.transform, false);
            var mrt = _mobileBlock.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0f);
            mrt.anchorMax = new Vector2(0.5f, 0f);
            mrt.pivot = new Vector2(0.5f, 0f);
            mrt.anchoredPosition = new Vector2(0f, 160f);
            mrt.sizeDelta = new Vector2(420f, 96f);

            var img = _mobileBlock.AddComponent<Image>();
            img.color = new Color(0.15f, 0.45f, 0.25f, 0.95f);
            img.raycastTarget = true;

            _runtimeMobileButton = _mobileBlock.AddComponent<Button>();
            _runtimeMobileButton.targetGraphic = img;
            _runtimeMobileButton.onClick.AddListener(OnMobileSleepButtonClicked);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(_mobileBlock.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _runtimeMobileLabel = textGo.AddComponent<TextMeshProUGUI>();
            _runtimeMobileLabel.alignment = TextAlignmentOptions.Center;
            _runtimeMobileLabel.fontSize = 36f;
            _runtimeMobileLabel.color = Color.white;
            _runtimeMobileLabel.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                _runtimeMobileLabel.font = TMP_Settings.defaultFontAsset;

            _runtimeRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (customMobileSleepButton != null)
                customMobileSleepButton.onClick.RemoveListener(OnMobileSleepButtonClicked);
            if (_runtimeMobileButton != null)
                _runtimeMobileButton.onClick.RemoveListener(OnMobileSleepButtonClicked);
        }

        private void Update()
        {
            if (!useTrigger || dayTimeManager == null)
                return;

            if (!UseKeyboardPath())
                return;

            if (!_playerInRange)
                return;

            if (WasSleepPressedThisFrame())
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[BedSleepInteractable] Sleep key pressed. " +
                        $"CanSleepNow={dayTimeManager.CanSleepInBedNow} " +
                        $"IsWaitingForSleep={dayTimeManager.IsWaitingForSleep} " +
                        $"RequireSleepToAdvanceDay={(dayTimeManager.CanSleepInBedNow || dayTimeManager.IsWaitingForSleep)}",
                        this);
                }
                TrySleep();
            }
        }

        private bool WasSleepPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            // Works when Active Input Handling = "Input System Package (New)" or "Both".
            if (Keyboard.current != null)
            {
                if (sleepKey switch
                    {
                        KeyCode.E => Keyboard.current.eKey.wasPressedThisFrame,
                        KeyCode.F => Keyboard.current.fKey.wasPressedThisFrame,
                        KeyCode.Space => Keyboard.current.spaceKey.wasPressedThisFrame,
                        KeyCode.Return => Keyboard.current.enterKey.wasPressedThisFrame,
                        _ => false
                    })
                    return true;
            }
#endif
            return Input.GetKeyDown(sleepKey);
        }

        private void LateUpdate()
        {
            if (!useTrigger || !_playerInRange || dayTimeManager == null)
                return;

            bool canSleep = dayTimeManager.CanSleepInBedNow;
            if (_hasPaintedSleepPromptOnce && canSleep == _lastPaintedCanSleep)
                return;

            _lastPaintedCanSleep = canSleep;
            _hasPaintedSleepPromptOnce = true;
            RefreshPromptContent(canSleep);
        }

        private void RefreshPromptContent(bool canSleep)
        {
            if (UseKeyboardPath())
            {
                string keyName = sleepKey.ToString();
                string msg = canSleep
                    ? string.Format(keyboardPromptReadyFormat, keyName)
                    : keyboardPromptWaitFormat;

                if (customKeyboardPromptText != null)
                    customKeyboardPromptText.text = msg;
                if (_runtimeKeyboardText != null)
                    _runtimeKeyboardText.text = msg;
            }
            else
            {
                string label = canSleep ? mobileButtonReadyLabel : mobileButtonWaitLabel;
                if (customMobileButtonLabel != null)
                    customMobileButtonLabel.text = label;
                if (_runtimeMobileLabel != null)
                    _runtimeMobileLabel.text = label;

                if (customMobileSleepButton != null)
                    customMobileSleepButton.interactable = canSleep;
                if (_runtimeMobileButton != null)
                    _runtimeMobileButton.interactable = canSleep;
            }
        }

        private bool UseKeyboardPath()
        {
#if UNITY_EDITOR
            if (forceKeyboardUiInEditor)
                return true;
            if (forceTouchUiInEditor)
                return false;
#endif
            return presentationMode switch
            {
                SleepUiPresentationMode.KeyboardPromptOnly => true,
                SleepUiPresentationMode.TouchButtonOnly => false,
                _ => !Application.isMobilePlatform
            };
        }

        private void OnMobileSleepButtonClicked()
        {
            if (!_playerInRange || dayTimeManager == null)
                return;

            TrySleep();
        }

        /// <summary>Called from a custom UI button or from script.</summary>
        public void TrySleep()
        {
            if (dayTimeManager == null)
            {
                Debug.LogWarning("[BedSleepInteractable] No GameDayTimeManager assigned or found.", this);
                return;
            }

            bool ok = dayTimeManager.SleepInBed();
            if (ok)
            {
                if (enableDebugLogs)
                    Debug.Log("[BedSleepInteractable] Sleep succeeded. Advanced to next day.", this);
                var hud = FindFirstObjectByType<GameDayTimeUI>();
                hud?.RefreshFromManager();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[BedSleepInteractable] Sleep blocked. CanSleepNow={dayTimeManager.CanSleepInBedNow} Waiting={dayTimeManager.IsWaitingForSleep}", this);
                Debug.Log("[BedSleepInteractable] Sleep not available yet — wait until the in-game day ends (e.g. 7 PM).", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!useTrigger)
                return;

            if (!IsPlayer(other))
                return;

            _playerInRange = true;
            if (enableDebugLogs)
                Debug.Log("[BedSleepInteractable] Player entered bed trigger range.", this);
            ShowInteractionUi(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!useTrigger)
                return;

            if (!IsPlayer(other))
                return;

            _playerInRange = false;
            _hasPaintedSleepPromptOnce = false;
            if (enableDebugLogs)
                Debug.Log("[BedSleepInteractable] Player exited bed trigger range.", this);
            ShowInteractionUi(false);
        }

        private void ShowInteractionUi(bool visible)
        {
            if (!visible)
            {
                SetKeyboardHostActive(false);
                SetMobileHostActive(false);
                if (_runtimeRoot != null)
                    _runtimeRoot.SetActive(false);
                return;
            }

            bool keyboard = UseKeyboardPath();

            if (UsesRuntimeHud)
            {
                _runtimeRoot.SetActive(true);
                if (_keyboardBlock != null)
                    _keyboardBlock.SetActive(keyboard);
                if (_mobileBlock != null)
                    _mobileBlock.SetActive(!keyboard);
            }
            else
            {
                SetKeyboardHostActive(keyboard);
                SetMobileHostActive(!keyboard);
            }

            if (dayTimeManager != null)
            {
                bool cs = dayTimeManager.CanSleepInBedNow;
                _lastPaintedCanSleep = cs;
                _hasPaintedSleepPromptOnce = true;
                RefreshPromptContent(cs);
            }
        }

        private void SetKeyboardHostActive(bool active)
        {
            if (customKeyboardPromptRoot != null)
                customKeyboardPromptRoot.SetActive(active);
            else if (customKeyboardPromptText != null)
                customKeyboardPromptText.gameObject.SetActive(active);
        }

        private void SetMobileHostActive(bool active)
        {
            if (customMobileButtonRoot != null)
                customMobileButtonRoot.SetActive(active);
            else if (customMobileSleepButton != null)
                customMobileSleepButton.gameObject.SetActive(active);
            else if (customMobileButtonLabel != null)
                customMobileButtonLabel.gameObject.SetActive(active);
        }

        private bool IsPlayer(Collider other)
        {
            if (string.IsNullOrEmpty(playerTag))
                return true;
            return other.CompareTag(playerTag);
        }
    }
}
