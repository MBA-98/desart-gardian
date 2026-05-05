using System;
using UnityEngine;

/// <summary>
/// Total trash collected; forwards pickups to <see cref="DailyTargetSystem"/> for day/target progression.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>Avoids repeated <c>FindFirstObjectByType</c> from many <see cref="TrashItem"/> instances (mobile GC + CPU).</summary>
    public static ScoreManager Instance { get; private set; }

    [SerializeField]
    private DailyTargetSystem dailyTarget;

    /// <summary>All-time trash count (every pickup).</summary>
    public int CurrentScore { get; private set; }

    /// <summary>Raised when <see cref="CurrentScore"/> changes.</summary>
    public event Action ScoreChanged;

    /// <summary>Raised with the positive pickup amount when trash is collected (see <see cref="TrashItem"/>). Not invoked on <see cref="ResetScore"/>.</summary>
    public event Action<int> ScoreIncreased;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[ScoreManager] Multiple ScoreManagers — Instance points at the first awake only.", this);
        Instance = this;

        if (dailyTarget == null)
            dailyTarget = GetComponent<DailyTargetSystem>();
        if (dailyTarget == null)
            dailyTarget = FindFirstObjectByType<DailyTargetSystem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Adds trash points and updates daily target progression.</summary>
    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        CurrentScore += amount;
        ScoreIncreased?.Invoke(amount);
        ScoreChanged?.Invoke();

        if (dailyTarget != null)
            dailyTarget.RegisterCollected(amount);
    }

    /// <summary>Reset lifetime score (e.g. Play Again from Game Over).</summary>
    public void ResetScore()
    {
        CurrentScore = 0;
        ScoreChanged?.Invoke();
    }
}
