using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{

    [RequireComponent(typeof(Animator))]
    public class BallAIPawn : MonoBehaviour
    {
        public class HitDesc
        {
            public HitAnimationTag hitAnimation;
            public float prepareTime = .2f;
            public float foreSwing = .1f;
            public float backSwing = .7f;
        }
        public HitDesc[] hits;


        Vector3 TryGetRootPositionToHit(int hitID,Vector3 hitPosition, Quaternion rootRotation )
        {
            var hit = hits[hitID];
            Vector3 delta = hit.hitAnimation.transform.position - transform.position;
            delta = rootRotation * Quaternion.Inverse(transform.rotation) * delta;
            return hitPosition - delta;
        }
        bool CanHit(int hitID, Vector3 hitPosition, Vector3 hitDirection, Vector3 rootPosition, Quaternion rootRotation)
        {
            var hit = hits[hitID];
            hitDirection = transform.rotation * Quaternion.Inverse(rootRotation) * hitDirection;
            hitPosition = transform.rotation * Quaternion.Inverse(rootRotation) * (hitPosition - rootPosition) + transform.position;
            throw new System.NotImplementedException();
        }

        StateMachine stateMachine;

    }
}

