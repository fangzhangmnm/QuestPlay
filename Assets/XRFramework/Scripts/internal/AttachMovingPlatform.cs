using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    public class AttachMovingPlatform
    {
        // Important, give animated platform a rigidbody, and animator.updatemode=animate physics
        public Transform attached { get; private set; }
        public Rigidbody attachedBody { get; private set; }
        Vector3 attachAnchorPositionLS = Vector3.zero, attachedPositionLS;
        public Vector3 attachedVelocity { get; private set; } = Vector3.zero;
        Transform transform;
        public AttachMovingPlatform(Transform transform)
        {
            this.transform = transform;
        }
        //TODO Attach
        public Vector3 GetAttachedTranslation(float dt = 0, float smoothTime = .01f)
        {
            if (attached)
            {
                Vector3 translation = attached.TransformPoint(attachedPositionLS) - transform.TransformPoint(attachAnchorPositionLS);
                //fast moving vehicles, predict the position after the physics frame
                if (attachedBody) translation += attachedBody.velocity * Time.fixedDeltaTime;
                if (dt > 0)
                    attachedVelocity = Vector3.Lerp(attachedVelocity, translation / dt, dt / smoothTime);

                return translation;
            }
            else return Vector3.zero;
        }
        public void ClearAttach()
        {
            this.attached = null;
            this.attachAnchorPositionLS = Vector3.zero;
            attachedVelocity = Vector3.zero;
        }
        public void SetAttach(Transform attached, Vector3 attachAnchorPositionLS)
        {
            if(this.attached!=attached)
                attachedVelocity = attachedBody ? attachedBody.GetPointVelocity(transform.position) : Vector3.zero;
            this.attached = attached;
            this.attachAnchorPositionLS = attachAnchorPositionLS;
            if (attached) attachedBody = attached.GetComponentInParent<Rigidbody>();
            if (attachedBody && attachedBody.isKinematic && attachedBody.transform.parent)
            {
                var parentBody = attachedBody.transform.parent.GetComponentInParent<Rigidbody>();
                if (parentBody) attachedBody = parentBody;
            }
            Vector3 attachedPointWS = transform.TransformPoint(attachAnchorPositionLS);
            //Compensate the correlation in GetAttachedTranslation
            if (attachedBody) attachedPointWS -= attachedBody.velocity * Time.fixedDeltaTime;
            if (attached) attachedPositionLS = attached.InverseTransformPoint(attachedPointWS);
        }
        /*
        public void SetAttach(Transform attached, Vector3 attachedPositionLS, Vector3 attachAnchorPositionLS, float dt)
        {
            SetAttach(attached, attachedPositionLS, dt);
            this.attachAnchorPositionLS = attachAnchorPositionLS;
        }
        */
    }
}