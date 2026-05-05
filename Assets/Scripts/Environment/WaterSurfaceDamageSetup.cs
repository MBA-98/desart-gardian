using UnityEngine;

/// <summary>
/// For water prefabs (e.g. Water4Advanced) that have no collider: builds a trigger box from all child
/// <see cref="MeshRenderer"/> bounds and adds <see cref="DamageVolume"/> so the player takes damage on contact.
/// Add this component once on the water root object.
/// </summary>
[DefaultExecutionOrder(-200)]
public class WaterSurfaceDamageSetup : MonoBehaviour
{
    private const string TriggerChildName = "WaterDamageTrigger";

    [SerializeField]
    private float damagePerTick = 18f;

    [SerializeField]
    private float hitCooldownSeconds = 0.75f;

    [Tooltip("Extra padding around renderer bounds (world units).")]
    [SerializeField]
    private Vector3 boundsPadding = new Vector3(0.5f, 0.35f, 0.5f);

    private void Awake()
    {
        if (transform.Find(TriggerChildName) != null)
            return;

        var renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[WaterSurfaceDamageSetup] No MeshRenderer found under " + name + ".", this);
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                b.Encapsulate(renderers[i].bounds);
        }

        b.Expand(boundsPadding);

        var go = new GameObject(TriggerChildName);
        go.layer = gameObject.layer;
        go.transform.SetPositionAndRotation(b.center, Quaternion.identity);
        go.transform.SetParent(transform, worldPositionStays: true);

        var box = go.AddComponent<BoxCollider>();
        Vector3 ls = go.transform.lossyScale;
        box.size = new Vector3(
            b.size.x / Mathf.Max(ls.x, 1e-4f),
            b.size.y / Mathf.Max(ls.y, 1e-4f),
            b.size.z / Mathf.Max(ls.z, 1e-4f));
        box.isTrigger = true;

        var dv = go.AddComponent<DamageVolume>();
        dv.ApplySettings(damagePerTick, hitCooldownSeconds);
    }
}
