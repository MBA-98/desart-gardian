using Invector.vCharacterController;
using UnityEngine;

/// <summary>
/// While sprinting (Invector <see cref="vThirdPersonMotor.isSprinting"/>), sprint speed slowly drops.
/// Each trash pickup (via <see cref="ScoreManager.ScoreIncreased"/>) reduces fatigue and speeds sprint back up.
/// Pickup logic lives on <see cref="TrashItem"/> → <see cref="ScoreManager.AddScore"/>.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSprintFatigueTrashBoost : MonoBehaviour
{
    [SerializeField]
    private vThirdPersonMotor motor;

    [SerializeField]
    private ScoreManager scoreManager;

    [Header("Sprint fatigue (0 = full sprint speed, 1 = slowest)")]
    [Tooltip("Fatigue gained per second while sprinting on the ground with movement input.")]
    [SerializeField]
    private float fatigueGainPerSecondWhileSprinting = 0.35f;

    [Tooltip("Fatigue lost per second when not sprinting (recovery).")]
    [SerializeField]
    private float fatigueRecoverPerSecond = 0.2f;

    [Tooltip("Sprint speed multiplier when fatigue is at 1 (exhausted).")]
    [SerializeField, Range(0.2f, 1f)]
    private float minSprintSpeedFactor = 0.45f;

    [Header("Trash pickup (TrashItem → ScoreManager)")]
    [Tooltip("Subtract this much fatigue per trash point collected (scoreValue on TrashItem).")]
    [SerializeField]
    private float fatigueReductionPerTrashPoint = 0.22f;

    private float _baseFreeSprint;
    private float _baseStrafeSprint;
    private float _fatigue;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<vThirdPersonMotor>();
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        if (motor != null)
        {
            _baseFreeSprint = motor.freeSpeed.sprintSpeed;
            _baseStrafeSprint = motor.strafeSpeed.sprintSpeed;
        }
    }

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.ScoreIncreased += OnTrashScoreIncreased;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.ScoreIncreased -= OnTrashScoreIncreased;
    }

    private void OnTrashScoreIncreased(int amount)
    {
        if (amount <= 0)
            return;
        _fatigue = Mathf.Max(0f, _fatigue - fatigueReductionPerTrashPoint * amount);
    }

    private void LateUpdate()
    {
        if (motor == null)
            return;

        bool sprinting = motor.IsSprintingOnGroundWithMoveInput;
        if (sprinting)
            _fatigue = Mathf.Clamp01(_fatigue + fatigueGainPerSecondWhileSprinting * Time.deltaTime);
        else
            _fatigue = Mathf.Clamp01(_fatigue - fatigueRecoverPerSecond * Time.deltaTime);

        float sprintMul = Mathf.Lerp(minSprintSpeedFactor, 1f, 1f - _fatigue);
        motor.freeSpeed.sprintSpeed = _baseFreeSprint * sprintMul;
        motor.strafeSpeed.sprintSpeed = _baseStrafeSprint * sprintMul;
    }
}
