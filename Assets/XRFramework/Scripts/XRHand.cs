using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
namespace fzmnm.XRPlayer
{
    [DefaultExecutionOrder(-9)]
    public class XRHand:MonoBehaviour
    {
        public XRNode whichHand;
        public XRLocomotion playerLocomotion;
        public Rigidbody trackedHand;
        public JointSettings jointSettings;
        public XRHandHovering hovering;
        public HapticSettings hapticSettings;

        public Vector3 trackedPosition => trackedHand.transform.position;//shoud not use body's
        public Quaternion trackedRotation => trackedHand.transform.rotation;

        public static XRHand leftHand, rightHand;
        public XRHand otherHand => this == leftHand ? rightHand : leftHand;

        public XRInteractable attached { get; private set; }
        [HideInInspector] public bool isEmpty => !attached;
        [HideInInspector] public Transform playerRoot;
        [HideInInspector] public XRController device;
        [HideInInspector] public bool mouseControl = false;

        Vector3 oldTrackedPositionTS;
        [HideInInspector] public Vector3 estimatedTrackedVelocityWS;

        private void Start()
        {
            oldTrackedPositionTS = playerLocomotion.trackingSpace.InverseTransformPoint(trackedPosition);

        }

        static List<XRNodeState> nodeStates = new List<XRNodeState>();
        private void FixedUpdate()
        {
            if (!mouseControl)
                device.ReadDeviceInputs();

            InputTracking.GetNodeStates(nodeStates);
            foreach (var nodeState in nodeStates)
                if(nodeState.nodeType==whichHand)
                {
                    if (nodeState.TryGetPosition(out Vector3 position))
                        trackedHand.transform.position = playerLocomotion.trackingSpace.TransformPoint(position);
                    if (nodeState.TryGetRotation(out Quaternion rotation))
                        trackedHand.transform.rotation = playerLocomotion.trackingSpace.rotation * rotation;
                }


            //Estimate controller velocity in world space
            //do not smooth for faster response and robust against nan
            Vector3 newTrackedPositionTS = playerLocomotion.trackingSpace.InverseTransformPoint(trackedPosition);
            estimatedTrackedVelocityWS = playerLocomotion.estimatedPlayerRootVelocity + playerLocomotion.trackingSpace.TransformVector(newTrackedPositionTS - oldTrackedPositionTS) / Time.fixedDeltaTime;
            oldTrackedPositionTS = newTrackedPositionTS;


            hovering.UpdateHovering(isEmpty);
            UpdateIgnoreCollision();
            if (device.grip > .5f && isEmpty && hovering.current && !lastgrip)
            {
                if (hovering.current.CanAttach(this, out _))
                    Attach(hovering.current, hovering.hitPosition, hovering.hitRotation);
            }
            lastgrip = device.grip > .5f;
            if (device.trigger > .5f && isEmpty && hovering.current && !lasttrigger)
            {
                if (hovering.current.CanInteract(this, out _))
                    hovering.current.OnInteract(this);
            }
        }
        public void Attach(XRInteractable interactable, Vector3 handAttachPositionWS, Quaternion handAttachRotationWS)
        {
            if (!interactable.CanAttach(this, out _)) return;
            DetachIfAny();

            attached = interactable;
            foreach (var c in attached.GetComponentsInChildren<Collider>())
                IgnoreCollider(c);

            interactable.OnAttach(this, handAttachPositionWS, handAttachRotationWS);
            device.SendHapticImpulse(hapticSettings.attachStrength, hapticSettings.attachDuration);
        }
        public void DetachIfAny()
        {
            if (attached)
            {
                attached.OnDetach(this);
                attached = null;
                device.SendHapticImpulse(hapticSettings.detachStrength, hapticSettings.detachDuration);
            }
            attached = null;
        }
        public void TransforAttached(XRHand other)
        {
            if (attached)
            {
                other.DetachIfAny();
                other.attached = attached;
                foreach (var c in attached.GetComponentsInChildren<Collider>())
                    other.IgnoreCollider(c);
                attached = null;


                device.SendHapticImpulse(hapticSettings.detachStrength, hapticSettings.detachDuration);
                other.device.SendHapticImpulse(other.hapticSettings.attachStrength, other.hapticSettings.attachDuration);
            }
        }
        public void OnTeleport(Vector3 playerVelocity)
        {
            attached?.OnTeleport(playerVelocity);
        }
        #region IgnoreCollision
        //TODO read the code
        List<Collider> ignoredColliders = new List<Collider>();
        Collider[] colliderBuffer = new Collider[50];
        [HideInInspector] public Collider[] handColliders;
        void InitIgnoreColliders()
        {
            handColliders = GetComponentsInChildren<Collider>();
            foreach (var c in playerRoot.GetComponentsInChildren<Collider>())
                foreach (var cc in handColliders)
                    Physics.IgnoreCollision(c, cc, true);
        }
        void IgnoreCollider(Collider c)
        {
            if (!ignoredColliders.Exists(x => x == c))
            {
                ignoredColliders.Add(c);

                foreach (var cc in handColliders)
                {
                    //Debug.Log($"IgnoreCollision {c} {cc}");
                    Physics.IgnoreCollision(cc, c, true);
                }
            }
        }
        void UpdateIgnoreCollision()
        {
            int n = Physics.OverlapSphereNonAlloc(hovering.grabSphere.transform.TransformPoint(hovering.grabSphere.center), hovering.grabSphere.transform.lossyScale.x * hovering.grabSphere.radius, colliderBuffer, hovering.interactionLayer);
            for (int i = ignoredColliders.Count - 1; i >= 0; --i)
            {
                var c = ignoredColliders[i];
                if (c == null) continue;
                bool found = false;
                for (int j = 0; j < n; ++j) if (c == colliderBuffer[j]) { found = true; break; }
                if (!found && attached && c.GetComponentInParent<XRInteractable>() == attached && attached.isActiveAndEnabled) continue;
                if (!found)
                {
                    foreach (var cc in handColliders)
                    {
                        Physics.IgnoreCollision(cc, ignoredColliders[i], false);
                        //Debug.Log($"UnIgnoreCollision {c} {cc}");
                    }
                    ignoredColliders.RemoveAt(i);
                }
            }
        }
        void ClearAllIgnoredColliders()
        {
            foreach (var c in ignoredColliders)
                if (c)
                    foreach (var cc in handColliders)
                        Physics.IgnoreCollision(cc, c, false);
            ignoredColliders.Clear();
        }
        #endregion

