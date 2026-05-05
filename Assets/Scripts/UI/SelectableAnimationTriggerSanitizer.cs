using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UGUI <see cref="Selectable"/> (Animation transition) calls <c>Animator.ResetTrigger(m_AnimationTriggers.normalTrigger)</c>
/// without null checks. Serialized null trigger names → <see cref="System.ArgumentNullException"/> Parameter name: key every frame.
/// Fixes empty/null names to Unity defaults so play mode stays stable (mobile + Editor).
/// </summary>
static class SelectableAnimationTriggerSanitizer
{
    static bool _sceneHooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _sceneHooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SanitizeAll();
        if (_sceneHooked)
            return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _sceneHooked = true;
    }

    static void OnSceneLoaded(Scene _, LoadSceneMode __) => SanitizeAll();

    static void SanitizeAll()
    {
        var selectables = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < selectables.Length; i++)
        {
            var s = selectables[i];
            if (s == null || s.transition != Selectable.Transition.Animation)
                continue;

            var t = s.animationTriggers;
            bool changed = false;
            if (string.IsNullOrEmpty(t.normalTrigger)) { t.normalTrigger = "Normal"; changed = true; }
            if (string.IsNullOrEmpty(t.highlightedTrigger)) { t.highlightedTrigger = "Highlighted"; changed = true; }
            if (string.IsNullOrEmpty(t.pressedTrigger)) { t.pressedTrigger = "Pressed"; changed = true; }
            if (string.IsNullOrEmpty(t.selectedTrigger)) { t.selectedTrigger = "Selected"; changed = true; }
            if (string.IsNullOrEmpty(t.disabledTrigger)) { t.disabledTrigger = "Disabled"; changed = true; }

            if (changed)
                s.animationTriggers = t;
        }
    }
}
