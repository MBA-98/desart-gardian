using System.Collections.Generic;
using GameDayTime;
using UnityEngine;

/// <summary>
/// Spawns trash at the start of each day using spawn points and/or zones.
/// </summary>
public class TrashSpawnManager : MonoBehaviour
{
    public enum SpawnCountMode
    {
        FixedPerDay,
        MatchDailyTarget
    }

    [Header("Wiring")]
    [SerializeField]
    private GameDayTimeManager dayTimeManager;

    [SerializeField]
    private DailyTargetSystem dailyTarget;

    [Header("Prefabs")]
    [Tooltip("Trash prefabs to spawn. If empty, tries Resources.LoadAll(\"Trash\").")]
    [SerializeField]
    private List<GameObject> trashPrefabs = new();

    [Header("Spawn sources (optional; auto-find if empty)")]
    [SerializeField]
    private List<TrashSpawnPoint> spawnPoints = new();

    [SerializeField]
    private List<TrashSpawnZone> spawnZones = new();

    [Header("Spawn amount")]
    [SerializeField]
    private SpawnCountMode spawnCountMode = SpawnCountMode.MatchDailyTarget;

    [SerializeField]
    private int fixedPerDay = 25;

    [Tooltip("Extra trash spawned per day (Day2 = +step, Day3 = +2*step...).")]
    [SerializeField]
    private int perDayStep = 0;

    [Header("Placement safety")]
    [Tooltip("Layer mask used for ground raycasts in zones.")]
    [SerializeField]
    private LayerMask groundMask = ~0;

    [Tooltip("Reject spawns that overlap with colliders in this radius.")]
    [SerializeField]
    private float overlapCheckRadius = 0.35f;

    [SerializeField]
    private LayerMask overlapMask = ~0;

    [Header("Hierarchy")]
    [Tooltip("Spawned trash will be parented under this transform. If empty, a TrashSpawned parent is created.")]
    [SerializeField]
    private Transform spawnedTrashParent;

    [Tooltip("If true, clears previously spawned trash at each new day start.")]
    [SerializeField]
    private bool clearOnNewDay = true;

    private readonly List<GameObject> _spawnedThisManager = new();

    private void Awake()
    {
        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();
        if (dailyTarget == null)
            dailyTarget = FindFirstObjectByType<DailyTargetSystem>();

        if (trashPrefabs.Count == 0)
        {
            var loaded = Resources.LoadAll<GameObject>("Trash");
            if (loaded != null && loaded.Length > 0)
                trashPrefabs.AddRange(loaded);
        }

        if (spawnedTrashParent == null)
        {
            var existing = GameObject.Find("TrashSpawned");
            if (existing == null)
                existing = new GameObject("TrashSpawned");
            spawnedTrashParent = existing.transform;
        }

        RefreshSpawnSourcesIfNeeded();
    }

    private void OnEnable()
    {
        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();
        if (dayTimeManager != null)
            dayTimeManager.DayNumberChanged += OnDayNumberChanged;
    }

    private void Start()
    {
        // Day 1: GameDayTimeManager may fire DayNumberChanged from Start as well — avoid double spawn by
        // only spawning here if nothing was spawned yet (event order can vary).
        if (_spawnedThisManager.Count == 0)
            SpawnForDay(GetDaySafe());
    }

    private void OnDisable()
    {
        if (dayTimeManager != null)
            dayTimeManager.DayNumberChanged -= OnDayNumberChanged;
    }

    private int GetDaySafe()
    {
        if (dayTimeManager != null)
            return Mathf.Max(1, dayTimeManager.CurrentDay);
        if (dailyTarget != null)
            return Mathf.Max(1, dailyTarget.CurrentDay);
        return 1;
    }

    private void OnDayNumberChanged(int day)
    {
        day = Mathf.Max(1, day);
        // First frame: Start() may already have spawned for day 1; skip duplicate.
        if (day == 1 && _spawnedThisManager.Count > 0)
            return;
        SpawnForDay(day);
    }

