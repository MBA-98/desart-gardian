using UnityEngine;

/// <summary>
/// Ensures <see cref="PlayerHealth"/>, fall impact, and HUD exist after scene load when the player is tagged Player.
/// </summary>
public static class PlayerHealthBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePlayerHealthSystems()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (player.GetComponent<PlayerHealth>() == null)
            player.AddComponent<PlayerHealth>();

        if (player.GetComponent<PlayerGameplayLock>() == null)
            player.AddComponent<PlayerGameplayLock>();

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null && player.GetComponent<Invector.vCharacterController.vThirdPersonMotor>() != null)
        {
            if (player.GetComponent<PlayerFallImpactHealth>() == null)
                player.AddComponent<PlayerFallImpactHealth>();
        }

        if (Object.FindFirstObjectByType<PlayerHealthUI>() != null)
            return;

        var host = new GameObject("PlayerHealthSystems");
        var ui = host.AddComponent<PlayerHealthUI>();
        if (ui == null)
            Debug.LogWarning("[PlayerHealthBootstrap] Failed to add PlayerHealthUI.", host);
    }
}
