using UnityEngine;
using UnityEngine.Events;


/*
 * TODO
 * isPickedUp, which hand, which hand's input events
 * 
 * 
 */
namespace fzmnm.XRPlayer
{
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(Rigidbody))]
    public class XRPickup : XRInteractable
    {

        #region API
        public XRHand hand { get; protected set; }
        public XRHand secondaryHand { get; protected set; }
        public Vector3 handAttachPositionLS { get; protected set; }
        public Vector3 secondaryHandAttachPositionLS { get; protected set; }
        public Quaternion handAttachRotationLS { get; protected set; }
        public Quaternion secondaryHandAttachRotationLS { get; protected set; }
        [HideInInspector] public UnityEvent onPickUp, onDrop, onInteractWhenPicked;
        public bool isPickedUp => hand != null;
        public void Attach(XRHand hand) => _Attach(hand);
        public void Drop() => _Drop();
        #endregion

        #region Configure
        public enum AttachMode { FreeGrab, FreeGrabTwoHanded, Handle, PrimarySecondaryHandle, Pole };
        public bool canSwapHand = true;
        bool hand2Enabled => attachMode == AttachMode.FreeGrabTwoHanded
                            || attachMode == AttachMode.PrimarySecondaryHandle
                            || attachMode == AttachMode.Pole;
        bool hand2AutoDrop => false;// attachMode == AttachMode.PrimarySecondaryHandle;
        public AttachMode attachMode = AttachMode.FreeGrab;

        public Transform attachRef, secondaryAttachRef;
        public JointSettings jointSettingsOverride;
        public float lostTrackDist = .6f;
        //public float throwSmoothTime = .1f;
        public bool breakWhenLostTrack = false;
        #endregion


        Rigidbody body;
        ConfigurableJoint joint, secondaryJoint;
        Quaternion jointBias, secondaryJointBias;
        const bool jointFlip = true;
        Vector3 desiredPosition;
        Quaternion desiredRotation;
        //Vector3 smoothedThorwVelocity, smoothedThrowAngularVelocity;
        [HideInInspector] public UnityEvent<Transform> updateAttachNotImplemented = new UnityEvent<Transform>();

        void _Drop()
        {
            if (secondaryHand) secondaryHand.DetachIfAny();
            if (hand) hand.DetachIfAny();
        }
        void _Attach(XRHand hand)
        {
            if (this.hand == hand || this.secondaryHand == hand) return;
            if (hand == null) return;
            hand.DetachIfAny();
            hand.Attach(this,transform.position,transform.rotation);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (!attachRef) attachRef = transform;
        }

        void OnDisable()
        {
            hand?.DetachIfAny();
            secondaryHand?.DetachIfAny();
            onDrop.Invoke();
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
                if (secondaryHand) return false;
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

        public override void OnInteract(XRHand hand)
        {
            onInteractWhenPicked.Invoke();
        }

        public override void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS)
        {
            Debug.Log($"{name}.OnAttach");
            //smoothedThorwVelocity = Vector3.zero;//TODO bug
            //smoothedThrowAngularVelocity = Vector3.zero;

            JointSettings jointSettings = jointSettingsOverride != null ? jointSettingsOverride : emptyHand.jointSettings;
            if (!hand)
            {
                hand = emptyHand;
                (joint, jointBias) = JointTools.CreateGrabJoint(body, emptyHand.trackedHand, jointSettings,flip:jointFlip);
                handAttachPositionLS = transform.InverseTransformPoint(attachPositionWS);
                handAttachRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
                ResetMovement();
                updateAttachNotImplemented.Invoke(emptyHand.playerRoot);
                onPickUp.Invoke();
            }
            else
            {
                if (!hand2Enabled)
                {
                    hand.TransforAttached(emptyHand);

                    hand = emptyHand;
                    (joint, jointBias) = JointTools.CreateGrabJoint(body, emptyHand.trackedHand, jointSettings, flip: jointFlip);
                    handAttachPositionLS = transform.InverseTransformPoint(attachPositionWS);
                    handAttachRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;

                    //TODO Test Logic
                }
                else
                {
                    if (secondaryHand) secondaryHand.DetachIfAny();

                    secondaryHand = emptyHand;
                    (secondaryJoint, secondaryJointBias) = JointTools.CreateGrabJoint(body, emptyHand.trackedHand, jointSettings, flip: jointFlip);
                    secondaryHandAttachPositionLS = transform.InverseTransformPoint(attachPositionWS);
                    secondaryHandAttachRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
                }
            }
        }

        public override void OnDetach(XRHand handAttachedMe)
        {
            Debug.Log($"{name}.OnDetach");
            if (handAttachedMe == secondaryHand)
            {
                Destroy(secondaryJoint); secondaryJoint = null; secondaryHand = null;
                if (hand.hovering.IsHovering(this))
                {
                    handAttachPositionLS = transform.InverseTransformPoint(hand.trackedPosition);
                    handAttachRotationLS = Quaternion.Inverse(transform.rotation) * hand.trackedRotation;
                }
            }
            else
            {
                if (secondaryHand)
                {
                    if (hand2AutoDrop)
                    {
                        secondaryHand.DetachIfAny();
                        Destroy(joint); joint = null; hand = null;
                        _OnDrop();
                    }
                    else
                    {
                        Destroy(joint); joint = null; hand = null;

                        hand = secondaryHand; secondaryHand = null;
                        joint = secondaryJoint; secondaryJoint = null;
                        jointBias = secondaryJointBias;
                        handAttachPositionLS = secondaryHandAttachPositionLS;
                        handAttachRotationLS = secondaryHandAttachRotationLS;
                    }
                }
                else
                {
                    Destroy(joint); joint = null; hand = null;
                    _OnDrop();
                }
            }

        }
        public override bool GetAttachPosition(XRHand hand, out Vector3 position, out Quaternion rotation)
        {
            if (hand == this.hand)
            {
                position = transform.TransformPoint(handAttachPositionLS);
                rotation = transform.rotation * handAttachRotationLS;
                return true;
            }
            else if(hand==this.secondaryHand)
            {
                position = transform.TransformPoint(secondaryHandAttachPositionLS);
                rotation = transform.rotation * secondaryHandAttachRotationLS;
                return true;
            }
            else
                return base.GetAttachPosition(hand, out position, out rotation);
        }

        void _OnDrop()
        {
            //body.velocity = rawThrowVelocityWS;
            //body.angularVelocity = rawThrowAngularVelocityWS;
            onDrop.Invoke();
            updateAttachNotImplemented.Invoke(null);
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

                bool break1 = hand && Vector3.Distance(hand.trackedPosition, transform.TransformPoint(handAttachPositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;
                bool break2 = secondaryHand && Vector3.Distance(secondaryHand.trackedPosition, transform.TransformPoint(secondaryHandAttachPositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;

                if (break1 || break2)
                {
                    if (breakWhenLostTrack)
                    {
                        if (break1)
                        {
                            hand?.DetachIfAny();
                        }
                        else
                            secondaryHand.DetachIfAny();
                    }
                    else
                        ResetMovement();
                }
                else
                {
                    //transform.position = desiredPosition;
                    //transform.rotation = desiredRotation;
                    if (joint)
                        JointTools.UpdateGrabJoint(joint, jointBias, desiredPosition, desiredRotation, hand.estimatedTrackedVelocityWS,flip:jointFlip);
                    if (secondaryJoint)
                        JointTools.UpdateGrabJoint(secondaryJoint, secondaryJointBias, desiredPosition, desiredRotation, secondaryHand.estimatedTrackedVelocityWS, flip: jointFlip);
                }
            }

            {
                //smoothedThorwVelocity = Vector3.Lerp(smoothedThorwVelocity, body.velocity, Time.fixedDeltaTime / throwSmoothTime);
                //smoothedThrowAngularVelocity = Vector3.Lerp(smoothedThrowAngularVelocity, body.angularVelocity, Time.fixedDeltaTime / throwSmoothTime);
            }

            if (hand && hand.device.grip < .5f)
                hand.DetachIfAny();
            if (secondaryHand && secondaryHand.device.grip < .5f)
                secondaryHand.DetachIfAny();

            hasTeleportedThisFrame = false;
        }

        bool trueCollisionEnterTriggered;
        private void OnCollisionEnter(Collision collision)//Will be called before start
        {
            trueCollisionEnterTriggered = false;
            if (!trueCollisionEnterTriggered && !JointTools.IsGhostCollision(collision))
                OnTrueCollisionEnter(collision);
        }
        private void OnCollisionStay(Collision collision)
        {
            if (!trueCollisionEnterTriggered && !JointTools.IsGhostCollision(collision))
                OnTrueCollisionEnter(collision);
        }
        void OnTrueCollisionEnter(Collision collision)
        {
            trueCollisionEnterTriggered = true;

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
                    JointTools.TeleportGrabJoint(joint, jointBias, desiredPosition, desiredRotation,hand.estimatedTrackedVelocityWS, flip: jointFlip);
                if (secondaryJoint)
                    JointTools.TeleportGrabJoint(secondaryJoint, secondaryJointBias, desiredPosition, desiredRotation, secondaryHand.estimatedTrackedVelocityWS, flip: jointFlip);
            }

        }


        void UpdateDesiredMovementAndAttachPosition()
        {
            if (secondaryHand && attachMode == AttachMode.PrimarySecondaryHandle)
            {
                secondaryHandAttachPositionLS = transform.InverseTransformPoint(secondaryAttachRef.position);
                secondaryHandAttachRotationLS = Quaternion.Inverse(transform.rotation) * secondaryAttachRef.rotation;
            }
            if (hand && (attachMode == AttachMode.PrimarySecondaryHandle || attachMode == AttachMode.Handle))
            {
                handAttachPositionLS = transform.InverseTransformPoint(attachRef.position);
                handAttachRotationLS = Quaternion.Inverse(transform.rotation) * attachRef.rotation;
            }

            if (hand && !secondaryHand)
            {
                desiredRotation = hand.trackedRotation * Quaternion.Inverse(handAttachRotationLS);
                desiredPosition = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(handAttachPositionLS);
            }
            else if (hand && secondaryHand)
            {
                if (attachMode == AttachMode.PrimarySecondaryHandle)
                    desiredRotation = hand.trackedRotation * Quaternion.Inverse(handAttachRotationLS);
                else
                {
                    var rot1 = hand.trackedRotation * Quaternion.Inverse(handAttachRotationLS);
                    var rot2 = secondaryHand.trackedRotation * Quaternion.Inverse(secondaryHandAttachRotationLS);
                    desiredRotation = Quaternion.Slerp(rot1, rot2, .5f);
                }
                var targetTargetAxisWS = transform.TransformVector(secondaryHandAttachPositionLS - handAttachPositionLS);
                var handHandAxisWS = secondaryHand.trackedPosition - hand.trackedPosition;
                var desiredDeltaRotation = desiredRotation * Quaternion.Inverse(transform.rotation);
                var alignRotation = Quaternion.FromToRotation(desiredDeltaRotation * targetTargetAxisWS, handHandAxisWS);
                desiredRotation = alignRotation * desiredRotation;

                if (attachMode == AttachMode.PrimarySecondaryHandle)
                {
                    desiredPosition = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(handAttachPositionLS);
                }
                else if (attachMode == AttachMode.FreeGrabTwoHanded)
                {
                    var desiredPosition1 = hand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(handAttachPositionLS);
                    var desiredPosition2 = secondaryHand.trackedPosition - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(secondaryHandAttachPositionLS);

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
        public override void OnValidate()
        {
            base.OnValidate();
            body = GetComponent<Rigidbody>();
            if (body.collisionDetectionMode == CollisionDetectionMode.ContinuousSpeculative)
                Debug.LogWarning("ContinuousSpeculative CollisionDetectionMode will raise ghost OnCollisionEnter on fast moving vehicles, " +
                    "However, it is still recommended for sword-like objects");
            if (body.interpolation != RigidbodyInterpolation.None)
                Debug.LogError("Disable interpolation because we want to sync graphics and physics");
        }

    }

}
