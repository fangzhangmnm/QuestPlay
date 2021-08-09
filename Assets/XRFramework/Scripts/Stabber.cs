using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{
    public class Stabber : MonoBehaviour
    {
        public Rigidbody body;
        public Collider blade;
        public Transform stabStart;//y+
        public Transform stabEnd;
        public Vector3 stabDir => stabStart.up;
        public float maxFriction = 50f;

        Dictionary<Rigidbody, StabInfo> stabs = new Dictionary<Rigidbody, StabInfo>();

        class StabInfo
        {
            public ConfigurableJoint joint;
            public FixedJoint joint2;
        }
        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionStay(collision);
        }
        private void OnCollisionStay(Collision collision)
        {
            if (!isActiveAndEnabled) return;
            if (PhysicsTools.IsGhostCollision(collision)) return;
            if (collision.relativeVelocity.magnitude < 2f) return;
            if (Vector3.Dot(stabDir, -collision.relativeVelocity.normalized) < .8f) return;
            if (collision.contacts[0].thisCollider != blade) return;
            var stabable = collision.collider.GetComponentInParent<Stabable>();
            if (!stabable) return;
            var attachedBody = collision.collider.attachedRigidbody;
            if (stabs.ContainsKey(attachedBody)) return;
            Debug.Log($"{name} stabs {attachedBody.name}");

            var point = collision.contacts[0].point;
            var joint = attachedBody.gameObject.AddComponent<ConfigurableJoint>();

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = body;
            joint.anchor = attachedBody.transform.InverseTransformPoint(point);
            joint.connectedAnchor = body.transform.InverseTransformPoint(point);

            joint.xMotion = ConfigurableJointMotion.Limited;
            var linearLimit = new SoftJointLimit();
            linearLimit.limit = Vector3.Dot(stabDir, stabStart.position - stabEnd.position);
            joint.linearLimit = linearLimit;
            joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.axis = attachedBody.transform.InverseTransformDirection(stabDir);
            //joint.enableCollision = false; //why not working

            stabs.Add(attachedBody, new StabInfo { joint = joint });
            JointTools.SetIgnoreCollision(attachedBody, body, true);
        }
        List<Rigidbody> toRemove = new List<Rigidbody>();
        private void FixedUpdate()
        {
            foreach (var kv in stabs)
            {
                var attachedBody = kv.Key;
                var joint = kv.Value.joint;
                if (attachedBody == null)
                    toRemove.Add(attachedBody);
                else
                {
                    Vector3 anchorDelta = body.transform.TransformPoint(joint.connectedAnchor) - attachedBody.transform.TransformPoint(joint.anchor);
                    float depth = Vector3.Dot(anchorDelta, stabDir);
                    float depthVelocity = Vector3.Dot(body.velocity - attachedBody.velocity, stabDir);
                    if (depth < -Physics.defaultContactOffset && depthVelocity < 0)
                    {
                        Debug.Log($"{name} unstabs {attachedBody.name}");
                        if (kv.Value.joint) Destroy(joint);
                        if (kv.Value.joint2) Destroy(kv.Value.joint2);
                        JointTools.SetIgnoreCollision(attachedBody, body, false);
                        toRemove.Add(attachedBody);
                    }
                    else
                    {
                        //Add friction to eliminate depthVelocity;
                        float mr = attachedBody.isKinematic ? body.mass : body.mass * attachedBody.mass / (body.mass + attachedBody.mass);
                        Vector3 frictionForce = mr * depthVelocity * stabDir * (1f / Time.fixedDeltaTime);
                        frictionForce = Vector3.ClampMagnitude(frictionForce, maxFriction);
                        Vector3 point = attachedBody.transform.TransformPoint(joint.anchor);
                        body.AddForceAtPosition(-frictionForce, point);
                        attachedBody.AddForceAtPosition(frictionForce, point);

                        if (!kv.Value.joint2 && frictionForce.magnitude <= maxFriction * 1.0001f)
                        {
                            var joint2 = attachedBody.gameObject.AddComponent<FixedJoint>();
                            kv.Value.joint2 = joint2;
                            joint2.connectedBody = body;
                            joint2.autoConfigureConnectedAnchor = false;
                            joint2.connectedBody = body;
                            joint2.anchor = attachedBody.transform.InverseTransformPoint(point);
                            joint2.connectedAnchor = body.transform.InverseTransformPoint(point);
                            joint2.breakForce = maxFriction;
                        }
                    }
                }

            }
            foreach (var attachedBody in toRemove)
                stabs.Remove(attachedBody);
            toRemove.Clear();
        }
        private void OnDisable()
        {
            foreach (var kv in stabs)
            {
                JointTools.SetIgnoreCollision(kv.Key, body, false);
                if (kv.Value.joint) Destroy(kv.Value.joint);
                if (kv.Value.joint2) Destroy(kv.Value.joint2);
            }
            stabs.Clear();
        }
    }
}

