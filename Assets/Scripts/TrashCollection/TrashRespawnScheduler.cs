using System.Collections;
using UnityEngine;

/// <summary>
/// / Runs the delay off the trash object so respawn completes even if the trash was disabled.
/// </summary>
[DefaultExecutionOrder(-50)]
sealed class TrashRespawnScheduler : MonoBehaviour
{
    static TrashRespawnScheduler s_Instance;

    public static void Schedule(TrashItem item, float waitSeconds, bool useUnscaledTime)
    {
        if (item == null) return;
        if (s_Instance == null)
        {
            var go = new GameObject("[TrashRespawnScheduler]");
            s_Instance = go.AddComponent<TrashRespawnScheduler>();
        }

        s_Instance.StartCoroutine(WaitAndFinish(item, Mathf.Max(0f, waitSeconds), useUnscaledTime));
    }

    static IEnumerator WaitAndFinish(TrashItem item, float t, bool unscaled)
    {
        if (t > 0f)
        {
            if (unscaled)
                yield return new WaitForSecondsRealtime(t);
            else
                yield return new WaitForSeconds(t);
        }

        if (item == null) yield break;
        item.FinishPendingRespawn();
    }
}
