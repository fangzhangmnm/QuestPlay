using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace fzmnm.XRPlayer
{
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(Rigidbody))]
    public class XRPickup : XRInteractable
    {
        public enum AttachMode { FreeGrab, FreeGrabTwoHanded, Handle, PrimarySecondaryHandle, Pole };
        public bool canSwapHand = true;
        bool hand2Enabled => attachMode == AttachMode.FreeGrabTwoHanded
                            || attachMode == AttachMode.PrimarySecondaryHandle
                            || attachMode == AttachMode.Pole;
        bool hand2AutoDrop => false;// attachMode == AttachMode.PrimarySecondaryHandle;
        public AttachMode attachMode = AttachMode.FreeGrab;

        public Transform attachRef, attach2Ref;
        public JointSettings jointSettings;
        public float lostTrackDist = .3f;
        public float throwSmoothTime = .1f;
        public bool breakWhenLostTrack = false;

        public UnityEvent onPickUp, onDrop;
        [System.Serializable] public class UpdateTransform : UnityEvent<Transform> { }
        public UpdateTransform updateAttach = new UpdateTransform();

        Rigidbody body;
        [HideInInspector] public XRHand hand, hand2;
        ConfigurableJoint joint, joint2;
        Quaternion jointBias, jointBias2;
        Vector3 attachedPositionLS, attached2PositionLS, desiredPosition;
        Quaternion attachedRotationLS, attached2RotationLS, desiredRotation;
        Vector3 smoothedThorwVelocity, smoothedThrowAngularVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (!attachRef) attachRef = transform;
        }

        void OnDisable()
        {
            hand?.DetachIfAny();
            hand2?.DetachIfAny();
            onDrop.Invoke();
        }

        public void DetachIfAttached()
        {
            if (hand) hand.DetachIfAny();
        }
        public override bool CanInteract(XRHand hand, out int priority)
        {
            return CanAttach(hand, out priority);
        }
        public override bool CanAttach(XRHand hand, out int priority)
        {
            if (!isActiveAndEnabled) { priority = 0; return false; }
            if (hand2Enabled && this.hand && isActiveAndEnabled)
            {
                priority = -1;
                if (hand2) return false;
                else return true;
            }
            else
            {
                if (this.hand)
                {
                    priority = -2;
                    return canSwapHand;
                }
                else
                {
                    priority = 0;
                    return true;
                }
            }
        }

        public override void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS)
        {
            Debug.Log($"{name}.OnAttach");
            smoothedThorwVelocity = Vector3.zero;
            smoothedThrowAngularVelocity = Vector3.zero;
            if (!hand)
            {
                hand = emptyHand;
                (joint, jointBias) = JointTools.CreateJoint(body, emptyHand.trackedHand, jointSettings);
                attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
                attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
                ResetMovement();
                updateAttach.Invoke(emptyHand.playerRoot);
                onPickUp.Invoke();
            }
            else
            {
                if (!hand2Enabled)
                {
                    hand.TransforAttached(emptyHand);

                    hand = emptyHand;
                    (joint, jointBias) = JointTools.CreateJoint(body, emptyHand.trackedHand, jointSettings);
                    attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
                    attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;

                    //TODO Test Logic
                }
                else
                {
                    if (hand2) hand2.DetachIfAny();

                    hand2 = emptyHand;
                    (joint2, jointBias2) = JointTools.CreateJoint(body, emptyHand.trackedHand, jointSettings);
                    attached2PositionLS = transform.InverseTransformPoint(attachPositionWS);
                    attached2RotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
                }
            }
        }

        public override void OnDetach(XRHand handAttachedMe)
        {
            Debug.Log($"{name}.OnDetach");
            if (handAttachedMe == hand2)
            {
                Destroy(joint2); joint2 = null; hand2 = null;
                if (hand.hovering.IsHovering(this))
                {
                    attachedPositionLS = transform.InverseTransformPoint(hand.trackedPosition);
                    attachedRotationLS = Quaternion.Inverse(transform.rotation) * hand.trackedRotation;
                }
            }
            else
            {
                if (hand2)
                {
                    if (hand2AutoDrop)
                    {
                        hand2.DetachIfAny();
                        Destroy(joint); joint = null; hand = null;
                        _OnDrop();
                    }
                    else
                    {
                        Destroy(joint); joint = null; hand = null;

                        hand = hand2; hand2 = null;
                        joint = joint2; joint2 = null;
                        jointBias = jointBias2;
                        attachedPositionLS = attached2PositionLS;
                        attachedRotationLS = attached2RotationLS;
                    }
                }
                else
                {
                    Destroy(joint); joint = null; hand = null;
                    _OnDrop();
                }
            }

        }

        void _OnDrop()
        {
            body.velocity = smoothedThorwVelocity;
            body.angularVelocity = smoothedThrowAngularVelocity;
            onDrop.Invoke();
            updateAttach.Invoke(null);
        }
        /*
        public override (Vector3, Quaternion) GetAttachPosition(XRHand hand)
        {
            if (hand == this.hand) return (transform.TransformPoint(attachedPositionLS), transform.rotation * attachedRotationLS);
            else return (transform.TransformPoint(attached2PositionLS), transform.rotation * attached2RotationLS);
        }
        */
        private void FixedUpdate()
        {
            if (hasTeleportedThisFrame)
            {
                ResetMovement();
            }

            if (hand)
            {
                UpdateDesiredMovementAndAttachPosition();

                bool break1 = hand && Vector3.Distance(hand.trackedPosition, transform.TransformPoint(attachedPositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;
                bool break2 = hand2 && Vector3.Distance(hand2.trackedPosition, transform.TransformPoint(attached2PositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;

                if (break1 || break2)
                {
                    if (breakWhenLostTrack)
                    {
                        if (break1)
                        {
                            hand?.DetachIfAny();
                        }
                        else
                            hand2.DetachIfAny();
                    }
                    else
                        ResetMovement();
                }
                else
                {
                    //transform.position = desiredPosition;
                    //transform.rotation = desiredRotation;
                    if (joint)
                        JointTools.UpdateJoint(joint, jointBias, desiredPosition, desiredRotation, hand.estimatedTrackedVelocityWS);
                    if (joint2)
                        JointTools.UpdateJoint(joint2, jointBias2, desiredPosition, desiredRotation, hand2.estimatedTrackedVelocityWS);
                }
            }

            {
                smoothedThorwVelocity = Vector3.Lerp(smoothedThorwVelocity, body.velocity, Time.fixedDeltaTime / throwSmoothTime);
                smoothedThrowAngularVelocity = Vector3.Lerp(smoothedThrowAngularVelocity, body.angularVelocity, Time.fixedDeltaTime / throwSmoothTime);
            }

            if (hand && hand.device.grip < .5f)
                hand.DetachIfAny();
            if (hand2 && hand2.device.grip < .5f)
                hand2.DetachIfAny();

            hasTeleportedThisFrame = false;
        }

        private void OnCollisionEnter(Collision collision)//Will be called before start
        {
            if (hand)
            {
                float speed = 1f;
                if (collision.contactCount > 0)
                {
                    Vector3 point = collision.GetContact(0).point;
                    Vector3 v1 = body.GetPointVelocity(point);
                    Vector3 v2 = collision.rigidbody ? collision.rigidbody.GetPointVelocity(point) : Vector3.zero;
                    speed = (v2 - v1).magnitude / hand.playerRoot.lossyScale.x;
                }
                float strength = Mathf.Lerp(0, hand.hapticSettings.collisionEnterMaxStrength, speed / hand.hapticSettings.collisionEnterMaxStrengthSpeed);
                hand.device.SendHapticImpulse(strength, hand.hapticSettings.collisionEnterDuration);
            }
        }

        void ResetMovement()//TODO is it necessary
        {
            if (hand)
            {
                UpdateDesiredMovementAndAttachPosition();
                transform.position = desiredPosition;
                transform.rotation = desiredRotation;
                if (joint)
                    JointTools.TeleportJoint(joint, jointBias, desiredPosition, desiredRotation,hand.estimatedTrackedVelocityWS);
                if (joint2)
                    JointTools.TeleportJoint(joint2, jointBias2, desiredPosition, desiredRotation, hand2.estimatedTrackedVelocityWS);
            }

        }
        void UpdateDesiredMovementAndAttachPosition()
        {
            if (hand2 && attachMode == AttachMode.PrimarySecondaryHandle)
            {
                attached2PositionLS = transform.InverseTransformPoint(attach2Ref.position);
                attached2RotationLS = Quaternion.Inverse(transform.rotation) * attach2Ref.rotation;
            }
            if (hand && (attachMode == AttachMode.PrimarySecondaryHandle || attachMode == AttachMode.Handle))
            {
                attachedPositionLS = transform.InverseTransformPoint(attachRef.position);
                attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRef.rotation;
            }

            if (hand && !hand2)
            {
                desiredRotation = hand.trackedRotation * Quaternion.Inverse(attachedRotationLS);
                desiredPosition = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
            }
            else if (hand && hand2)
            {
                if (attachMode == AttachMode.PrimarySecondaryHandle)
                    desiredRotation = hand.trackedRotation * Quaternion.Inverse(attachedRotationLS);
                else
                {
                    var rot1 = hand.trackedRotation * Quaternion.Inverse(attachedRotationLS);
                    var rot2 = hand2.trackedRotation * Quaternion.Inverse(attached2RotationLS);
                    desiredRotation = Quaternion.Slerp(rot1, rot2, .5f);
                }
                var targetTargetAxisWS = transform.TransformVector(attached2PositionLS - attachedPositionLS);
                var handHandAxisWS = hand2.trackedPosition - hand.trackedPosition;
                var desiredDeltaRotation = desiredRotation * Quaternion.Inverse(transform.rotation);
                var alignRotation = Quaternion.FromToRotation(desiredDeltaRotation * targetTargetAxisWS, handHandAxisWS);
                desiredRotation = alignRotation * desiredRotation;

                if (attachMode == AttachMode.PrimarySecondaryHandle)
                {
                    desiredPosition = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
                }
                else if (attachMode == AttachMode.FreeGrabTwoHanded)
                {
                    var desiredPosition1 = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
                    var desiredPosition2 = hand2.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attached2PositionLS);

                    desiredPosition = (desiredPosition1 + desiredPosition2) / 2;

                    //attachedPositionLS = transform.InverseTransformPoint(hand.position);
                    //attachedRotationLS = Quaternion.Inverse(transform.rotation) * hand.rotation;
                    //attached2PositionLS = transform.InverseTransformPoint(hand2.position);
                    //attached2RotationLS = Quaternion.Inverse(transform.rotation) * hand2.rotation;
                    //will introduce sliding
                }
                else
                {
                    //need also attachposition update lol
                    throw new System.NotImplementedException();
                }
            }
        }

    }

}
