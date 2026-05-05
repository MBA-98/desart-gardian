using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Makes a LowPolyPeople character wander and drop trash periodically.
/// Recommended: add a <see cref="NavMeshAgent"/> on the same GameObject and bake a NavMesh.
/// If no NavMesh is available, falls back to simple ground wandering.
/// </summary>
public class PedestrianWanderAndDropTrash : MonoBehaviour
{
    public enum MovementMode
    {
        [Tooltip("Uses NavMeshAgent if present, otherwise simple wandering.")]
        PreferNavMeshAgent = 0,
        [Tooltip("Forces NavMeshAgent usage (logs an error if missing).")]
        NavMeshOnly = 1,
        [Tooltip("Ignores NavMesh and uses simple wandering to random ground points.")]
        SimpleGroundWander = 2
    }

    [Header("Movement")]
    [SerializeField]
    private MovementMode movementMode = MovementMode.PreferNavMeshAgent;

    [SerializeField]
    private float moveSpeed = 1.4f;

    [Tooltip("Radius for picking a new target (from home for Simple, or from current position for NavMesh sampling).")]
    [SerializeField]
    private float wanderRadius = 18f;

    [SerializeField]
    private float repickDestinationInterval = 4f;

    [SerializeField]
    private float arriveDistance = 0.35f;

    [Header("Animator (LowPolyPeople: triggers idle / walk / run / wave)")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private bool driveAnimatorTriggers = true;

    [Header("Trash drop")]
    [SerializeField]
    private GameObject trashPrefab;

    [SerializeField]
    [Min(0.5f)]
    private float dropTrashEverySeconds = 10f;

    [SerializeField]
    private Vector3 trashSpawnOffset = new Vector3(0f, 0.02f, 0.15f);

    [Header("Ground (simple wander)")]
    [SerializeField]
    private LayerMask groundMask = ~0;

    private NavMeshAgent _agent;
    private Vector3 _homePosition;
    private Vector3 _simpleTarget;
    private float _nextRepickTime;
    private float _dropTimer;
    private bool _wasMoving;
    private bool _useNavMesh;
    private bool _hasWalkTriggerParam;
    private bool _hasIdleTriggerParam;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        CacheAnimatorTriggerAvailability();
    }

    private void Start()
    {
        _homePosition = transform.position;
        _simpleTarget = _homePosition;
        _nextRepickTime = Time.time;
        // Stagger initial drop timers to avoid many instances doing work on the same frame.
        _dropTimer = Random.Range(0f, dropTrashEverySeconds);

        switch (movementMode)
        {
            case MovementMode.NavMeshOnly:
                _useNavMesh = true;
                if (_agent == null)
                {
                    Debug.LogError("[PedestrianWanderAndDropTrash] NavMeshOnly requires NavMeshAgent on " + name, this);
                    enabled = false;
                    return;
                }
                break;
            case MovementMode.SimpleGroundWander:
                _useNavMesh = false;
                break;
            default:
                _useNavMesh = _agent != null && _agent.isActiveAndEnabled;
                break;
        }

        if (_useNavMesh)
        {
            _agent.speed = moveSpeed;
            _agent.acceleration = 8f;
            _agent.angularSpeed = 360f;
            if (NavMesh.SamplePosition(transform.position, out var warpHit, 12f, NavMesh.AllAreas))
                _agent.Warp(warpHit.position);
            else
                Debug.LogWarning("[PedestrianWanderAndDropTrash] No NavMesh near " + name + " — add a baked NavMesh or use SimpleGroundWander.", this);
            TrySetNavDestination();
            _nextRepickTime = Time.time + Random.Range(0.05f, repickDestinationInterval);
        }
        else
        {
            PickSimpleGroundTarget();
            _nextRepickTime = Time.time + Random.Range(0.05f, repickDestinationInterval);
        }

        CacheAnimatorTriggerAvailability();
    }

    /// <summary>For spawners: set the trash prefab after Instantiate (without editing every prefab manually).</summary>
    public void SetTrashPrefab(GameObject prefab) => trashPrefab = prefab;

    /// <summary>Adjust the drop interval from a spawner script.</summary>
    public void SetDropTrashInterval(float seconds) => dropTrashEverySeconds = Mathf.Max(0.5f, seconds);

