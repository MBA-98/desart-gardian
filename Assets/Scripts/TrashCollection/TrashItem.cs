using UnityEngine;

/// <summary>
/// / Pickup, score, then respawn after a delay at a fixed place (original or assigned transform).
/// Calls <see cref="ScoreManager.AddScore"/> → <see cref="ScoreManager.ScoreIncreased"/> (e.g. <c>PlayerSprintFatigueTrashBoost</c>).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrashItem : MonoBehaviour
{
    public enum RespawnLocationMode
    {
        [Tooltip("Same place as placed in scene or first spawn.")]
        AtInitialPosition = 0,
        [Tooltip("Use only the assigned transform below.")]
        AtAssignedPoint = 1,
        [Tooltip("Random inside TrashSpawnZone if any exist.")]
        RandomInSpawnZones = 2
    }

    [Header("Pickup / score")]
    [SerializeField]
    private int scoreValue = 1;

    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private ScoreManager scoreManager;

    [Header("Pickup SFX")]
    [Tooltip("Optional one-shot sound played when the player picks up this trash item.")]
    [SerializeField]
    private AudioClip pickupSfx;

    [Range(0f, 1f)]
    [SerializeField]
    private float pickupSfxVolume = 1f;

    [Header("Respawn")]
    [Tooltip("Used by AtAssignedPoint, or as a fallback when RandomInSpawnZones fails.")]
    [SerializeField]
    private Transform respawnPoint;

    [SerializeField]
    private RespawnLocationMode respawnMode = RespawnLocationMode.AtInitialPosition;

    [Tooltip("If false, the object is destroyed on pickup (legacy).")]
    [SerializeField]
    private bool respawnAfterPickup = true;

    [Min(0f)]
    [SerializeField]
    private float respawnTimeMin = 10f;

    [Min(0f)]
    [SerializeField]
    private float respawnTimeMax = 15f;

    [Tooltip("Raycast down to ground when using initial/assigned XZ.")]
    [SerializeField]
    private bool snapToGroundOnFixedRespawn = true;

    [SerializeField]
    private LayerMask groundMask = ~0;

    [Tooltip("If the game uses timeScale=0 while paused, enable to still count respawn time.")]
    [SerializeField]
    private bool useUnscaledTimeForDelay = false;

    [Header("Highlight")]
    [Tooltip("Soft glow above the mesh; mesh stays visible.")]
    [SerializeField]
    private bool addSpotLight = true;

    [Tooltip("Light sits above center so the can/fruit shape reads clearly.")]
    [SerializeField]
    private float highlightHeight = 0.22f;

    [SerializeField]
    private float lightRange = 0.75f;

    [SerializeField]
    private float lightIntensity = 0.3f;

    private bool _collected;
    private bool _respawnScheduled;
    private Vector3 _homePosition;
    private Quaternion _homeRotation;
    private Renderer[] _renderers;
    private Light[] _lights;
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        _homePosition = transform.position;
        _homeRotation = transform.rotation;
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (scoreManager == null)
            scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindFirstObjectByType<ScoreManager>();

        if (addSpotLight && GetComponentInChildren<Light>() == null)
            AddHighlightLight();

        CacheVisuals();
        if (_renderers == null || _renderers.Length == 0)
            Debug.LogWarning("[TrashItem] No MeshRenderer on this object or children — assign a visible mesh, or you will only see the highlight light.", this);
    }

    private void Start()
    {
        // Reliable after spawn: final position after spawn manager places the object.
        _homePosition = transform.position;
        _homeRotation = transform.rotation;
        CacheVisuals();
    }

    void CacheVisuals()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _lights = GetComponentsInChildren<Light>(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;
        if (_collected)
            return;
        TryCollect();
    }

    private void TryCollect()
    {
        if (_collected)
            return;

        _collected = true;

        if (scoreManager == null)
            scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindFirstObjectByType<ScoreManager>();

        if (scoreManager == null)
        {
            Debug.LogWarning("[TrashItem] No ScoreManager in scene — trash not counted.", this);
            _collected = false;
            return;
        }

        scoreManager.AddScore(scoreValue);
        PlayPickupSfx();

        if (respawnAfterPickup)
        {
            HideForRespawn();
            float wait = respawnTimeMax < respawnTimeMin
                ? respawnTimeMin
                : Random.Range(respawnTimeMin, respawnTimeMax);
            if (!_respawnScheduled)
            {
                _respawnScheduled = true;
                TrashRespawnScheduler.Schedule(this, wait, useUnscaledTimeForDelay);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PlayPickupSfx()
    {
        if (pickupSfx == null || pickupSfxVolume <= 0f)
            return;

        // Uses a temporary AudioSource internally; simple and reliable for student projects.
        AudioSource.PlayClipAtPoint(pickupSfx, transform.position, pickupSfxVolume);
    }

    /// <summary>Called by <see cref="TrashRespawnScheduler"/> only.</summary>
    public void FinishPendingRespawn()
    {
        _respawnScheduled = false;
        if (this == null) return;

        gameObject.SetActive(true);
        ApplyRespawnPosition();
        ShowForPickup();
        _collected = false;
    }

    private void HideForRespawn()
    {
        if (_col != null) _col.enabled = false;
        if (_renderers != null)
        {
            foreach (var r in _renderers)
            {
                if (r != null) r.enabled = false;
            }
        }
        if (_lights != null)
        {
            foreach (var l in _lights)
            {
                if (l != null) l.enabled = false;
            }
        }
    }

    private void ShowForPickup()
    {
        if (_col != null) _col.enabled = true;
        if (_renderers != null)
        {
            foreach (var r in _renderers)
            {
                if (r != null) r.enabled = true;
            }
        }
        if (_lights != null)
        {
            foreach (var l in _lights)
            {
                if (l != null) l.enabled = true;
            }
        }
    }

    private void ApplyRespawnPosition()
    {
        switch (respawnMode)
        {
            case RespawnLocationMode.AtAssignedPoint:
                if (respawnPoint != null)
                    PlaceAt(respawnPoint.position, respawnPoint.rotation);
                else
                    PlaceAtHome();
                return;
            case RespawnLocationMode.RandomInSpawnZones:
                if (TryGetRandomZonePose(out var pos, out var rot, groundMask))
                {
                    transform.SetPositionAndRotation(pos, rot);
                    return;
                }
                if (respawnPoint != null)
                {
                    PlaceAt(respawnPoint.position, respawnPoint.rotation);
                    return;
                }
                PlaceAtHome();
                return;
            default:
                PlaceAtHome();
                return;
        }
    }

    private void PlaceAtHome()
    {
        PlaceAt(_homePosition, _homeRotation);
    }

    private void PlaceAt(Vector3 position, Quaternion rotation)
    {
        if (snapToGroundOnFixedRespawn)
        {
            var origin = position + Vector3.up * 3f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 50f, groundMask, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            }
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    private static bool TryGetRandomZonePose(out Vector3 position, out Quaternion rotation, LayerMask groundMask)
    {
        position = default;
        rotation = default;
        var zones = FindObjectsByType<TrashSpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (zones == null || zones.Length == 0) return false;

        for (int t = 0; t < Mathf.Min(4, zones.Length); t++)
        {
            var z = zones[Random.Range(0, zones.Length)];
            if (z == null) continue;
            for (int a = 0; a < 10; a++)
            {
                if (z.TryGetSpawnPose(out position, out rotation, groundMask))
                    return true;
            }
        }
        return false;
    }

    private void AddHighlightLight()
    {
        var go = new GameObject("TrashHighlight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * highlightHeight;
        go.transform.localRotation = Quaternion.identity;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = lightRange;
        light.intensity = lightIntensity;
        // Warm yellow, not a white-out blob
        light.color = new Color(1f, 0.88f, 0.45f, 1f);
        light.shadows = LightShadows.None;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (GetComponent<Collider>() == null)
        {
            var s = gameObject.AddComponent<SphereCollider>();
            s.isTrigger = true;
            s.radius = 1.2f;
        }
    }
#endif
}
