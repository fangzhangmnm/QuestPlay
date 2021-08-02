using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace fzmnm.XRPlayer
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(XRPlayerTracking))]
    [RequireComponent(typeof(KinematicCharacterCollider))]
    public partial class XRLocomotion : MonoBehaviour
    {
        public Transform trackingSpace => tracking.trackingSpace;
        public Transform head => tracking.head;
        public float speed = 3f; 
        public float jumpSpeed = 4.5f;
        public float drag = .001f;
        public float joystickDeadZone = .2f;
        public float rotateSnap = 45f;
        public float rotateSnapTime = .4f;

        [HideInInspector]public UnityEvent<Vector3> onTeleport;

        Vector3 inertiaVelocity;
        public Vector3 estimatedPlayerRootVelocity { get; private set; }

        bool isFakeMoving, hasTeleported;

        XRPlayerTracking tracking;
        StateMachine stateMachine;
        StateMachine.State GroundedState, FallingState, SeatedState, ClimbingState;
        KinematicCharacterCollider controller;
        AttachMovingPlatform attach;

        [SerializeField] private string debug_Statename;



        public void TeleportTo(Vector3 position)
        {
            if (!this || !isActiveAndEnabled) return;
            hasTeleported = true;
            throw new System.NotImplementedException();
        }

        private void Start()
        {
            stateMachine = new StateMachine();
            stateMachine.logTransition = true;
            GroundedState = new StateMachine.State("Grounded");
            GroundedState.Update = UpdateGrounded;
            GroundedState.OnEnter = EnterGrounded;
            GroundedState.OnExit = ExitGrounded;
            FallingState = new StateMachine.State("Falling");
            FallingState.Update = UpdateFalling;
            SeatedState = new StateMachine.State("Seated");
            SeatedState.Update = UpdateSeated;
            SeatedState.OnEnter = EnterSeated;
            SeatedState.OnExit = ExitSeated;

            stateMachine.DoStateTransitionImmediately(GroundedState);
        }
        private void FixedUpdate()
        {

            isFakeMoving = false;
            stateMachine.UpdateState(Time.fixedDeltaTime);
            inputJump = false;
            estimatedPlayerRootVelocity = inertiaVelocity;

            // Exercution Order:
            // 0 other scripts last frame, or
            // <-10 other scripts this frame
            //      
            // -10 XRLocomotion.fixedUpdate
            //      deal movement, prepare estimatedPlayerRootVelocity
            //      hasTeleported=>bareHand.OnTeleport set flag
            //      hasTeleported=>hand.OnTeleport=>pickup.OnTeleport set flag
            // -9 XRHand.fixedUpdate
            //      prepare estimatedTrackedVelocityWS using estimatedPlayerRootVelocity, and old Tracked Space Position
            // -8 XRBareHand.fixedUpdate
            //      check hasTeleported, update or reset joint
            // -1 XRPickup.fixedUpdate
            //      check hasTeleported, update or reset joint

            if (hasTeleported)
                onTeleport.Invoke(estimatedPlayerRootVelocity);

            hasTeleported = false;

            debug_Statename = stateMachine.currentState != null ? stateMachine.currentState.name : "";
        }



        private void LateUpdate()
        {
            stateMachine.LateUpdateState();
        }




        private void Update()
        {
            GetInput();
        }
        [HideInInspector] public bool inputJump;bool inputJumpReleased=true;
        [HideInInspector] public Vector2 inputStickL, inputStickR;
        [HideInInspector] public bool mouseControl = false;
        void GetInput()
        {
            if (mouseControl) 
                return;
            inputStickL = inputStickR = Vector2.zero;
            var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftController.isValid)
                if (leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 value))
                    inputStickL = value;
            var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightController.isValid)
                if (rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 value))
                    inputStickR = value;

            if (inputStickL.magnitude < joystickDeadZone)
                inputStickL = Vector2.zero;
            if (inputStickR.y > .5f && inputStickR.y > Mathf.Abs(inputStickR.x))
            {
                if (inputJumpReleased)
                    inputJump = true;
                inputJumpReleased = false;
            }
            else
                inputJumpReleased = true;
        }
        float rotateCD;
        void DealSnapRotation(float dt)
        {
            //Input Rotation
            int inputTurn = 0;
            if (Mathf.Abs(inputStickR.x) > .5f && Mathf.Abs(inputStickR.x) > Mathf.Abs(inputStickR.y))
                inputTurn = inputStickR.x > 0 ? 1 : -1;

            if (inputTurn != 0)
            {
                if (rotateCD <= 0)
                {
                    rotateCD = rotateSnapTime;
                    transform.rotation = transform.rotation * Quaternion.AngleAxis(rotateSnap * inputTurn, Vector3.up);
                    isFakeMoving =hasTeleported= true;
                }
            }
            else
            {
                rotateCD = 0;
            }
        }
        void DealTrackedRotation()
        {
            //Head Rotation
            Vector3 lookForward = ProjectHorizontal( head.forward).normalized;
            if (lookForward.magnitude > 0)
            {
                float angle = Vector3.SignedAngle(transform.forward, lookForward, transform.up);
                Vector3 oldHmdPos = head.position;
                transform.Rotate(transform.up, angle);
                trackingSpace.Rotate(transform.up, -angle);
                trackingSpace.position += ProjectHorizontal(oldHmdPos - head.position);
            }
        }
        void AdjustColliderAndHead()
        {
            //Setup Collider
            Debug.Assert(trackingSpace.lossyScale == Vector3.one);
            float trackedHeight = trackingSpace.InverseTransformPoint(head.position).y + tracking.trackedSpaceBias + controller.R;
            float currentHeight = controller.H;
            float maxHeight=controller.DetectMaxHeight();
            float newHeight = trackedHeight;
            if (newHeight > currentHeight && currentHeight < maxHeight)
                newHeight = maxHeight;
            controller.SetHeight(newHeight);
            trackingSpace.position = transform.position + transform.up * tracking.trackedSpaceBias + ProjectHorizontal(trackingSpace.position - transform.position);

            //Is it necessary to align head with collider height, or just let it penetrate the mesh?
        }



        private void Awake()
        {
            controller = GetComponent<KinematicCharacterCollider>();
            attach = new AttachMovingPlatform(transform);
            tracking = GetComponent<XRPlayerTracking>();
            //Setting Tracking Origin to Floor, default device on Oculus Quest
            var xrInputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances<XRInputSubsystem>(xrInputSubsystems);
            foreach (var ss in xrInputSubsystems) if (ss.running)
                {
                    ss.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                }
        }
        Vector3 ProjectHorizontal(Vector3 vec) => Vector3.ProjectOnPlane(vec, up);
        Vector3 up => transform.up;
        Vector3 gravityUp => Physics.gravity.sqrMagnitude > 0 ? -Physics.gravity.normalized : Vector3.up;
        void AlignRotationToGravity()
        {
            Vector3 gup = -Physics.gravity.normalized; if (gup.magnitude == 0) gup = Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, gup).normalized; if (forward.magnitude == 0) forward = -Vector3.ProjectOnPlane(transform.up, gup).normalized;
            transform.LookAt(transform.position + forward, gup);
        }
    }
}

