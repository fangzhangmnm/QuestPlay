using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{
    public class Grabable : MonoBehaviour
    {
        List<Grabber> grabbers;

        public bool _CanGrab(Grabber grabber) { return true; }
        public void _AddGrab(Grabber grabber,Vector3 attachPositionWS,Quaternion attachRotationWS)
        {
            Debug.Assert(!grabber.joint && !grabber.grabbed);
            (grabber.joint, grabber.jointBias) = JointTools.CreateJoint(body, grabber.body, grabber.jointSettings);
            grabber.attachPositionLS = transform.InverseTransformPoint(attachPositionWS);
            grabber.attachRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
            grabber.grabbed = this;
        }
        public void _RemoveGrab(Grabber grabber)
        {
            Debug.Assert(grabbers.Contains(grabber));

            Destroy(grabber.joint); 
            grabber.joint = null;
            grabber.grabbed = null;
            grabbers.Remove(grabber);
        }
        private void OnDisable()
        {
            foreach (var grabber in grabbers)
                grabber.DropIfAny();
        }
        Rigidbody body;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }
    }
}
