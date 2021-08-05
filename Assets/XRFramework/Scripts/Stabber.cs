using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stabber : MonoBehaviour
{
    public Rigidbody body;
    public Collider blade;
    public Transform stabStart;//y+
    public Transform stabEnd;
    public Vector3 stabDir => stabStart.up;

    Dictionary<Rigidbody, ConfigurableJoint> stabs = new Dictionary<Rigidbody, ConfigurableJoint>();
    private void OnCollisionEnter(Collision collision)
    {
        if (!isActiveAndEnabled)return;
        if (collision.relativeVelocity.magnitude < 2f)return;
        if (Vector3.Dot(stabDir, -collision.relativeVelocity.normalized) < .8f)return;
        if (collision.contacts[0].thisCollider != blade)return;
        var stabable = collision.collider.GetComponentInParent<Stabable>();
        if (!stabable)return;
        var attachedBody = collision.collider.attachedRigidbody;
        if (stabs.ContainsKey(attachedBody))return;
        Debug.Log($"{name} stabs {attachedBody.name}");
        
        var point = collision.contacts[0].point;
        var joint = attachedBody.gameObject.AddComponent<ConfigurableJoint>();

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = body;
        joint.anchor = attachedBody.transform.InverseTransformPoint(point);
        joint.connectedAnchor = body.transform.InverseTransformPoint(point);

        joint.xMotion = ConfigurableJointMotion.Limited;
        var linearLimit = new SoftJointLimit();
        linearLimit.limit= Vector3.Dot(stabDir, stabStart.position - stabEnd.position);
        joint.linearLimit = linearLimit;
        joint.yMotion  = joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.axis = attachedBody.transform.InverseTransformDirection(stabDir);
        joint.enableCollision = false; //why not working



        stabs.Add(attachedBody, joint);
        SetIgnoreCollision(attachedBody, body, true);
    }
    static void SetIgnoreCollision(Rigidbody a, Rigidbody b, bool ignore)
    {
        foreach (var ac in a.GetComponentsInChildren<Collider>())
            if (ac.attachedRigidbody == a)
                foreach (var bc in b.GetComponentsInChildren<Collider>())
                    if (bc.attachedRigidbody == b)
                        Physics.IgnoreCollision(ac, bc, ignore);
    }
    List<Rigidbody> toRemove = new List<Rigidbody>();
    private void FixedUpdate()
    {
        foreach (var k in stabs)
        {
            var attachedBody = k.Key;
            var joint = k.Value;
            if (attachedBody == null)
                toRemove.Add(attachedBody);
            else
            {
                Vector3 anchorDelta = body.transform.TransformPoint(joint.connectedAnchor) - attachedBody.transform.TransformPoint(joint.anchor);
                float depth = Vector3.Dot(anchorDelta, stabDir);
                if (depth < -.05f)
                {
                    Debug.Log($"{name} unstabs {attachedBody.name}");
                    Destroy(joint);
                    SetIgnoreCollision(attachedBody, body, false);
                    toRemove.Add(attachedBody);
                }
            }
               
        }
        foreach(var attachedBody in toRemove)
            stabs.Remove(attachedBody);
        toRemove.Clear();
    }
}
