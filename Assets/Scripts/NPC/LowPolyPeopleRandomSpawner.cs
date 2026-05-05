using UnityEngine;

/// <summary>
/// Randomly spawns LowPolyPeople characters inside <see cref="TrashSpawnZone"/> (or around this transform),
/// and assigns a random trash prefab to <see cref="PedestrianWanderAndDropTrash"/>.
/// </summary>
public class LowPolyPeopleRandomSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Drag all prefabs from LowPolyPeople/Prefabs (root only, not the Animations folder).")]
    [SerializeField]
    private GameObject[] characterPrefabs;

    [Tooltip("Trash prefabs; one is picked randomly per character.")]
    [SerializeField]
    private GameObject[] trashPrefabs;

    [Header("Where to spawn")]
    [Tooltip("If left empty, all TrashSpawnZone components in the scene are found automatically.")]
    [SerializeField]
    private TrashSpawnZone[] spawnZones;

    [Tooltip("If no zones exist, spawns on a horizontal disk around this GameObject.")]
    [SerializeField]
    private float fallbackRadiusAroundSelf = 22f;

    [SerializeField]
    private LayerMask groundMask = ~0;

    [Header("Count")]
    [SerializeField]
    [Min(0)]
    private int spawnCount = 10;

    [SerializeField]
    private bool spawnOnStart = true;

    [Tooltip("Random drop interval between min and max seconds per character.")]
    [SerializeField]
    private float dropIntervalMin = 6f;

    [SerializeField]
    private float dropIntervalMax = 14f;

    [SerializeField]
    private Transform spawnedParent;

    private void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    [ContextMenu("Spawn Now")]
    public void SpawnAll()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogWarning("[LowPolyPeopleRandomSpawner] characterPrefabs is empty.", this);
            return;
        }

        ResolveSpawnZones();

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryGetRandomSpawnPose(out var pos, out var rot))
            {
                Debug.LogWarning("[LowPolyPeopleRandomSpawner] Failed to find a ground position for this attempt — skipping.", this);
                continue;
            }

            var prefab = characterPrefabs[Random.Range(0, characterPrefabs.Length)];
            if (prefab == null)
                continue;

            var instance = Instantiate(prefab, pos, rot, spawnedParent != null ? spawnedParent : null);
            if (instance.TryGetComponent<PedestrianWanderAndDropTrash>(out var ped))
            {
                var trash = PickRandomTrash();
                if (trash != null)
                    ped.SetTrashPrefab(trash);
                if (dropIntervalMax >= dropIntervalMin && dropIntervalMax > 0f)
                    ped.SetDropTrashInterval(Random.Range(dropIntervalMin, dropIntervalMax));
            }
        }
    }

    private void ResolveSpawnZones()
    {
        if (spawnZones != null && spawnZones.Length > 0)
            return;

        spawnZones = FindObjectsByType<TrashSpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private GameObject PickRandomTrash()
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0)
            return null;
        return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
    }

    private bool TryGetRandomSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = default;

        if (spawnZones != null && spawnZones.Length > 0)
        {
            for (int t = 0; t < Mathf.Min(6, spawnZones.Length); t++)
            {
                var z = spawnZones[Random.Range(0, spawnZones.Length)];
                if (z == null || z.Disabled)
                    continue;
                for (int a = 0; a < z.AttemptsPerSpawn; a++)
                {
                    if (z.TryGetSpawnPose(out position, out rotation, groundMask))
                        return true;
                }
            }
        }

        for (int attempt = 0; attempt < 32; attempt++)
        {
            var disk = Random.insideUnitCircle * fallbackRadiusAroundSelf;
            var origin = transform.position + new Vector3(disk.x, 60f, disk.y);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 250f, groundMask, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                return true;
            }
        }

        return false;
    }
}
