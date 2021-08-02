using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    public class JointTools
    {
        //TODO swapbody support
        public static (ConfigurableJoint, Quaternion) CreateJoint(Rigidbody body, Rigidbody attachedRigidbody, JointSettings jointSettings)
        {
            var joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = attachedRigidbody;

            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.xDrive = joint.yDrive = joint.zDrive =new JointDrive { positionSpring = jointSettings.spring, positionDamper = jointSettings.damper, maximumForce = jointSettings.maxForce };
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive =new JointDrive { positionSpring = jointSettings.angularSpring, positionDamper = jointSettings.angularDamper, maximumForce = jointSettings.angularMaxForce };

            var jointSetupInverseAttachedTimesBodyRotation = Quaternion.Inverse(attachedRigidbody.transform.rotation) * body.transform.rotation;
            return (joint, jointSetupInverseAttachedTimesBodyRotation);
        }

        public static void UpdateJoint(ConfigurableJoint joint, Quaternion jointSetupInverseAttachedTimesBodyRotation,
             Vector3 targetPosition, Quaternion targetRotation, Vector3 targetDeltaVelocityWS)
        {

             Rigidbody attachedRigidbody = joint.connectedBody;

            //TODO check anchor is influenced by scale
            // tutorial. all anchors defined in scaled local space.
            // when the body is in target rotation, the two anchors should coincide. so we just transform connectedAnchor using connectedBody's current transform,
            // then transform it back using body's scale but targetPosition and targetRotation
            // joint.transform.InverseTransformVector * joint.transform.rotation acts as the inverse scale transform
            joint.targetPosition = Vector3.zero;
            joint.connectedAnchor = Vector3.zero; //always at connectedBody's pivot
            joint.anchor=joint.transform.InverseTransformVector(
                joint.transform.rotation * Quaternion.Inverse(targetRotation) * (attachedRigidbody.transform.TransformPoint(joint.connectedAnchor) - targetPosition)
                );

            // tutorial. b: body's rotation, a: attached body's rotation, r: joint's target rotation
            // at setup, rotate b0 locally by bias, we achieve a0: b0*bias=a0  =>  bias=b0^-1 a0
            // now, when body is at the target rotation b, it satisfies: after rotate the body locally by r, it retains the initial rotational offset to a:
            // b*r*bias=a => r=b^-1 a a0^-1 b0
            
            joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.transform.rotation * jointSetupInverseAttachedTimesBodyRotation;

            // target velocity is the velocity of the connectedAnchor relative to anchor in the body frame
            // choose current rotation gives better stability than target rotation under rapid rotation change and fast movements
            joint.targetVelocity = -Vector3.Lerp(Quaternion.Inverse(targetRotation) * targetDeltaVelocityWS ,Quaternion.Inverse(joint.transform.rotation)*targetDeltaVelocityWS,1); 
        }
        
        public static void TeleportJoint(ConfigurableJoint joint, Quaternion jointSetupInverseAttachedTimesBodyRotation,
            Vector3 targetPosition, Quaternion targetRotation, Vector3 targetDeltaVelocityWS)
        {

            Rigidbody body = joint.GetComponent<Rigidbody>();
            Rigidbody attachedRigidbody = joint.connectedBody;

            //changes in body will not reflect in transform in this frame
            body.transform.position = targetPosition;
            //?? + targetDeltaVelocityWS * Time.fixedDeltaTime;
            body.transform.rotation = targetRotation;
            body.velocity = (attachedRigidbody.isKinematic?Vector3.zero: attachedRigidbody.velocity)+targetDeltaVelocityWS;
            body.angularVelocity = Vector3.zero;

            UpdateJoint(joint, jointSetupInverseAttachedTimesBodyRotation, targetPosition, targetRotation, targetDeltaVelocityWS);
        }
    }
}