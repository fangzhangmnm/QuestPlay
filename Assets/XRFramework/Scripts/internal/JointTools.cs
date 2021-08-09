using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    public static class JointTools
    {
        public static (ConfigurableJoint, Quaternion) CreateGrabJoint(Rigidbody body, Rigidbody attachedRigidbody, JointSettings jointSettings, bool flip=false)
        {
            if (flip) { var t = body;body = attachedRigidbody;attachedRigidbody = t; }

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

        public static void UpdateGrabJoint(ConfigurableJoint joint, Quaternion jointSetupInverseAttachedTimesBodyRotation,
             Vector3 targetPosition, Quaternion targetRotation, Vector3 targetDeltaVelocityWS, bool flip=false)
        {
            //Rigidbody body = joint.GetComponent<Rigidbody>();
            Rigidbody attachedRigidbody = joint.connectedBody;

            //TODO check anchor is influenced by scale
            // tutorial. all anchors defined in scaled local space.
            // when the body is in target rotation, the two anchors should coincide. so we just transform connectedAnchor using connectedBody's current transform,
            // then transform it back using body's scale but targetPosition and targetRotation
            // joint.transform.InverseTransformVector * joint.transform.rotation acts as the inverse scale transform
            joint.targetPosition = Vector3.zero;
            if (!flip)
            {
                joint.connectedAnchor = Vector3.zero; //always at connectedBody's pivot
                //joint.anchor=joint.transform.AtTargetPositionAndRotation.InverseTransformPoint(attachedRigidbody.transform.TransformPoint(joint.connectedAnchor));
                Vector3 anchorWS = attachedRigidbody.transform.TransformPoint(joint.connectedAnchor);
                Vector3 anchorLS = anchorWS - targetPosition;
                anchorLS = Quaternion.Inverse(targetRotation) * anchorLS;
                anchorLS = joint.transform.InverseTransformVector(joint.transform.rotation * anchorLS);
                joint.anchor = anchorLS;
            }
            else
            {
                joint.anchor = Vector3.zero; //always at connectedBody's pivot
                //joint.connectedAnchor=attachedRigidbody.transform.AtTargetPositionAndRotation.InverseTransformPoint(joint.transform.TransformPoint(joint.anchor));
                Vector3 connectedAnchorWS = joint.transform.TransformPoint(joint.anchor);
                Vector3 connectedAnchorLS = connectedAnchorWS - targetPosition;
                connectedAnchorLS = Quaternion.Inverse(targetRotation) * connectedAnchorLS;
                connectedAnchorLS = attachedRigidbody.transform.InverseTransformVector(attachedRigidbody.transform.rotation * connectedAnchorLS);
                joint.connectedAnchor = connectedAnchorLS;
            }
            // tutorial. b: body's rotation, a: attached body's rotation, r: joint's target rotation
            // at setup, rotate b0 locally by bias, we achieve a0: b0*bias=a0  =>  bias=b0^-1 a0
            // now, when body is at the target rotation b, it satisfies: after rotate the body locally by r, it retains the initial rotational offset to a:
            // b*r*bias=a => r=b^-1 a a0^-1 b0
            if (!flip)
                joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.transform.rotation * jointSetupInverseAttachedTimesBodyRotation;
            else
                joint.targetRotation = Quaternion.Inverse(joint.transform.rotation) * targetRotation * jointSetupInverseAttachedTimesBodyRotation;


            // target velocity is the velocity of the connectedAnchor relative to anchor in the body frame
            // choose current rotation gives better stability than target rotation under rapid rotation change and fast movements

            if (!flip)
                joint.targetVelocity = -Vector3.Lerp(Quaternion.Inverse(targetRotation) * targetDeltaVelocityWS, Quaternion.Inverse(joint.transform.rotation) * targetDeltaVelocityWS, 1);
            else
                joint.targetVelocity = Quaternion.Inverse(joint.transform.rotation) * targetDeltaVelocityWS;
        }
        
        public static void TeleportGrabJoint(ConfigurableJoint joint, Quaternion jointSetupInverseAttachedTimesBodyRotation,
            Vector3 targetPosition, Quaternion targetRotation, Vector3 targetDeltaVelocityWS, bool flip=false)
        {

            Rigidbody body = joint.GetComponent<Rigidbody>();
            Rigidbody attachedRigidbody = joint.connectedBody;

            if (!flip)
            {
                body.transform.position = targetPosition;//changes in body will not reflect in transform in this frame
                //?? + targetDeltaVelocityWS * Time.fixedDeltaTime;
                body.transform.rotation = targetRotation;
                body.velocity = (attachedRigidbody.isKinematic ? Vector3.zero : attachedRigidbody.velocity) + targetDeltaVelocityWS;//TODO add angular support
                body.angularVelocity = Vector3.zero;
            }
            else
            {
                attachedRigidbody.transform.position = targetPosition;
                attachedRigidbody.transform.rotation = targetRotation;
                attachedRigidbody.velocity = (body.isKinematic ? Vector3.zero : body.velocity) + targetDeltaVelocityWS;
                attachedRigidbody.angularVelocity = Vector3.zero;
            }

            UpdateGrabJoint(joint, jointSetupInverseAttachedTimesBodyRotation, targetPosition, targetRotation, targetDeltaVelocityWS,flip:flip);
        }

        public static void SetIgnoreCollision(Rigidbody a, Rigidbody b, bool ignore)
        {
            if (a != null && b != null)
                foreach (var ac in a.GetComponentsInChildren<Collider>())
                    if (ac.attachedRigidbody == a)
                        foreach (var bc in b.GetComponentsInChildren<Collider>())
                            if (bc.attachedRigidbody == b)
                                Physics.IgnoreCollision(ac, bc, ignore);
        }



    }
}