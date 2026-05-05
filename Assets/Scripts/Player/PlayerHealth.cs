using System;
using UnityEngine;

/// <summary>
/// Player vitality: damage lowers health; after <see cref="RecoveryDelaySeconds"/> without damage,
/// health returns to max (unless the player is dead from Game Over).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;

    [Tooltip("Seconds without damage before health is restored to max (ignored while dead).")]
    [SerializeField]
    private float recoveryDelaySeconds = 10f;

    private float _current;
    private float _timeSinceLastDamage;
    private bool _dead;

    public float MaxHealth => maxHealth;
    public float Current => _current;
    public float CurrentHealth => _current;
    public float RecoveryDelaySeconds => recoveryDelaySeconds;
    public bool IsDead => _dead;

    /// <summary>current, max</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired once when health reaches zero.</summary>
    public event Action OnDied;

    private void Awake()
    {
        _current = maxHealth;
        _dead = false;
    }

    private void OnEnable()
    {
        OnHealthChanged?.Invoke(_current, maxHealth);
    }

    private void Update()
    {
        if (_dead)
            return;

        if (_current >= maxHealth - 0.001f)
            return;

        _timeSinceLastDamage += Time.deltaTime;
        if (_timeSinceLastDamage < recoveryDelaySeconds)
            return;

        _current = maxHealth;
        _timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(_current, maxHealth);
    }

    /// <summary>Call from fall impact, hazards, enemies, etc.</summary>
    public void TakeDamage(float amount)
    {
        if (_dead || amount <= 0f)
            return;

        _current = Mathf.Max(0f, _current - amount);
        _timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(_current, maxHealth);

        if (_current <= 0.001f)
            CommitDeath();
    }

    /// <summary>Add health without exceeding max. Does nothing if already dead.</summary>
    public void Heal(float amount)
    {
        if (_dead || amount <= 0f)
            return;

        _current = Mathf.Min(maxHealth, _current + amount);
        OnHealthChanged?.Invoke(_current, maxHealth);
    }

    /// <summary>Force death at zero health (idempotent).</summary>
    public void Die()
    {
        if (_dead)
            return;

        _current = 0f;
        _timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(_current, maxHealth);
        CommitDeath();
    }

    private void CommitDeath()
    {
        if (_dead)
            return;

        _dead = true;
        OnDied?.Invoke();
    }

    /// <summary>Restore full health and clear dead state (e.g. Play Again). Does not raise <see cref="OnDied"/>.</summary>
    public void Revive()
    {
        _dead = false;
        _current = maxHealth;
        _timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(_current, maxHealth);
    }

    public static PlayerHealth FindOnPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null)
            return null;
        return go.GetComponent<PlayerHealth>() ?? go.GetComponentInChildren<PlayerHealth>();
    }
}