        #region Haptics
        [System.Serializable]
        public class HapticSettings
        {
            public float hoveringEnterStrength = .1f;
            public float hoveringEnterDuration = .01f;
            public float hoveringExitStrength = 0f;
            public float hoveringExitDuration = .01f;
            public float attachStrength = .5f;
            public float attachDuration = .01f;
            public float detachStrength = .5f;
            public float detachDuration = .01f;
            public float collisionEnterMaxStrength = 1f;
            public float collisionEnterMaxStrengthSpeed = 2f;
            public float collisionEnterDuration = .01f;
        }
        private void OnCollisionEnter(Collision collision)//Will be called before start
        {
            float speed = 1f;
            if (collision.contactCount > 0)
            {
                Vector3 point = collision.GetContact(0).point;
                Vector3 v1 = body.GetPointVelocity(point);
                Vector3 v2 = collision.rigidbody ? collision.rigidbody.GetPointVelocity(point) : Vector3.zero;
                speed = playerLocomotion.trackingSpace.InverseTransformVector(v2 - v1).magnitude;
            }
            float strength = Mathf.Lerp(0, hapticSettings.collisionEnterMaxStrength, speed / hapticSettings.collisionEnterMaxStrengthSpeed);
            device.SendHapticImpulse(strength, hapticSettings.collisionEnterDuration);

            //if (collision.rigidbody)Debug.Log("Collide with body " + collision.rigidbody.name + body.velocity + " " + collision.rigidbody.velocity);
        }
        #endregion
        bool lastgrip = false, lasttrigger = false;
        Rigidbody body;
        private void Awake()
        {
            playerRoot = GetComponentInParent<XRLocomotion>().transform;
            body = GetComponent<Rigidbody>();
            device = new XRController(whichHand);
            hovering.Init(this);
        }
        private void OnEnable()//should not use awake for network compatibility
        {
            if (whichHand == XRNode.LeftHand) leftHand = this;
            if (whichHand == XRNode.RightHand) rightHand = this;

            InitIgnoreColliders();

            playerLocomotion.onTeleport.AddListener(OnTeleport);
            hovering.onNewHoveringEnter.AddListener(() => device.SendHapticImpulse(hapticSettings.hoveringEnterStrength, hapticSettings.hoveringEnterDuration));
            hovering.onExitAllHovering.AddListener(() => device.SendHapticImpulse(hapticSettings.hoveringExitStrength, hapticSettings.hoveringExitDuration));
        }
        private void OnDisable()
        {
            DetachIfAny();
            ClearAllIgnoredColliders();
            playerLocomotion.onTeleport.RemoveListener(OnTeleport);
            hovering.onNewHoveringEnter.RemoveAllListeners();
            hovering.onExitAllHovering.RemoveAllListeners();
        }

        private void OnValidate()
        {
            Debug.Assert(whichHand == XRNode.LeftHand || whichHand == XRNode.RightHand);
            Debug.Assert(trackedHand.isKinematic);
        }
    }
}