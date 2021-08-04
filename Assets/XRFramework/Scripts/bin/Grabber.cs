using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm.XRPlayer
{
    public class Grabber : MonoBehaviour
    {
        public JointSettings jointSettings;

        //Managed by grabable, dont modify
        [HideInInspector] public Grabable grabbed;
        [HideInInspector] public ConfigurableJoint joint;
        [HideInInspector] public Quaternion jointBias;
        [HideInInspector] public Vector3 attachPositionLS;
        [HideInInspector] public Quaternion attachRotationLS; 
        
        public void Grab(Grabable grabable,Vector3 attachPositionWS,Quaternion attachRotationWS)
        {
            if (!grabable._CanGrab(this)) return;
            DropIfAny();

            grabable._AddGrab(this,attachPositionWS,attachRotationWS);
        }
        public void DropIfAny()
        {
            if (grabbed)
            {
                grabbed._RemoveGrab(this);
            }
        }
        
        
        private void OnDisable()
        {
            DropIfAny();
        }
        private void OnValidate()
        {
            Debug.Assert(GetComponent<Rigidbody>().isKinematic);
        }
        [HideInInspector] public Rigidbody body;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }
    }
}
