using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;
namespace fzmnm.XRPlayer
{
    [System.Serializable]
    public class XRHandHovering
    {
        public LayerMask interactionLayer;
        public SphereCollider grabSphere;
        public Transform grabHandTransform;
        [Tooltip("measured in grabHand's scale")]
        public float pickUpDistance = 1.5f;

        [HideInInspector] public XRInteractable current;
        [HideInInspector] public Vector3 hitPosition;
        [HideInInspector] public Quaternion hitRotation;

        [HideInInspector] public UnityEvent onNewHoveringEnter, onExitAllHovering;


        XRInteractable FindInteractableFromCollider(Collider c)
        {
            //return c.attachedRigidbody ? c.attachedRigidbody.GetComponent<XRInteractable>() : c.GetComponent<XRInteractable>();
            return c.GetComponentInParent<XRInteractable>();
        }

        public bool IsHovering(XRInteractable interactable)
        {
            int grabHitNum = Physics.SphereCastNonAlloc(grabSphere.transform.position - grabSphere.radius * grabSphere.transform.localScale.x * grabSphere.transform.forward,
                                grabSphere.radius * grabSphere.transform.localScale.x,
                                grabSphere.transform.forward,
                                hitBuffer,
                                (grabSphere.center.z + grabSphere.radius) * grabSphere.transform.localScale.x,
                                interactionLayer);

            for (int i = 0; i < grabHitNum; ++i)
            {
                var hit = hitBuffer[i];
                var it = FindInteractableFromCollider(hit.collider);
                if (it == interactable) return true;
            }
            return false;
        }
        public void UpdateHovering(bool canHover)
        {
            XRInteractable newHovering = null;
            if (canHover)
            {
                int grabHitNum = Physics.OverlapSphereNonAlloc(grabSphere.transform.TransformPoint(grabSphere.center),
                                    grabSphere.radius * grabSphere.transform.localScale.x,
                                    colliderBuffer,
                                    interactionLayer,
                                    QueryTriggerInteraction.Collide);
                int maxP = int.MinValue; float minD = float.MaxValue;
                for (int i = 0; i < grabHitNum; ++i)
                {
                    var cld = colliderBuffer[i];
                    var it = FindInteractableFromCollider(cld);
                    if (!it) continue;
                    Vector3 clostestPoint = cld.ClosestPoint(grabSphere.transform.position);
                    if (Vector3.Dot(grabSphere.transform.forward, clostestPoint - grabSphere.transform.position) < 0) continue;
                    float distance = Vector3.Distance(clostestPoint, grabSphere.transform.position);
                    if (!it.isActiveAndEnabled) continue;
                    if (it.CanInteract(hand, out int p))
                    {
                        if (p > maxP || p == maxP && distance < minD)
                        {
                            maxP = p; minD = distance;
                            newHovering = it;
                            hitPosition = clostestPoint;
                            hitRotation = grabHandTransform.rotation;
                        }
                    }
                }
                if (!newHovering)
                {
                    if (Physics.SphereCast(grabSphere.transform.TransformPoint(grabSphere.center) + grabHandTransform.forward * grabSphere.radius * grabSphere.transform.localScale.x * .0f,
                        grabSphere.radius * grabSphere.transform.localScale.x,
                        grabHandTransform.forward,
                        out RaycastHit hit2,
                        grabHandTransform.lossyScale.x * pickUpDistance,
                        interactionLayer,
                        QueryTriggerInteraction.Collide))
                    {
                        var it = FindInteractableFromCollider(hit2.collider);
                        if (it && it.isActiveAndEnabled && it.CanInteract(hand, out _))
                        {
                            newHovering = it;
                            hitPosition = hit2.point;
                            hitRotation = grabHandTransform.rotation;
                        }
                    }
                }
            }

            if (current != newHovering && current != null && current != hand.otherHand.hovering.current)
                current.SetHovering(false);

            if (newHovering)
                newHovering.SetHovering(true);

            if (current != newHovering)
            {
                if (newHovering == null)
                    onExitAllHovering.Invoke();
                //controller.SendHapticImpulse(hapticSettings.hoveringExitStrength, hapticSettings.hoveringEnterDuration);
                else
                    onNewHoveringEnter.Invoke();
                    //controller.SendHapticImpulse(hapticSettings.hoveringEnterStrength, hapticSettings.hoveringEnterDuration);
            }

            current = newHovering;
        }
        public void Init(XRHand hand)
        {
            this.hand = hand;
        }
        [HideInInspector] public XRHand hand;

        RaycastHit[] hitBuffer = new RaycastHit[50];
        Collider[] colliderBuffer = new Collider[50];
    }
}
