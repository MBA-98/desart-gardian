using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Autonomous wander (no keyboard — random map roaming)")]
        [Tooltip("When true, this animal ignores player input and walks/runs in random directions.")]
        [SerializeField]
        private bool m_AutonomousRandomMovement;

        [SerializeField]
        private float m_MinDirectionChangeSeconds = 2f;

        [SerializeField]
        private float m_MaxDirectionChangeSeconds = 5f;

        [Tooltip("Chance each tick to stand idle instead of walking.")]
        [SerializeField, Range(0f, 1f)]
        private float m_IdleChance = 0.12f;

        [SerializeField, Range(0f, 1f)]
        private float m_RunChance = 0.12f;

        [SerializeField]
        private float m_ObstacleCheckDistance = 0.55f;

        [SerializeField]
        private LayerMask m_ObstacleMask = ~0;

        [Header("Chase player (add CreatureEnemyChase on same object)")]
        [Tooltip("Optional: chase + melee damage when autonomous. Leave empty to auto-pick GetComponent.")]
        [SerializeField]
        private CreatureEnemyChase m_EnemyChase;

        [Header("Character")]
        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";
        [SerializeField]
        private string m_VerticalAxis = "Vertical";
        [SerializeField]
        private string m_JumpButton = "Jump";
        [SerializeField]
        private KeyCode m_RunKey = KeyCode.LeftShift;

        [Header("Camera")]
        [SerializeField]
        private PlayerCamera m_Camera;
        [SerializeField]
        private string m_MouseX = "Mouse X";
        [SerializeField]
        private string m_MouseY = "Mouse Y";
        [SerializeField]
        private string m_MouseScroll = "Mouse ScrollWheel";

        private CreatureMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;
        private bool m_IsJump;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;

        private float m_NextWanderChangeTime;
        private Vector3 m_WanderPlanarForward;
        private CharacterController m_CharacterController;

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();
            m_CharacterController = GetComponent<CharacterController>();
            if (m_EnemyChase == null)
                m_EnemyChase = GetComponent<CreatureEnemyChase>();
            m_WanderPlanarForward = HorizontalForward(transform.forward);
        }

        private void Start()
        {
            ScheduleNextWanderChange(0f);
        }

        private void Update()
        {
            GatherInput();
            SetInput();
        }

        public void GatherInput()
        {
            if (m_AutonomousRandomMovement)
            {
                GatherAutonomousWander();
                m_MouseDelta = Vector2.zero;
                m_Scroll = 0f;
                if (m_Camera != null)
                    m_Camera.SetInput(in m_MouseDelta, m_Scroll);
                return;
            }

            m_Axis = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
            m_IsRun = Input.GetKey(m_RunKey);
            m_IsJump = Input.GetButton(m_JumpButton);

            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;
            m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));
            m_Scroll = Input.GetAxis(m_MouseScroll);
        }

        private void GatherAutonomousWander()
        {
            m_IsJump = false;

            if (m_EnemyChase != null && m_EnemyChase.TryGetChaseInput(out var chaseAxis, out var chaseTarget, out var chaseRun))
            {
                m_Axis = chaseAxis;
                m_Target = chaseTarget;
                m_IsRun = chaseRun;
                return;
            }

            if (Time.time >= m_NextWanderChangeTime || IsBlockedAhead(m_WanderPlanarForward))
                PickNewWanderState();

            m_Target = transform.position + m_WanderPlanarForward * 8f + Vector3.up * 0.25f;
        }

        private void PickNewWanderState()
        {
            if (Random.value < m_IdleChance)
            {
                m_Axis = Vector2.zero;
                m_IsRun = false;
            }
            else
            {
                float yaw = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                m_WanderPlanarForward = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)).normalized;
                if (m_WanderPlanarForward.sqrMagnitude < 0.01f)
                    m_WanderPlanarForward = Vector3.forward;

                m_Axis = new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.65f, 1f));
                m_Axis = Vector2.ClampMagnitude(m_Axis, 1f);
                m_IsRun = Random.value < m_RunChance;
            }

            ScheduleNextWanderChange(Random.Range(m_MinDirectionChangeSeconds, m_MaxDirectionChangeSeconds));
        }

        private void ScheduleNextWanderChange(float delay)
        {
            m_NextWanderChangeTime = Time.time + Mathf.Max(0.1f, delay);
        }

        private bool IsBlockedAhead(Vector3 planarDir)
        {
            if (planarDir.sqrMagnitude < 0.01f || m_ObstacleCheckDistance <= 0f)
                return false;

            var origin = transform.position + Vector3.up * 0.28f + planarDir.normalized * 0.15f;
            float radius = m_CharacterController != null ? m_CharacterController.radius * 0.85f : 0.2f;

            if (Physics.SphereCast(origin, radius, planarDir.normalized, out var hit, m_ObstacleCheckDistance, m_ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                    return false;
                return true;
            }

            return false;
        }

        private static Vector3 HorizontalForward(Vector3 dir)
        {
            var h = new Vector3(dir.x, 0f, dir.z);
            return h.sqrMagnitude > 0.0001f ? h.normalized : Vector3.forward;
        }

        public void BindMover(CreatureMover mover)
        {
            m_Mover = mover;
        }

        public void SetInput()
        {
            if (m_Mover != null)
            {
                m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);
            }

            if (m_Camera != null && !m_AutonomousRandomMovement)
            {
                m_Camera.SetInput(in m_MouseDelta, m_Scroll);
            }
        }
    }
}
