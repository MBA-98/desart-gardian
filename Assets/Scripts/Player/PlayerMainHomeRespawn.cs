using System.Collections;
using GameDayTime;
using UnityEngine;

/// <summary>
/// Teleports the player to <see cref="mainHomeSpawn"/> (or saved start pose), restores health and controls.
/// Optional: one-frame delayed auto-respawn on death when <see cref="respawnOnDeath"/> is enabled (usually off
/// if you use Game Over + <see cref="RespawnNow"/> from the Play Again button).
/// </summary>
[DisallowMultipleComponent]
public class PlayerMainHomeRespawn : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    [Tooltip("Assign MainHome_Spawn (or your home empty). Used by RespawnNow and auto-respawn.")]
    [SerializeField]
    private Transform mainHomeSpawn;

    [Tooltip("If true, one frame after death the player is moved home without pressing Play Again. Leave false to stay on Game Over until RespawnNow().")]
    [SerializeField]
    private bool respawnOnDeath;

    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private bool _savedPose;
    private Coroutine _respawnRoutine;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
    }

    private void Start()
    {
        CaptureSpawnFromTransform();
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
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }
    }

    /// <summary>Call after moving the spawn empty in the editor, or to refresh cached pose from <see cref="mainHomeSpawn"/>.</summary>
    public void CaptureSpawnFromTransform()
    {
        if (mainHomeSpawn != null)
        {
            _savedPosition = mainHomeSpawn.position;
            _savedRotation = mainHomeSpawn.rotation;
        }
        else
        {
            _savedPosition = transform.position;
            _savedRotation = transform.rotation;
        }

        _savedPose = true;
    }

    /// <summary>
    /// No scene reload: move to main home, full health, restore input/camera, hide Game Over.
    /// Wire the Play Again <c>Button.onClick</c> to this, or rely on <see cref="PlayerHealthUI"/> when this component is on the player.
    /// </summary>
    public void RespawnNow()
    {
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        ResetRunState();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();

        if (mainHomeSpawn != null)
        {
            _savedPosition = mainHomeSpawn.position;
            _savedRotation = mainHomeSpawn.rotation;
            _savedPose = true;
        }
        else if (!_savedPose)
            CaptureSpawnFromTransform();

        if (!_savedPose)
            return;

        ApplyWorldPose(_savedPosition, _savedRotation);

        playerHealth?.Revive();

        if (TryGetComponent<PlayerGameplayLock>(out var locker))
            locker.RestoreGameplay();

        foreach (var ui in FindObjectsByType<PlayerHealthUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ui.HideGameOverUI();
    }

    private void OnPlayerDied()
    {
        // Requirement: on death -> reset score immediately (HUD listens to ScoreChanged).
        ResetRunState();

        if (!respawnOnDeath || !_savedPose || playerHealth == null)
            return;

        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);
        _respawnRoutine = StartCoroutine(RespawnAfterDeathFrame());
    }

    private IEnumerator RespawnAfterDeathFrame()
    {
        yield return null;

        if (mainHomeSpawn != null)
        {
            _savedPosition = mainHomeSpawn.position;
            _savedRotation = mainHomeSpawn.rotation;
        }

        ApplyWorldPose(_savedPosition, _savedRotation);

        playerHealth.Revive();

        if (TryGetComponent<PlayerGameplayLock>(out var locker))
            locker.RestoreGameplay();

        foreach (var ui in FindObjectsByType<PlayerHealthUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ui.HideGameOverUI();

        _respawnRoutine = null;
    }

    private static void ResetRunState()
    {
        var dayMgr = FindFirstObjectByType<GameDayTimeManager>();
        dayMgr?.ResetToStartingDay();

        var score = FindFirstObjectByType<ScoreManager>();
        score?.ResetScore();

        foreach (var milestones in FindObjectsByType<TrashCollectionMilestones>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            milestones.ResetMilestoneTracking();

        foreach (var dayUi in FindObjectsByType<GameDayTimeUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            dayUi.RefreshFromManager();

        foreach (var trashUi in FindObjectsByType<TrashCollectionUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            trashUi.RefreshHud();
    }

    private void ApplyWorldPose(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        if (_rb != null)
        {
            _rb.position = position;
            _rb.rotation = rotation;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}
