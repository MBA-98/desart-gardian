using Invector.vCharacterController;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple start screen: blocks gameplay until Play is pressed.
/// Attach this to a Canvas object that contains the start panel + Play button.
/// </summary>
[DisallowMultipleComponent]
public class StartScreenController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Root panel that covers the screen (set active at start, hidden on Play).")]
    [SerializeField] private GameObject startRoot;

    [Tooltip("Optional title text (e.g. 'Desert Guardian').")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Play button. OnClick will be wired automatically if assigned.")]
    [SerializeField] private Button playButton;

    [Header("Gameplay lock")]
    [Tooltip("If true, pauses game time while the start screen is visible.")]
    [SerializeField] private bool pauseWithTimeScale = true;

    [Tooltip("If empty, uses the GameObject tagged 'Player'.")]
    [SerializeField] private GameObject player;

    [Header("Optional")]
    [SerializeField] private string title = "Desert Guardian";

    private float _previousTimeScale = 1f;
    private bool _initialized;

    private void Awake()
    {
        // If built in-scene (manual), fields are already assigned.
        // If built at runtime by StartScreenBootstrap, it will call Initialize() after creating UI.
        TryWireButton();
        ApplyTitle();
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        // Only show/lock once we have a real root (panel) assigned.
        if (startRoot != null)
            ShowStartScreen(true);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(Play);
    }

    public void Play()
    {
        ShowStartScreen(false);
    }

    /// <summary>
    /// Called by runtime UI builders to provide references and wire the button.
    /// Safe to call multiple times.
    /// </summary>
    public void Initialize(GameObject startRootObj, TMP_Text titleObj, Button playBtn, bool pauseTimeScale)
    {
        startRoot = startRootObj;
        titleText = titleObj;
        playButton = playBtn;
        pauseWithTimeScale = pauseTimeScale;

        _initialized = true;
        ApplyTitle();
        TryWireButton();

        // Ensure the start screen is visible and gameplay is locked immediately.
        if (startRoot != null)
            ShowStartScreen(true);
    }

    private void ShowStartScreen(bool visible)
    {
        if (startRoot != null)
            startRoot.SetActive(visible);

        if (visible)
        {
            LockGameplay();
        }
        else
        {
            UnlockGameplay();
        }
    }

    private void LockGameplay()
    {
        if (pauseWithTimeScale)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (player == null)
            return;

        // Prefer existing project script if present.
        if (player.TryGetComponent<PlayerGameplayLock>(out var locker))
        {
            // PlayerGameplayLock locks on death only; we replicate the same disabling here.
            DisableGameplayComponents(player);
        }
        else
        {
            DisableGameplayComponents(player);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UnlockGameplay()
    {
        if (pauseWithTimeScale)
            Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;

        if (player != null)
        {
            if (player.TryGetComponent<PlayerGameplayLock>(out var locker))
            {
                locker.RestoreGameplay();
            }
            else
            {
                EnableGameplayComponents(player);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyTitle()
    {
        if (titleText != null)
            titleText.text = title;
    }

    private void TryWireButton()
    {
        if (playButton == null)
            return;
        playButton.onClick.RemoveListener(Play);
        playButton.onClick.AddListener(Play);
    }

    private static void DisableGameplayComponents(GameObject target)
    {
        var invectorInput = target.GetComponent<vThirdPersonInput>();
        if (invectorInput != null)
            invectorInput.enabled = false;

        var starterInputs = target.GetComponent<StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
            starterInputs.analogMovement = false;
            starterInputs.cursorInputForLook = false;
            starterInputs.enabled = false;
        }

#if ENABLE_INPUT_SYSTEM
        var playerInput = target.GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.DeactivateInput();
#endif

        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private static void EnableGameplayComponents(GameObject target)
    {
        var invectorInput = target.GetComponent<vThirdPersonInput>();
        if (invectorInput != null)
            invectorInput.enabled = true;

        var starterInputs = target.GetComponent<StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.enabled = true;
            starterInputs.cursorLocked = true;
            starterInputs.cursorInputForLook = true;
        }

#if ENABLE_INPUT_SYSTEM
        var playerInput = target.GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.ActivateInput();
#endif
    }
}