    public void RefreshSpawnSourcesIfNeeded()
    {
        if (spawnPoints.Count == 0)
            spawnPoints.AddRange(FindObjectsByType<TrashSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        if (spawnZones.Count == 0)
            spawnZones.AddRange(FindObjectsByType<TrashSpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    public void SpawnForDay(int day)
    {
        if (trashPrefabs.Count == 0)
        {
            Debug.LogWarning("[TrashSpawnManager] No trash prefabs assigned and none found in Resources/Trash.", this);
            return;
        }

        RefreshSpawnSourcesIfNeeded();

        int count = ComputeSpawnCount(day);
        if (count <= 0)
            return;

        if (clearOnNewDay)
            ClearSpawned();

        // Prefer spawn points (fixed, designer-controlled). Use zones for overflow.
        int spawned = 0;
        spawned += SpawnUsingPoints(count);
        if (spawned < count)
            spawned += SpawnUsingZones(count - spawned);

        if (spawned < count)
        {
            Debug.LogWarning($"[TrashSpawnManager] Only spawned {spawned}/{count}. Add more spawn points/zones or loosen overlap settings.", this);
        }
    }

    private int ComputeSpawnCount(int day)
    {
        day = Mathf.Max(1, day);
        int baseCount = spawnCountMode == SpawnCountMode.MatchDailyTarget && dailyTarget != null
            ? dailyTarget.GetTargetForDay(day)
            : Mathf.Max(0, fixedPerDay);

        int extra = Mathf.Max(0, perDayStep) * (day - 1);
        return Mathf.Max(0, baseCount + extra);
    }

    private int SpawnUsingPoints(int requested)
    {
        if (requested <= 0 || spawnPoints.Count == 0)
            return 0;

        // Build a shuffled list of enabled points.
        var enabled = new List<TrashSpawnPoint>(spawnPoints.Count);
        foreach (var p in spawnPoints)
        {
            if (p != null && !p.disabled && p.gameObject.activeInHierarchy)
                enabled.Add(p);
        }
        Shuffle(enabled);

        int spawned = 0;
        foreach (var p in enabled)
        {
            if (spawned >= requested)
                break;

            if (TrySpawnAt(p.transform.position, p.transform.rotation))
                spawned++;
        }
        return spawned;
    }

    private int SpawnUsingZones(int requested)
    {
        if (requested <= 0 || spawnZones.Count == 0)
            return 0;

        var enabled = new List<TrashSpawnZone>(spawnZones.Count);
        foreach (var z in spawnZones)
        {
            if (z != null && !z.Disabled && z.gameObject.activeInHierarchy)
                enabled.Add(z);
        }
        if (enabled.Count == 0)
            return 0;

        int spawned = 0;
        int zoneIndex = 0;

        // Round-robin zones to distribute.
        while (spawned < requested)
        {
            var zone = enabled[zoneIndex % enabled.Count];
            zoneIndex++;

            bool success = false;
            for (int a = 0; a < zone.AttemptsPerSpawn; a++)
            {
                if (!zone.TryGetSpawnPose(out var pos, out var rot, groundMask))
                    continue;

                if (TrySpawnAt(pos, rot))
                {
                    success = true;
                    spawned++;
                    break;
                }
            }

            if (!success)
                break;
        }

        return spawned;
    }

    private bool TrySpawnAt(Vector3 position, Quaternion rotation)
    {
        if (overlapCheckRadius > 0f)
        {
            var hits = Physics.OverlapSphere(position, overlapCheckRadius, overlapMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
                return false;
        }

        var prefab = trashPrefabs[Random.Range(0, trashPrefabs.Count)];
        if (prefab == null)
            return false;

        var go = Instantiate(prefab, position, rotation, spawnedTrashParent);
        EnsureTrashPickup(go);
        _spawnedThisManager.Add(go);
        return true;
    }

    public void ClearSpawned()
    {
        for (int i = _spawnedThisManager.Count - 1; i >= 0; i--)
        {
            var go = _spawnedThisManager[i];
            if (go != null)
                Destroy(go);
        }
        _spawnedThisManager.Clear();
    }

    /// <summary>
    /// Mess Maker prefabs (and similar) may not include <see cref="TrashItem"/>; add pickup logic at spawn time.
    /// </summary>
    private static void EnsureTrashPickup(GameObject instance)
    {
        if (instance == null)
            return;
        if (instance.GetComponentInChildren<TrashItem>(true) != null)
            return;

        // TrashItem uses GetComponent<Collider>() on the same GameObject as the script.
        if (instance.GetComponent<Collider>() == null)
        {
            var sphere = instance.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.4f;
            sphere.center = Vector3.up * 0.05f;
        }

        instance.AddComponent<TrashItem>();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

