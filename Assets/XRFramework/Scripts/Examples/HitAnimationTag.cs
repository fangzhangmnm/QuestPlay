using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{
    public class HitAnimationTag : MonoBehaviour
    {
        public int hitID;
        public Transform effector;
        public float maxLookatBiasAngle = 30f;
        public BoxCollider allowedArea;
        public float prepareNormalizedTime = 0f;
        public float hitNormalizedTime = 0f;
        private void OnDrawGizmos()
        {
            Debug.DrawRay(transform.position, transform.rotation * Quaternion.Euler(-maxLookatBiasAngle, 0, 0) * Vector3.forward * .5f, Color.white);
            Debug.DrawRay(transform.position, transform.rotation * Quaternion.Euler(maxLookatBiasAngle, 0, 0) * Vector3.forward * .5f, Color.white);
            Debug.DrawRay(transform.position, transform.rotation * Quaternion.Euler(0, -maxLookatBiasAngle, 0) * Vector3.forward * .5f, Color.white);
            Debug.DrawRay(transform.position, transform.rotation * Quaternion.Euler(0, maxLookatBiasAngle, 0) * Vector3.forward * .5f, Color.white);
            PhysicsDebugger.DebugDrawCircle(transform.position + transform.forward * .5f * Mathf.Cos(Mathf.Deg2Rad * maxLookatBiasAngle), transform.rotation, .5f * Mathf.Sin(Mathf.Deg2Rad * maxLookatBiasAngle), Color.white);
        }
        private void OnValidate()
        {
            Debug.Assert(allowedArea.enabled == false && allowedArea.isTrigger);
        }

    }
}

