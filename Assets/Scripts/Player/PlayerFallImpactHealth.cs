using Invector.vCharacterController;
using UnityEngine;

/// <summary>
/// For Invector <see cref="vThirdPersonMotor"/> characters: hard landings (high downward speed before touch-down)
/// reduce <see cref="PlayerHealth"/> — same recovery rules as other damage.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerFallImpactHealth : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    [Tooltip("Ground impact speed (m/s, negative) below this does not count as a hard landing.")]
    [SerializeField]
    private float hardLandingSpeedThreshold = -9f;

    [SerializeField]
    private float damagePerUnitOverThreshold = 4f;

    [SerializeField]
    private float maxDamageThisLanding = 35f;

    private Rigidbody _rb;
    private vThirdPersonMotor _motor;
    private bool _wasGrounded = true;
    private float _peakFallVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _motor = GetComponent<vThirdPersonMotor>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
    }

    private void Start()
    {
        if (_motor != null)
            _wasGrounded = _motor.isGrounded;
    }

    private void FixedUpdate()
    {
        if (_motor == null || playerHealth == null || playerHealth.IsDead)
            return;

        bool grounded = _motor.isGrounded;

        if (!grounded)
        {
            _peakFallVelocity = Mathf.Min(_peakFallVelocity, _rb.linearVelocity.y);
        }
        else
        {
            if (!_wasGrounded && _peakFallVelocity <= hardLandingSpeedThreshold)
            {
                float over = Mathf.Abs(_peakFallVelocity) - Mathf.Abs(hardLandingSpeedThreshold);
                float dmg = Mathf.Min(maxDamageThisLanding, over * damagePerUnitOverThreshold);
                if (dmg > 0.5f)
                    playerHealth.TakeDamage(dmg);
            }

            _peakFallVelocity = 0f;
        }

        _wasGrounded = grounded;
    }
}
