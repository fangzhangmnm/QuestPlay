using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        public float defaultBounciness = 0.5f, defaultFriction = 0.5f;
        public float airDensity = 1.225f;
        public float dragCoeff = .1f;
        public float angularDragCoeff = .05f;
        public float magnusCoeff = .5f;
        [Tooltip("50rad/s 10m/s")]
        public float debug_magnusAcceleration;
        public float minImpact = .3f;

        public bool hasTailWings = false;
        public float CD = .15f, CLa = 1.4f;
        public float frontAreaLocal = .01f * .01f;
        public float wingAreaLocal = .02f * .05f;
        float frontArea => frontAreaLocal * transform.lossyScale.x * transform.lossyScale.x;
        float wingArea => wingAreaLocal * transform.lossyScale.x * transform.lossyScale.x;

        [HideInInspector] public Vector3 storedVelocity, storedAngularVelocity, storedNormal, storedPosition;
        bool _inCol; int _inColTimer;
        [HideInInspector] public Rigidbody body;
        SphereCollider sphereCollider;
        public float radius => transform.lossyScale.x * sphereCollider.radius;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.contactOffset = 0.005f;
        }
        int ffNum = 0;
        private void FixedUpdate()
        {
            ++ffNum;
            if (!_inCol)
            {
                storedVelocity = body.velocity;
                storedAngularVelocity = body.angularVelocity;
            }
            _inCol = false;
            if (!body.isKinematic && !body.IsSleeping())
            {
                (Vector3 force, Vector3 torque) = GetForce(body.position, body.rotation, body.velocity, body.angularVelocity);
                body.AddForce(force);
                body.AddTorque(torque);
                //PhysicsDebugger.ShowVectorInGame(body.position, body.angularVelocity/10f, Time.fixedDeltaTime);
            }
        }

        public Vector3 GetGravity() => GetGravity(body.position);
        public Vector3 GetGravity(Vector3 position) => Physics.gravity;
        public (Vector3, Vector3) GetForce(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {

            Vector3 totalNondragForce = Vector3.zero;
            Vector3 totalDragForce = Vector3.zero;
            Vector3 totalNondragTorque = Vector3.zero;
            Vector3 totalDragTorque = Vector3.zero;

            Vector3 v = velocity, w = angularVelocity;
            float r = radius;

            Vector3 buoyancy = -airDensity * Mathf.PI * 4f / 3f * r * r * r * Physics.gravity;
            Vector3 drag = -dragCoeff * Mathf.PI * r * r * .5f * airDensity * v.magnitude * v;
            //Magnus Effect http://math.mit.edu/~bush/wordpress/wp-content/uploads/2013/11/Beautiful-Game-2013.pdf
            Vector3 magnus = magnusCoeff * Mathf.PI * airDensity * r * r * r * Vector3.Cross(w, v);
            Vector3 angularDrag = -angularDragCoeff * PhysicsTools.ApplyTensor(w, body.inertiaTensor, rotation * body.inertiaTensorRotation);
            Vector3 gravityForce = body.mass * GetGravity(position);

            totalNondragForce += buoyancy + magnus + gravityForce;
            totalDragForce += drag;
            totalDragTorque += angularDrag;

            if (hasTailWings)
            {
                Vector3 tailWingsDrag = -.5f * CD * airDensity * frontArea * v.magnitude * v;
                tailWingsDrag = Vector3.ClampMagnitude(tailWingsDrag, v.magnitude * body.mass * Time.fixedDeltaTime);//Prevent Jittering
                Vector3 tailWingsLiftTorque = .5f * airDensity * wingArea * v.sqrMagnitude * CLa * Vector3.Cross(rotation * Vector3.forward, v);

                totalDragForce += tailWingsDrag;
                totalDragTorque += tailWingsLiftTorque;
            }

            //Prevent Jittering
            totalDragForce = Vector3.ClampMagnitude(totalDragForce, v.magnitude * body.mass / Time.fixedDeltaTime);
            totalDragTorque = Vector3.ClampMagnitude(totalDragTorque, PhysicsTools.ApplyTensor(w, body.inertiaTensor, rotation * body.inertiaTensorRotation).magnitude / Time.fixedDeltaTime);//Prevent Jittering

            return (totalNondragForce + totalDragForce, totalNondragTorque + totalDragTorque);
        }



        private void OnCollisionEnter(Collision collision)
        {
            _inCol = true; _inColTimer = 0;
            storedNormal = collision.GetContact(0).normal;
            storedPosition = collision.GetContact(0).point + storedNormal * radius;
        }
        private void OnCollisionStay(Collision collision)
        {
            _inCol = true; _inColTimer += 1;
            storedNormal = collision.GetContact(0).normal;
            storedPosition = collision.GetContact(0).point + storedNormal * radius;
            DealCollision(collision);
        }
        private void OnCollisionExit(Collision collision)
        {
            if((body.position-storedPosition).magnitude<3*radius)
                DealCollision(collision);
        }
        void DealCollision(Collision collision)
        {

            Vector3 rsv = Vector3.zero, rsw = Vector3.zero, rp = Vector3.zero;
            float bounciness = defaultBounciness;
            float friction = defaultFriction;

            var racket = collision.rigidbody ? collision.rigidbody.GetComponent<Racket>() : collision.collider.GetComponent<Racket>();
            if (collision.collider.sharedMaterial)
            {
                bounciness = collision.collider.sharedMaterial.bounciness;
                friction = collision.collider.sharedMaterial.dynamicFriction;
            }
            if (racket)
            {
                rsv = racket.smoothedVelocity;
                rsw = racket.smoothedAngularVelocity;
                rp = racket.smoothedPosition;
            }
            else if (collision.rigidbody)
            {
                rsv = body.velocity;
                rsw = body.angularVelocity;
                rp = body.position;
            }
            RacketBallCollision(storedPosition, storedVelocity, storedAngularVelocity,
                rp, rsv, rsw,
                storedNormal, radius, bounciness, friction,
                out Vector3 newVelocity, out Vector3 newAngularVelocity, out float newImpact);
            if (newImpact > minImpact)
            {
                //ShowVector(storedPosition, storedNormal * newImpact / 10);

                body.velocity = newVelocity;
                body.angularVelocity = newAngularVelocity;
                /*if(Physics.ComputePenetration(sphereCollider, body.position, body.rotation,
                    collision.collider, collision.collider.transform.position, collision.collider.transform.rotation,
                    out Vector3 dir, out float dist))
                {
                    transform.position += dir * dist;
                }*/
            }
        }
        public static void RacketBallCollision(Vector3 rBall, Vector3 vBall, Vector3 wBall, Vector3 rRacket, Vector3 vRacket, Vector3 wRacket, Vector3 normal, float radius, float bounciness, float friction, out Vector3 vf, out Vector3 wf, out float impact)
        {
            normal = normal.normalized;
            float momentOfInertia = radius * radius * 2 / 3;//moment of inertia with unit mass

            //Transform ball movement into racket frame to set velocity of collision point as zero
            //TODO physics check
            Vector3 rContact = rBall - normal * radius;
            Vector3 vContact = vRacket + Vector3.Cross(wRacket, rContact - rRacket);
            Vector3 v = vBall - vContact;
            Vector3 w = wBall - wRacket;

            //Resolve the impact
            impact = -Vector3.Dot(v, normal);

            if (impact < 0) { vf = vBall; wf = wBall; impact = 0; return; }

            float impulse = impact * (1 + bounciness);
            v += impulse * normal;

            int N = 10;
            Vector3 R = -normal * radius;
            for (int i = 0; i < N; ++i)
            {
                Vector3 slidingVelocity = Vector3.ProjectOnPlane(v, normal) + Vector3.Cross(w, R);

                Vector3 frictionDir = -slidingVelocity.normalized;

                Vector3 dvh = frictionDir + Vector3.Cross(Vector3.Cross(R, frictionDir) / momentOfInertia, R);
                if (dvh.sqrMagnitude > 0)
                {
                    float maxI = -Vector3.Dot(slidingVelocity, dvh) / dvh.sqrMagnitude;
                    Debug.Assert(maxI >= 0);

                    Vector3 frictionImpulse = Mathf.Min(impulse / N * friction, maxI) * frictionDir;

                    v += frictionImpulse;
                    w += Vector3.Cross(R, frictionImpulse) / momentOfInertia;
                }
            }

            //Convert back to world frame and write to rigidbody
            vf = v + vContact;
            wf = w + wRacket;

        }
        private void OnValidate()
        {
            body = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            if (body.drag > .0001f)
                Debug.LogError(gameObject.name + " Rigidbody's builtin linear drag should be disabled");
            if (body.angularDrag > .0001f)
                Debug.LogError(gameObject.name + " Rigidbody's builtin angular drag should be disabled");
            if (body.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
                Debug.LogError(gameObject.name + " Should use ContinuousDynamic CollisionDetectionMode");
            if (Physics.bounceThreshold > .1f)
                Debug.LogWarning("Set Physics.bounceThreshold <=.1f");
            if (Physics.defaultMaxAngularSpeed < 50f)
                Debug.LogWarning("Set Physics.defaultMaxAngularSpeed >=50f");
            float r = radius;
            //Magnus Effect http://math.mit.edu/~bush/wordpress/wp-content/uploads/2013/11/Beautiful-Game-2013.pdf
            debug_magnusAcceleration = magnusCoeff * Mathf.PI * airDensity * r * r * r * 50*10/body.mass;
        }
    }

}