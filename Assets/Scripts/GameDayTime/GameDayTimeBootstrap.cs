using UnityEngine;

namespace GameDayTime
{
    /// <summary>
    /// Ensures a <see cref="GameDayTimeManager"/> exists so the day/time system works in Play Mode.
    /// UI is expected to be scene-authored (persistent) and is not created here.
    /// </summary>
    public static class GameDayTimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDayTimeExists()
        {
            var mgr = Object.FindFirstObjectByType<GameDayTimeManager>();
            if (mgr == null)
            {
                var mgrGo = new GameObject("GameDayTimeManager");
                mgr = mgrGo.AddComponent<GameDayTimeManager>();
            }

            if (Object.FindFirstObjectByType<GameDayTimeUI>() == null)
                Debug.LogWarning("[GameDayTimeBootstrap] No GameDayTimeUI found in scene. Create a persistent HUD in the editor.", mgr);
        }
    }
}
