using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{

    public class BallTrajectoryPredictor
    {
        public float dt = .01f;
        public float sampleDt = .02f;
        public int maxStep = 1000;
        public Ball ball;
        public Plane ground;
        public PhysicMaterial groundMaterial, racketMaterial;
        public Collider[] obstacles = new Collider[0];
        public float[] times = new float[200];
        public Vector3[] positions = new Vector3[200];
        public Quaternion[] rotations = new Quaternion[200];
        public Vector3[] velocities = new Vector3[200];
        public Vector3[] angularVelocities = new Vector3[200];

        public int positionCount;
        public BallTrajectoryPredictor(Ball ball) 
            { this.ball = ball;}
        public void Clear() { positionCount = 0; time = 0; }

        public float time;
        public Vector3 position, velocity, angularVelocity;
        public Quaternion rotation;
        public bool isGroundHit, isObstacleHit;

        public void Set(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            this.position = position; this.rotation = rotation; this.velocity = velocity; this.angularVelocity = angularVelocity;
        }

        public void Predict(bool record = true)
        {
            float sampleCD = 0;
            isGroundHit = isObstacleHit = false;
            int remainingStep = maxStep;

            if (record && positionCount < positions.Length)
            {
                times[positionCount] = time;
                positions[positionCount] = position;
                rotations[positionCount] = rotation;
                velocities[positionCount] = velocity;
                angularVelocities[positionCount] = angularVelocity;
                ++positionCount;
            }

            while (remainingStep>0 && !isGroundHit && !isObstacleHit)
            {
                (Vector3 f, Vector3 t) = ball.GetForce(position, rotation, velocity, angularVelocity);
                Vector3 a = f / ball.body.mass;
                Vector3 b = PhysicsTools.ApplyTensor(t, ball.body.inertiaTensor, rotation * ball.body.inertiaTensorRotation, inverse: true);

                Vector3 vHalf = velocity + .5f * dt * a;
                velocity += a * dt;
                Vector3 wHalf = angularVelocity + .5f * dt * b;
                angularVelocity += b * dt;
                position += vHalf * dt;
                rotation = Quaternion.AngleAxis(wHalf.magnitude * dt * Mathf.Rad2Deg, wHalf.normalized) * rotation;
                time += dt;


                if (ground.GetDistanceToPoint(position) < ball.radius && Vector3.Dot(velocity, ground.normal) < 0)
                {
                    isGroundHit = true;
                    float overDt = (ball.radius - ground.GetDistanceToPoint(position)) / -Vector3.Dot(velocity, ground.normal);
                    overDt = Mathf.Clamp(overDt, 0, dt);
                    position -= vHalf * overDt;
                    time -= overDt;
                }
                foreach (var c in obstacles)
                    if ((Physics.ClosestPoint(position, c, c.transform.position, c.transform.rotation) - position).magnitude < ball.radius)
                    {
                        isObstacleHit = true;
                    }

                if (sampleCD <= 0)
                {
                    if (record && positionCount < positions.Length)
                    {
                        times[positionCount] = time;
                        positions[positionCount] = position;
                        rotations[positionCount] = rotation;
                        velocities[positionCount] = velocity;
                        angularVelocities[positionCount] = angularVelocity;
                        ++positionCount;
                    }
                    sampleCD += sampleDt;
                }

                sampleCD -= dt;
                remainingStep -= 1;
            }
        }
        public void CollideGround()
        {
            Ball.RacketBallCollision(position, velocity, angularVelocity,
                ground.ClosestPointOnPlane(position), Vector3.zero, Vector3.zero,
                ground.normal, ball.radius,
                groundMaterial.bounciness, groundMaterial.dynamicFriction,
                out velocity, out angularVelocity, out float impact);
        }

        //Gravity is down
        public bool DesignTrajectory(Vector3 start, Quaternion startRotation, Vector3 startAngularVelocity, Vector3 end, float gravity, float maxY, out Vector3 startVelocity, out float hitTime)//y is up
        {
            //phase 1 estimate
            startVelocity = Vector3.zero; hitTime = 0;
            if (maxY < start.y)
            {
                //Debug.Log("DesignTrajectory failed, maxH < start.y");
                return false;
            }
            float vy = Mathf.Sqrt(2 * gravity * (maxY - start.y));
            float vyFinal = -Mathf.Sqrt(vy * vy + 2 * gravity * (start.y - end.y));
            hitTime = (vy - vyFinal) / gravity;
            if (hitTime < .01f)
            {
                //Debug.Log("DesignTrajectory failed, hitTime too short");
                return false;
            }
            startVelocity = (end - start) / hitTime;
            startVelocity.y = vy;

            //phase 2 refine
            Set(start, startRotation, startVelocity, startAngularVelocity);
            Predict(record: false);
            if (isObstacleHit)
            {
                //Debug.Log("DesignTrajectory failed, obstacle hit");
                return false;
            }
            //Debug.Log($"DesignTrajectory delta: {(end - position):F5}");

            startVelocity += (end - position) / hitTime;
            Set(start, startRotation, startVelocity, startAngularVelocity);
            Predict(record: false);
            if (isObstacleHit)
            {
                //Debug.Log("DesignTrajectory failed, obstacle hit");
                return false;
            }
            //Debug.Log($"DesignTrajectory delta2: {(end - position):F5}");


            return true;
        }
        void designHit(Vector3 ballV1, Vector3 ballV2, out Vector3 velocity, out Vector3 normal)
        {
            normal = (ballV2 - ballV1).normalized;
            float v1p = Vector3.Dot(ballV1, -normal);
            float v2p = Vector3.Dot(ballV2, normal);
            velocity = (v2p - racketMaterial.bounciness * v1p) / (1 + racketMaterial.bounciness) * normal;
        }
        void designHitAdvanced(Vector3 ballV1, Vector3 ballV2, Vector3 ballW1, out Vector3 velocity, out Vector3 normal)
        {
            float[] dv = new float[3];

            Vector3 ballV1Biased = ballV1;
            designHit(ballV1Biased, ballV2, out velocity, out normal);
            for (int i = 0; i < 3; ++i)
            {
                Ball.RacketBallCollision(normal * ball.radius, ballV1, ballW1,
                    Vector3.zero, velocity, Vector3.zero,
                    normal, ball.radius, racketMaterial.bounciness, racketMaterial.dynamicFriction,
                    out Vector3 ballV2Pred, out Vector3 ballW2Pred, out float impact);
                dv[i] = (ballV2Pred - ballV2).magnitude;
                ballV1Biased += (ballV2Pred - ballV2);
                designHit(ballV1Biased, ballV2, out velocity, out normal);
            }
            //GameManager.DebugWrite($"vrel={(ballV2 - ballV1).magnitude:F2},dv={dv[0]:F2},{dv[1]:F2},{dv[2]:F2}");
        }


        public int Query(float time)
        {
            int start = 0, end = positionCount;
            while (start < end)
            {
                int mid = start + (end - start) / 2;
                if (time > times[mid])
                    start = mid + 1;
                else
                    end = mid;
            }
            return Mathf.Clamp(start - 1, 0, positionCount - 1);
        }
    }

}