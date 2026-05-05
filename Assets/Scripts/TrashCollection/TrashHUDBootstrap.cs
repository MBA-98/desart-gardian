using UnityEngine;

/// <summary>
/// Ensures core trash systems exist. UI is expected to be scene-authored (persistent) and is not created here.
/// </summary>
public static class TrashHUDBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureTrashHud()
    {
        // Ensure core systems exist.
        var score = Object.FindFirstObjectByType<ScoreManager>();
        var daily = Object.FindFirstObjectByType<DailyTargetSystem>();
        var spawner = Object.FindFirstObjectByType<TrashSpawnManager>();

        if (score == null || daily == null || spawner == null)
        {
            // Prefer an existing "TrashSystems" root; otherwise create one.
            var root = GameObject.Find("TrashSystems");
            if (root == null)
                root = new GameObject("TrashSystems");

            if (daily == null)
                daily = root.GetComponent<DailyTargetSystem>() ?? root.AddComponent<DailyTargetSystem>();
            if (score == null)
                score = root.GetComponent<ScoreManager>() ?? root.AddComponent<ScoreManager>();
            if (spawner == null)
                spawner = root.GetComponent<TrashSpawnManager>() ?? root.AddComponent<TrashSpawnManager>();
        }

        // Ensure score forwards to the daily target (ScoreManager also auto-finds, but we try to keep it clean).
        // dailyTarget is a private serialized field; the runtime auto-find will keep this working even if left null.

        if (Object.FindFirstObjectByType<TrashCollectionUI>() == null)
            Debug.LogWarning("[TrashHUDBootstrap] No TrashCollectionUI found in scene. Create a persistent HUD in the editor.", score);

        if (Object.FindFirstObjectByType<DailyTargetWinSequence>() == null)
            new GameObject("DailyTargetWinSequence").AddComponent<DailyTargetWinSequence>();
    }
}
