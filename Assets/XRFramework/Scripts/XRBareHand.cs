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
        const bool jointFlip = true;

        private void Start()
        {
            //if rigidbody is inside player transform, it will be moved when moving player transform.
            //so the rigidbody's velocity will not account for its real velocity. which will result trouble in attaching joints
            transform.parent = null;

            //TODO for fast rotation, try swapbody
            (joint, jointBias) = JointTools.CreateGrabJoint(body, hand.trackedHand, jointSettings,flip:jointFlip);
        }

        private void FixedUpdate()
        {
            //TODO unify the data source of scale
            bool handLostTrack = Vector3.Distance(body.transform.position, hand.meshPosition) > lostTrackDist * scale;
            if (handLostTrack)
                Debug.Log($"{name} lost track");
            if (hand.attached)
            {
                body.isKinematic = true;
                transform.position = hand.meshPosition;
                transform.rotation = hand.meshRotation;
                needTeleportJoint = true;
            }
            else
            {
                body.isKinematic = false;

                if (needTeleportJoint || handLostTrack)
                    JointTools.TeleportGrabJoint(joint, jointBias, hand.meshPosition, hand.meshRotation, hand.estimatedMeshVelocityWS, flip: jointFlip);
                needTeleportJoint = false;
                JointTools.UpdateGrabJoint(joint, jointBias, hand.meshPosition, hand.meshRotation, hand.estimatedMeshVelocityWS, flip: jointFlip);


            }


            //TODO move to dedicated full body tracking script
            animator.SetFloat(hand.whichHand == XRNode.LeftHand ? "GripL" : "GripR", hand.device.grip);
        }
        private void LateUpdate()
        {
            if (hand.attached && hand.attached.GetAttachPosition(hand, out Vector3 p, out Quaternion q))
            {
                body.isKinematic = true;
                transform.position = p;
                transform.rotation = q;
                needTeleportJoint = true;
            }
        }


        Rigidbody body;
        ConfigurableJoint joint;
        Quaternion jointBias;
        bool needTeleportJoint = true;
        float scale => transform.localScale.x;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }
        void OnTeleport(Vector3 playerVelocity) 
        {
            needTeleportJoint = true;
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
            if (body.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
                Debug.Log("Suggest using ContinuousSpeculative");
            Debug.Assert(Physics.defaultMaxDepenetrationVelocity <=3f);
            if (body.interpolation != RigidbodyInterpolation.None)
                Debug.LogError("Disable interpolation because we want to sync graphics and physics");


            if (Physics.defaultSolverIterations < 15)
                Debug.LogWarning("Set Physics.defaultSolverIterations>=15 for better sword blocking");
            if (Physics.defaultMaxAngularSpeed < 50f)
                Debug.LogError("Set Physics.defaultMaxAngularSpeed>=50 for physica-based hand tracking");
            if (Physics.defaultMaxDepenetrationVelocity > 3f)
                Debug.LogWarning("Set Physics.defaultMaxAngularSpeed<3 to avoid ejecting objects when jittering");
            //Also Need to set Physics iterstions=>(10,10) and enableAdaptiveForce
            //Otherwise will trigger false collision when in fast moving vehicles
        }
    }

}
