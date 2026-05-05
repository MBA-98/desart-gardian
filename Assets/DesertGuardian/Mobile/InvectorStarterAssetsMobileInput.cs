using Invector.vCharacterController;
using StarterAssets;
using UnityEngine;

namespace DesertGuardian.Mobile
{
    /// <summary>
    /// Feeds Invector from StarterAssetsInputs (PlayerInput + virtual UI on the same instance).
    /// Falls back to legacy input axes when Starter look/move are idle.
    /// </summary>
    public class InvectorStarterAssetsMobileInput : vThirdPersonInput
    {
        const float MoveDeadZoneSqr = 0.0004f;
        const float LookDeadZoneSqr = 0.0004f;

        [Header("Starter Assets")]
        [Tooltip("If null, resolved from this GameObject, then FindFirstObjectByType.")]
        public StarterAssetsInputs starterAssetsInputs;

        [Tooltip("Scales virtual look stick on mobile/tablet (look is multiplied by deltaTime * this).")]
        [SerializeField]
        float mobileLookSensitivity = 1f;

        bool _prevMobileJump;
        bool _prevMobileSprint;

        protected virtual void Awake()
        {
            if (starterAssetsInputs == null)
                TryGetComponent(out starterAssetsInputs);
        }

        protected override void Start()
        {
            if (starterAssetsInputs == null)
                starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>();

            base.Start();
        }

        public override void MoveInput()
        {
            if (starterAssetsInputs != null && starterAssetsInputs.move.sqrMagnitude > MoveDeadZoneSqr)
            {
                cc.input.x = starterAssetsInputs.move.x;
                cc.input.z = starterAssetsInputs.move.y;
            }
            else
            {
                base.MoveInput();
            }
        }

        protected override void CameraInput()
        {
            if (tpCamera == null)
            {
                tpCamera = FindFirstObjectByType<vThirdPersonCamera>();
                if (tpCamera != null)
                {
                    tpCamera.SetMainTarget(transform);
                    tpCamera.Init();
                }
            }

            if (!cameraMain)
            {
                if (!Camera.main)
                    Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
                else
                {
                    cameraMain = Camera.main;
                    cc.rotateTarget = cameraMain.transform;
                }
            }

            if (cameraMain)
                cc.UpdateMoveDirection(cameraMain.transform);

            if (tpCamera == null)
                return;

            float x;
            float y;

            if (starterAssetsInputs != null && starterAssetsInputs.look.sqrMagnitude > LookDeadZoneSqr)
            {
                if (Application.isMobilePlatform)
                {
                    float dt = Time.deltaTime;
                    x = starterAssetsInputs.look.x * dt * mobileLookSensitivity;
                    y = starterAssetsInputs.look.y * dt * mobileLookSensitivity;
                }
                else
                {
                    x = starterAssetsInputs.look.x;
                    y = starterAssetsInputs.look.y;
                }
            }
            else
            {
                y = Input.GetAxis(rotateCameraYInput);
                x = Input.GetAxis(rotateCameraXInput);
            }

            tpCamera.RotateCamera(x, y);
        }

        protected override void SprintInput()
        {
            if (starterAssetsInputs != null)
            {
                bool ms = starterAssetsInputs.sprint;
                if (ms && !_prevMobileSprint)
                    cc.Sprint(true);
                else if (!ms && _prevMobileSprint)
                    cc.Sprint(false);
                _prevMobileSprint = ms;

                if (ms)
                    return;
            }

            if (Input.GetKeyDown(sprintInput))
                cc.Sprint(true);
            else if (Input.GetKeyUp(sprintInput))
                cc.Sprint(false);
        }

        protected override void JumpInput()
        {
            if (cc == null) return;

            if (cc.isGrounded) _airJumpsRemaining = maxAirJumps;

            bool edgeJump = false;
            if (starterAssetsInputs != null)
            {
                var j = starterAssetsInputs.jump;
                if (j && !_prevMobileJump) edgeJump = true;
                _prevMobileJump = j;
            }
            if (!edgeJump)
                edgeJump = Input.GetKeyDown(jumpInput);
            if (!edgeJump) return;

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
    }
}
