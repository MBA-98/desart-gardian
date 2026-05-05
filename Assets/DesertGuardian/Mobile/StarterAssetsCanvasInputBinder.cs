using StarterAssets;
using UnityEngine;

namespace DesertGuardian.Mobile
{
    /// <summary>
    /// Points <see cref="UICanvasControllerInput"/> at the Player's <see cref="StarterAssetsInputs"/>
    /// so virtual joysticks and <see cref="UnityEngine.InputSystem.PlayerInput"/> share one input state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StarterAssetsCanvasInputBinder : MonoBehaviour
    {
        [Tooltip("If set, this reference is used. Otherwise the Player-tagged object's StarterAssetsInputs is used.")]
        [SerializeField]
        StarterAssetsInputs _playerStarterAssetsInputs;

        void Awake()
        {
            var bridge = GetComponent<UICanvasControllerInput>();
            if (bridge == null)
                return;

            if (_playerStarterAssetsInputs != null)
            {
                bridge.starterAssetsInputs = _playerStarterAssetsInputs;
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var inputs = player.GetComponent<StarterAssetsInputs>();
            if (inputs != null)
                bridge.starterAssetsInputs = inputs;
        }
    }
}
