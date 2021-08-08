using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallTrajectoryPredictor
{
    public float dt = .01f;
    public float sampleDt = .02f;
    public int maxStep = 1000;
    public Ball ball;
    public Plane ground;
    public Vector3[] points = new Vector3[200];
    public int positionCount;
    public BallTrajectoryPredictor(Ball ball, Plane ground) { this.ball = ball;this.ground = ground; }
    public void Clear() { positionCount = 0; }

    public Vector3 position, velocity, angularVelocity;
    public Quaternion rotation;
    public bool isGroundHit;

    public void Set(Vector3 position, Quaternion rotation, Vector3 velocity,Vector3 angularVelocity)
    {
        this.position = position;this.rotation = rotation;this.velocity = velocity;this.angularVelocity = angularVelocity;
    }

    public void Predict(float time=4f, bool record=true)
    {
        float sampleCD = 0;
        isGroundHit = false;
        float remainingTime = Mathf.Min(time,maxStep*dt);

        if(record && positionCount < points.Length)points[positionCount++] = position;

        while (remainingTime > 0)
        {
            (Vector3 f, Vector3 t) = ball.GetForce(position, rotation, velocity, angularVelocity);
            Vector3 a = f / ball.body.mass;
            Vector3 b = Ball.ApplyTensor(t, ball.body.inertiaTensor, rotation * ball.body.inertiaTensorRotation, inverse: true);

            Vector3 vHalf = velocity + .5f * dt * a;
            velocity += a * dt;
            Vector3 wHalf = angularVelocity + .5f * dt * b;
            angularVelocity += b * dt;
            position += vHalf * dt;
            rotation = Quaternion.AngleAxis(wHalf.magnitude * dt * Mathf.Rad2Deg, wHalf.normalized) * rotation;

            if (sampleCD <= 0)
            {
                if(record && positionCount < points.Length)points[positionCount++] = position;
                sampleCD += sampleDt;
            }

            sampleCD -= dt;
            remainingTime -= dt;


            if (ground.GetDistanceToPoint(position) < ball.radius && Vector3.Dot(velocity,ground.normal)<0) {
                /*
                float overDt = (ball.radius - ground.GetDistanceToPoint(position)) / Vector3.Dot(ground.normal, -velocity);
                overDt = Mathf.Clamp(overDt, 0, dt);
                remainingTime += overDt;
                position -= vHalf * overDt;
                rotation = Quaternion.AngleAxis(wHalf.magnitude * -overDt * Mathf.Rad2Deg, wHalf.normalized) * rotation;
                velocity -= a * overDt;
                angularVelocity -= b * overDt;
                */

                isGroundHit = true; break; 
            }
        }
    }
    //Gravity is down
    public bool Design(Vector3 start, Quaternion startRotation, Vector3 startAngularVelocity, Vector3 end, float gravity, float maxY, out Vector3 startVelocity, out float hitTime)//y is up
    {
        //phase 1 estimate
        startVelocity = Vector3.zero;hitTime = 0;
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
        Predict(hitTime, record: false);
        //Debug.Log($"DesignTrajectory delta: {(end - position):F5}");

        startVelocity += (end - position) / hitTime;
        Set(start, startRotation, startVelocity, startAngularVelocity);
        Predict(hitTime, record: false);
        //Debug.Log($"DesignTrajectory delta2: {(end - position):F5}");


        return true;
    }
}
