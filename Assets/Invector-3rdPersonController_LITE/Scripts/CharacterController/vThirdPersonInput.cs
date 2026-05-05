using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Invector.vCharacterController
{
    public class vThirdPersonInput : MonoBehaviour
    {
        #region Variables       

        [Header("Controller Input")]
        public string horizontalInput = "Horizontal";
        public string verticallInput = "Vertical";
        public KeyCode jumpInput = KeyCode.Space;
        public KeyCode strafeInput = KeyCode.Tab;
        public KeyCode sprintInput = KeyCode.LeftShift;

        [Tooltip("Extra jumps while airborne after leaving the ground. 0 = ground only. 1 = double jump. 2 = triple jump, etc.")]
        [Min(0)]
        public int maxAirJumps = 2;

        [Header("Camera Input")]
        public string rotateCameraXInput = "Mouse X";
        public string rotateCameraYInput = "Mouse Y";

        [Tooltip("Flip horizontal look if moving the mouse feels backwards.")]
        public bool invertCameraHorizontal;

        [Tooltip("When using the New Input System only, Mouse X/Y axes can be zero — use pointer delta as a fallback.")]
        public bool useNewInputMouseDeltaFallback = true;

        [Tooltip("Scales pointer delta when the fallback is used (tweak if camera feels too fast/slow).")]
        [Min(0f)]
        public float newInputMouseDeltaScale = 0.02f;

        [HideInInspector] public vThirdPersonController cc;
        [HideInInspector] public vThirdPersonCamera tpCamera;
        [HideInInspector] public Camera cameraMain;

        protected int _airJumpsRemaining;

        #endregion

        protected virtual void Start()
        {
            InitilizeController();
            InitializeTpCamera();
        }

        protected virtual void FixedUpdate()
        {
            cc.UpdateMotor();               // updates the ThirdPersonMotor methods
            cc.ControlLocomotionType();     // handle the controller locomotion type and movespeed
            cc.ControlRotationType();       // handle the controller rotation type
        }

        protected virtual void Update()
        {
            InputHandle();                  // update the input methods
            cc.UpdateAnimator();            // updates the Animator Parameters
        }

        public virtual void OnAnimatorMove()
        {
            cc.ControlAnimatorRootMotion(); // handle root motion animations 
        }

        #region Basic Locomotion Inputs

        protected virtual void InitilizeController()
        {
            cc = GetComponent<vThirdPersonController>();

            if (cc != null)
                cc.Init();
        }

        protected virtual void InitializeTpCamera()
        {
            if (tpCamera == null)
            {
                tpCamera = FindFirstObjectByType<vThirdPersonCamera>();
                if (tpCamera == null)
                    return;
                if (tpCamera)
                {
                    tpCamera.SetMainTarget(this.transform);
                    tpCamera.Init();
                }
            }
        }

        protected virtual void InputHandle()
        {
            MoveInput();
            CameraInput();
            SprintInput();
            StrafeInput();
            JumpInput();
        }

        public virtual void MoveInput()
        {
            cc.input.x = Input.GetAxis(horizontalInput);
            cc.input.z = Input.GetAxis(verticallInput);
        }

        protected virtual void CameraInput()
        {
            if (!cameraMain)
            {
                if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
                else
                {
                    cameraMain = Camera.main;
                    cc.rotateTarget = cameraMain.transform;
                }
            }

            if (cameraMain)
            {
                cc.UpdateMoveDirection(cameraMain.transform);
            }

            if (tpCamera == null)
                return;

            var Y = Input.GetAxis(rotateCameraYInput);
            var X = Input.GetAxis(rotateCameraXInput);

#if ENABLE_INPUT_SYSTEM
            if (useNewInputMouseDeltaFallback && Mouse.current != null)
            {
                if (Mathf.Approximately(X, 0f) && Mathf.Approximately(Y, 0f))
                {
                    var d = Mouse.current.delta.ReadValue();
                    X = d.x * newInputMouseDeltaScale;
                    Y = d.y * newInputMouseDeltaScale;
                }
            }
#endif

            if (invertCameraHorizontal)
                X = -X;

            tpCamera.RotateCamera(X, Y);
        }

        protected virtual void StrafeInput()
        {
            if (Input.GetKeyDown(strafeInput))
                cc.Strafe();
        }

        protected virtual void SprintInput()
        {
            if (Input.GetKeyDown(sprintInput))
                cc.Sprint(true);
            else if (Input.GetKeyUp(sprintInput))
                cc.Sprint(false);
        }

        /// <summary>
        /// Conditions to trigger the Jump animation & behavior
        /// </summary>
        /// <returns></returns>
        protected virtual bool JumpConditions()
        {
            return cc.isGrounded && cc.GroundAngle() < cc.slopeLimit && !cc.isJumping && !cc.stopMove;
        }

        /// <summary>
        /// Input to trigger the Jump 
        /// </summary>
        protected virtual void JumpInput()
        {
            if (cc == null)
                return;

            if (cc.isGrounded)
                _airJumpsRemaining = maxAirJumps;

            if (!Input.GetKeyDown(jumpInput))
                return;

            if (JumpConditions())
            {
                cc.Jump();
                return;
            }

            if (maxAirJumps > 0 && !cc.isGrounded && _airJumpsRemaining > 0 && !cc.stopMove)
            {
                cc.Jump();
                _airJumpsRemaining--;
            }
        }

        #endregion       
    }
}