    private void CacheAnimatorTriggerAvailability()
    {
        _hasWalkTriggerParam = AnimatorHasTrigger(animator, "walk");
        _hasIdleTriggerParam = AnimatorHasTrigger(animator, "idle");
    }

    private static bool AnimatorHasTrigger(Animator anim, string paramName)
    {
        if (anim == null || string.IsNullOrEmpty(paramName) || anim.runtimeAnimatorController == null)
            return false;

        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
                return true;
        }

        return false;
    }

    private void Update()
    {
        if (_useNavMesh && _agent != null && _agent.isOnNavMesh)
            TickNavMesh();
        else
            TickSimpleWander();

        TickTrashDrop();
        UpdateAnimatorTriggers();
    }

    private void TickNavMesh()
    {
        // Guard with a timer: when "arrived", the old condition became true every frame and re-ran
        // 24x NavMesh.SamplePosition per agent → stalls / very low FPS.
        if (_agent.pathPending || Time.time < _nextRepickTime)
            return;

        TrySetNavDestination();
    }

    private void TrySetNavDestination()
    {
        _nextRepickTime = Time.time + repickDestinationInterval;

        for (int i = 0; i < 24; i++)
        {
            var rnd = Random.insideUnitSphere * wanderRadius;
            rnd.y = 0f;
            var sampleOrigin = transform.position + rnd;
            if (NavMesh.SamplePosition(sampleOrigin, out var hit, wanderRadius * 0.5f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void TickSimpleWander()
    {
        var flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        var flatTarget = new Vector3(_simpleTarget.x, 0f, _simpleTarget.z);
        var toTarget = flatTarget - flatPos;
        var distSq = toTarget.sqrMagnitude;

        if (Time.time >= _nextRepickTime)
        {
            PickSimpleGroundTarget();
            _nextRepickTime = Time.time + repickDestinationInterval;
            flatTarget = new Vector3(_simpleTarget.x, 0f, _simpleTarget.z);
            toTarget = flatTarget - flatPos;
            distSq = toTarget.sqrMagnitude;
        }

        if (distSq <= arriveDistance * arriveDistance)
            return;

        if (distSq < 1e-8f)
            return;

        toTarget.Normalize();
        var delta = toTarget * (moveSpeed * Time.deltaTime);
        var next = transform.position + new Vector3(delta.x, 0f, delta.z);

        if (Physics.Raycast(next + Vector3.up * 2f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            next.y = hit.point.y;

        transform.forward = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        transform.position = next;
    }

    private void PickSimpleGroundTarget()
    {
        for (int attempt = 0; attempt < 28; attempt++)
        {
            var disk = Random.insideUnitCircle * wanderRadius;
            var tryTop = _homePosition + new Vector3(disk.x, 80f, disk.y);
            if (Physics.Raycast(tryTop, Vector3.down, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            {
                _simpleTarget = hit.point;
                return;
            }
        }
        _simpleTarget = _homePosition;
    }

    private void TickTrashDrop()
    {
        if (trashPrefab == null)
            return;

        _dropTimer -= Time.deltaTime;
        if (_dropTimer > 0f)
            return;

        _dropTimer = dropTrashEverySeconds;

        var rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        var pos = transform.position + transform.TransformVector(trashSpawnOffset);
        Instantiate(trashPrefab, pos, rot);
    }

    private void UpdateAnimatorTriggers()
    {
        if (!driveAnimatorTriggers || animator == null || animator.runtimeAnimatorController == null)
            return;

        bool moving;
        if (_useNavMesh && _agent != null && _agent.isOnNavMesh)
            moving = _agent.velocity.sqrMagnitude > 0.04f;
        else
        {
            var flat = new Vector3(transform.position.x, 0f, transform.position.z);
            var tgt = new Vector3(_simpleTarget.x, 0f, _simpleTarget.z);
            moving = (tgt - flat).sqrMagnitude > arriveDistance * arriveDistance;
        }

        if (moving == _wasMoving)
            return;

        _wasMoving = moving;
        if (moving)
        {
            if (_hasWalkTriggerParam)
                animator.SetTrigger("walk");
        }
        else
        {
            if (_hasIdleTriggerParam)
                animator.SetTrigger("idle");
        }
    }
}
