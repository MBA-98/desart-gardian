using UnityEngine;

/// <summary>
/// Trigger collider: when the object tagged <see cref="playerTag"/> enters, applies damage to <see cref="PlayerHealth"/>.
/// Use for spikes, heat, enemies, etc.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageVolume : MonoBehaviour
{
    [SerializeField]
    private float damage = 15f;

    [SerializeField]
    private string playerTag = "Player";

    [Tooltip("Minimum time between damage ticks while the player stays inside the trigger.")]
    [SerializeField]
    private float hitCooldownSeconds = 0.75f;

    private float _nextDamageTime;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (hitCooldownSeconds <= 0f)
            return;
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;
        if (Time.time < _nextDamageTime)
            return;

        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            health = PlayerHealth.FindOnPlayer();
        if (health == null || health.IsDead)
            return;

        health.TakeDamage(damage);
        _nextDamageTime = Time.time + Mathf.Max(0.05f, hitCooldownSeconds);
    }

    /// <summary>Runtime tuning (e.g. from <see cref="WaterSurfaceDamageSetup"/>).</summary>
    public void ApplySettings(float damageAmount, float cooldownSeconds)
    {
        damage = Mathf.Max(0f, damageAmount);
        hitCooldownSeconds = Mathf.Max(0f, cooldownSeconds);
    }
}
