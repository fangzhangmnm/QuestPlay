using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
namespace fzmnm.XRPlayer
{

    [DefaultExecutionOrder(-8)]
    [RequireComponent(typeof(Rigidbody))]
    public class XRBareHand : MonoBehaviour
    {
        public XRHand hand;

        public JointSettings jointSettings;
        public float lostTrackDist = .3f;
        public Animator animator;
        const bool jointFlip = false;

        private void Start()
        {
            //if rigidbody is inside player transform, it will be moved when moving player transform.
            //so the rigidbody's velocity will not account for its real velocity. which will result trouble in attaching joints
            transform.parent = null;

            //TODO for fast rotation, try swapbody
            (joint, jointBias) = JointTools.CreateJoint(body, hand.trackedHand, jointSettings,flip:jointFlip);
            joint.swapBodies = true;
        }

        private void FixedUpdate()
        {
            //TODO unify the data source of scale
            bool handLostTrack = Vector3.Distance(body.transform.position, hand.trackedPosition) > lostTrackDist * scale;
            if (handLostTrack)
                Debug.Log($"{name} lost track");
            if (hasTeleportedThisFrame||handLostTrack)
            {
                JointTools.TeleportJoint(joint, jointBias,hand.trackedPosition, hand.trackedRotation, hand.estimatedTrackedVelocityWS, flip: jointFlip);
            }

            JointTools.UpdateJoint(joint, jointBias,hand.trackedPosition, hand.trackedRotation, hand.estimatedTrackedVelocityWS, flip: jointFlip);
            hasTeleportedThisFrame = false;

            //TODO move to dedicated full body tracking script
            animator.SetFloat(hand.whichHand == XRNode.LeftHand ? "GripL" : "GripR", hand.device.grip);
        }


        Rigidbody body;
        ConfigurableJoint joint;
        Quaternion jointBias;
        bool hasTeleportedThisFrame = true;
        float scale => transform.localScale.x;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }
        void OnTeleport(Vector3 playerVelocity) 
        {
            hasTeleportedThisFrame = true;
        }
        private void OnEnable()
        {
            hand.playerLocomotion.onTeleport.AddListener(OnTeleport);
        }
        private void OnDisable()
        {
            hand.playerLocomotion.onTeleport.RemoveListener(OnTeleport);
        }
        private void OnValidate()
        {
            body = GetComponent<Rigidbody>();
            Debug.Assert(body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic);
            Debug.Assert(Physics.defaultMaxAngularSpeed >= 50f);
            Debug.Assert(Physics.defaultMaxDepenetrationVelocity <=3f);
            //Also Need to set Physics iterstions=>(10,10) and enableAdaptiveForce
            //Otherwise will trigger false collision when in fast moving vehicles
        }
    }

}
