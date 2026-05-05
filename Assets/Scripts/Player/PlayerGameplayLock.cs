using Invector.vCharacterController;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// On player death: disables locomotion/camera input scripts and zeros rigidbody motion.
/// Does not modify Invector package scripts.
/// </summary>
[DisallowMultipleComponent]
public class PlayerGameplayLock : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        var input = GetComponent<vThirdPersonInput>();
        if (input != null)
            input.enabled = false;

        if (TryGetComponent<StarterAssetsInputs>(out var starter))
        {
            starter.move = Vector2.zero;
            starter.look = Vector2.zero;
            starter.jump = false;
            starter.sprint = false;
            starter.analogMovement = false;
            starter.cursorInputForLook = false;
            starter.enabled = false;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetComponent<PlayerInput>(out var playerInput))
            playerInput.DeactivateInput();
#endif

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Re-enable input after Play Again (opposite of death lock).</summary>
    public void RestoreGameplay()
    {
        var input = GetComponent<vThirdPersonInput>();
        if (input != null)
            input.enabled = true;

        if (TryGetComponent<StarterAssetsInputs>(out var starter))
        {
            starter.enabled = true;
            starter.cursorLocked = true;
            starter.cursorInputForLook = true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetComponent<PlayerInput>(out var playerInput))
            playerInput.ActivateInput();
#endif

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

