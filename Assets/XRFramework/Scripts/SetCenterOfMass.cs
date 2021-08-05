using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{
    [RequireComponent(typeof(Rigidbody))]
    public class SetCenterOfMass : MonoBehaviour
    {
        public Transform centerOfMassPosition;
        public bool scaleInertiaTensor=false;
        public float inertiaTensorMultiplier = 1f;
        public bool regulateInertiaTensor = false;
        public float minRegulatedInertiaTensorComponent = .1f;
        private void Awake()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (centerOfMassPosition == null) centerOfMassPosition = transform;
            body.ResetCenterOfMass();
            body.centerOfMass = Quaternion.Inverse(transform.rotation)*(centerOfMassPosition.position-transform.position);//not affected by scale!
            body.ResetInertiaTensor();
            var v = body.inertiaTensor;
            if (scaleInertiaTensor)
            {
                v *= inertiaTensorMultiplier;
            }
            if (regulateInertiaTensor)
            {
                float min = Mathf.Max(v.x, v.y, v.z)*minRegulatedInertiaTensorComponent;
                v.x = Mathf.Max(v.x, min);
                v.y = Mathf.Max(v.y, min);
                v.z = Mathf.Max(v.z, min);
            }
            body.inertiaTensor = v;
            Destroy(this);
        }
    }

}