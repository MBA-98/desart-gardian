using UnityEngine;

/// <summary>
/// A rectangular spawn area for trash (uses a BoxCollider bounds).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TrashSpawnZone : MonoBehaviour
{
    [Tooltip("If true, this zone is ignored.")]
    [SerializeField]
    private bool disabled;

    [Tooltip("How many spawn attempts are allowed per requested spawn inside this zone.")]
    [SerializeField]
    private int attemptsPerSpawn = 12;

    [Tooltip("How far above the ground to raycast down from.")]
    [SerializeField]
    private float raycastStartHeight = 4f;

    [Tooltip("Max raycast distance downward to find ground.")]
    [SerializeField]
    private float raycastDistance = 12f;

    [Tooltip("Random yaw rotation on spawn.")]
    [SerializeField]
    private bool randomYaw = true;

    public bool Disabled => disabled;
    public int AttemptsPerSpawn => Mathf.Max(1, attemptsPerSpawn);

    private BoxCollider _box;

    public Bounds WorldBounds
    {
        get
        {
            if (_box == null) _box = GetComponent<BoxCollider>();
            return _box.bounds;
        }
    }

    private void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(20f, 5f, 20f);
    }

    public bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation, LayerMask groundMask)
    {
        position = default;
        rotation = default;

        if (disabled)
            return false;

        var b = WorldBounds;
        // Pick a random point in XZ and raycast down to the ground.
        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);
        Vector3 origin = new Vector3(x, b.max.y + raycastStartHeight, z);

        if (!Physics.Raycast(origin, Vector3.down, out var hit, raycastDistance + (b.size.y + raycastStartHeight), groundMask, QueryTriggerInteraction.Ignore))
            return false;

        position = hit.point;
        rotation = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        return true;
    }
}

