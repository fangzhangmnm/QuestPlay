using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm.XRPlayer
{
    public abstract class XRInteractable : MonoBehaviour
    {
        public virtual bool CanInteract(XRHand hand, out int priority) { priority = 0; return true; }
        public virtual bool CanAttach(XRHand hand, out int priority) { priority = 0; return false; }
        public virtual void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS) { }
        public virtual void OnDetach(XRHand handAttachedMe) { }
        public virtual bool GetAttachPosition(XRHand hand, out Vector3 position, out Quaternion rotation) 
        { 
            position = hand.trackedPosition; 
            rotation = hand.trackedRotation;
            return false; 
        }


        public void OnTeleport(Vector3 playerVelocity) { hasTeleportedThisFrame = true; }
        public bool hasTeleportedThisFrame { get; protected set; } = true;
        public virtual void OnInteract(XRHand hand) { }
        public Behaviour hoveringEffect;
        public virtual void SetHovering(bool isHovering) { hoveringEffect.enabled = isHovering; }
        public virtual void OnEnable() { hoveringEffect.enabled = false; }

        public virtual void OnValidate()
        {
            if (!LayerMask.LayerToName(gameObject.layer).StartsWith("Interactable"))
                Debug.LogError("The Layer for interactable objects must be \"InteractableXXX\"");
            //Debug.Assert(gameObject.GetComponent<Rigidbody>());
        }
    }
}