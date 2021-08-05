using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{
    public class SmoothVelocity
    {
        Rigidbody body;
        public Vector3 smoothedCOMVelocity;
        public Vector3 smoothedAngularVelocity;
        public float smoothTime = .1f;
        public float pauseTime = 0;
        public SmoothVelocity(Rigidbody body) { this.body = body; }
        public void Pause(float pauseTime) { this.pauseTime = pauseTime; }
        public void Update(float dt)
        {
            pauseTime -= dt;
            if (pauseTime <= 0)
            {
                smoothedCOMVelocity = Vector3.Lerp(smoothedCOMVelocity, body.velocity, dt / smoothTime);
                smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, body.angularVelocity, dt / smoothTime);
            }
        }
        public Vector3 getPointVelocity(Vector3 worldPoint)
        {
            Vector3 comPosition = body.position + body.rotation * body.centerOfMass;
            return smoothedCOMVelocity + Vector3.Cross(smoothedAngularVelocity, worldPoint - comPosition);
        }
    }

}