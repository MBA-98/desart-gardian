using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Optional: when total points from <see cref="ScoreManager"/> cross configured thresholds, invoke events (e.g. VFX, audio, power-ups).
/// </summary>
public class TrashCollectionMilestones : MonoBehaviour
{
    [SerializeField]
    private ScoreManager scoreManager;

    [Tooltip("Cumulative point thresholds, in ascending order (e.g. 100, 250, 500).")]
    [SerializeField]
    private int[] pointThresholds = { 100, 250, 500 };

    [SerializeField]
    private UnityEvent<int> onMilestonePoints;

    private int _nextIndex;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged += OnScoreChanged;
        OnScoreChanged();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged()
    {
        if (scoreManager == null || pointThresholds == null || onMilestonePoints == null)
            return;
        int total = scoreManager.CurrentScore;
        while (_nextIndex < pointThresholds.Length && total >= pointThresholds[_nextIndex])
        {
            onMilestonePoints?.Invoke(pointThresholds[_nextIndex]);
            _nextIndex++;
        }
    }

    /// <summary>Call after <see cref="ScoreManager.ResetScore"/> so milestones can fire again in a new run.</summary>
    public void ResetMilestoneTracking()
    {
        _nextIndex = 0;
        OnScoreChanged();
    }
}
