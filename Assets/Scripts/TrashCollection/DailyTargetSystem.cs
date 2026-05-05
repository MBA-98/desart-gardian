using System;
using GameDayTime;
using UnityEngine;

/// <summary>
/// Daily trash targets driven by the actual game day from <see cref="GameDayTimeManager"/>.
/// Progress resets at the start of each new day.
/// </summary>
public class DailyTargetSystem : MonoBehaviour
{
    [Header("Target curve")]
    [Tooltip("Trash required on day 1.")]
    [SerializeField]
    private int baseTarget = 20;

    [Tooltip("Added per extra day after day 1. 0 = every day uses the same count as Base Target (e.g. always 20).")]
    [SerializeField]
    private int targetStep = 0;

    [Header("Wiring")]
    [Tooltip("If left empty, will auto-find a GameDayTimeManager in the scene.")]
    [SerializeField]
    private GameDayTimeManager dayTimeManager;

    [Header("State (read-only in play)")]
    [SerializeField]
    private int currentDay = 1;

    /// <summary>Progress toward <see cref="TargetToday"/> for the current day.</summary>
    public int ProgressToday { get; private set; }

    public int CurrentDay => currentDay;

    /// <summary>Trash required to finish the current day.</summary>
    public int TargetToday => GetTargetForDay(currentDay);

    public event Action ProgressChanged;

    private void Awake()
    {
        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();

        // Initialize day from the manager if present.
        if (dayTimeManager != null)
            currentDay = Mathf.Max(1, dayTimeManager.CurrentDay);
        else
            currentDay = Mathf.Max(1, currentDay);
    }

    private void OnEnable()
    {
        if (dayTimeManager == null)
            dayTimeManager = FindFirstObjectByType<GameDayTimeManager>();

        if (dayTimeManager != null)
            dayTimeManager.DayNumberChanged += OnDayNumberChanged;

        // Force initial UI refresh.
        ProgressChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (dayTimeManager != null)
            dayTimeManager.DayNumberChanged -= OnDayNumberChanged;
    }

    private void OnDayNumberChanged(int day)
    {
        SetDay(day, resetProgress: true);
    }

    /// <summary>Target for a 1-based day index.</summary>
    public int GetTargetForDay(int day)
    {
        day = Mathf.Max(1, day);
        return Mathf.Max(1, baseTarget + (day - 1) * targetStep);
    }

    public bool IsTargetComplete => ProgressToday >= TargetToday;

    public void SetDay(int day, bool resetProgress)
    {
        day = Mathf.Max(1, day);
        if (day == currentDay && !resetProgress)
            return;

        currentDay = day;
        if (resetProgress)
            ProgressToday = 0;

        ProgressChanged?.Invoke();
    }

    public void ResetProgressForNewDay()
    {
        ProgressToday = 0;
        ProgressChanged?.Invoke();
    }

    /// <summary>Called from <see cref="ScoreManager"/> when trash is collected.</summary>
    public void RegisterCollected(int amount)
    {
        if (amount <= 0)
            return;

        ProgressToday = Mathf.Min(TargetToday, ProgressToday + amount);

        ProgressChanged?.Invoke();
    }
}
