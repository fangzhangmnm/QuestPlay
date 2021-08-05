using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm.XRPlayer
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class KinematicCharacterCollider : MonoBehaviour
    {
        public LayerMask environmentLayers = ~0;
        public LayerMask groundLayers = ~0;
        public float stepHeight = .3f;
        public float slopeLimit = 60f;
        public float minHeight = .6f;
        public float maxHeight = 2.5f;

        public Vector3 StepOnGround(Vector3 delta, out bool isNextStepHit)
        {
            float stepDist = Mathf.Max(delta.magnitude, R * scale);
            isNextStepHit = PhysicsDebugger.SphereCast(TP(0, .9f * R + stepHeight, 0) + delta.normalized * Mathf.Max(.9f * R * scale, delta.magnitude),
                .9f * R * scale, TD(0, -1, 0), out RaycastHit nextStepHit, 2 * stepHeight * scale, groundLayers);
            if (isNextStepHit)
            {
                Vector3 nextStep = TP(0, .9f * R + stepHeight, 0) + delta.normalized * Mathf.Max(.9f * R * scale, delta.magnitude) + nextStepHit.distance * TD(0, -1, 0) + TV(0, -.9f * R, 0);
                if (Vector3.Dot(nextStepHit.point - transform.position, GravityUp) < Mathf.Sin(slopeLimit * Mathf.Deg2Rad))
                    delta = (nextStep - transform.position).normalized * delta.magnitude;
                else
                    delta = Vector3.zero;
            }
            return delta;
        }

        public Vector3 SweepCollider(Vector3 delta, bool slide)
        {
            //Sweep
            bool isHit = PhysicsDebugger.CapsuleCast(P1, P2, .9f * R * scale, delta.normalized, out RaycastHit hit, delta.magnitude + .1f * R * scale, environmentLayers);
            if (isHit)
            {
                Vector3 delta1 = delta.normalized * Mathf.Clamp(hit.distance - .1f * R * scale, 0, delta.magnitude);//Skinning is needed
                if (slide)
                    return delta1;
                else
                {
                    Vector3 delta2 = Vector3.ProjectOnPlane(delta - delta1, hit.normal);
                    bool isHit2 = PhysicsDebugger.CapsuleCast(P1 + delta1, P2 + delta1, .9f * R, delta2.normalized, out RaycastHit hit2, delta2.magnitude + .1f * R * scale, environmentLayers);
                    if (isHit2)
                    {
                        return delta1 + delta2.normalized * Mathf.Clamp(hit2.distance - .1f * R * scale, 0, delta2.magnitude);
                    }
                    else
                    {
                        return delta1 + delta2;
                    }
                }
            }
            else
                return delta;
        }
        public Vector3 ResolveCollision()
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            int overlapCount;
            overlapCount = Physics.OverlapCapsuleNonAlloc(P1, P2, R * scale * 1.1f, colliderBuffer, environmentLayers);
            capsuleCollider.enabled = true;
            for (int i = 0; i < overlapCount; ++i)
            {
                var c = colliderBuffer[i];
                //raycast won't collide with disabled colliders
                if (c == capsuleCollider) continue;
                if (Physics.ComputePenetration(capsuleCollider, position, rotation, c, c.transform.position, c.transform.rotation,
                    out Vector3 resolveDir, out float resolveDist))
                {
                    position += resolveDir * resolveDist;
                }
            }
            capsuleCollider.enabled = false;
            return position- capsuleCollider.transform.position;
        }
        public bool DetectGround(out float groundDist, out RaycastHit groundHit)
        {
            bool isThisStep= PhysicsDebugger.SphereCast(TP(0, .9f * R + stepHeight, 0),
                .9f * R * scale, TD(0, -1, 0), out groundHit, 2 * stepHeight * scale, groundLayers);
            groundDist = isThisStep ? groundHit.distance - stepHeight * scale : float.PositiveInfinity;
            return isThisStep;
        }
        public float DetectMaxHeight()
        {
            bool isHeadHit = PhysicsDebugger.SphereCast(TP(0, R, 0),
                .9f * R * scale, up, out RaycastHit headHit, maxHeight * scale, environmentLayers);
            return isHeadHit ? Mathf.Clamp( headHit.distance / scale + 1.9f * R, minHeight,maxHeight) : maxHeight;
        }
        public void SetHeight(float height)
        {
            capsuleCollider.height = Mathf.Clamp(height, minHeight, maxHeight);
            capsuleCollider.center= new Vector3(0, capsuleCollider.height / 2, 0);
        }



        [HideInInspector]public CapsuleCollider capsuleCollider;
        public float R => capsuleCollider.radius;
        public float H => capsuleCollider.height;
        public float scale => transform.lossyScale.x;
        public float RScaled => R * scale;
        public float HScaled => H * scale;
        Vector3 up => transform.up;
        Vector3 GravityUp => transform.up;
        Vector3 TP(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
        Vector3 TV(float x, float y, float z) => transform.TransformVector(new Vector3(x, y, z));
        Vector3 TD(float x, float y, float z) => transform.TransformDirection(new Vector3(x, y, z));
        Vector3 P1 => TP(0, R, 0);
        Vector3 P2 => TP(0, H - R, 0);
        Collider[] colliderBuffer = new Collider[20];
        private void Awake()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            capsuleCollider.enabled = false;
        }
    }
}