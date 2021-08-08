using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using fzmnm;
using fzmnm.XRPlayer;

public class PongAI : MonoBehaviour
{
    public float fieldWidth = 1.525f;
    public float fieldDepth = 2.74f;
    public float playerWidth = 2.5f;
    public float playerHalfDepth = 2.5f;
    public float netHeight = .1525f;
    float ballRadius;
    float tableBounciness;
    float racketBounciness;
    float racketFriction;
    float gravity = 9.81f;
    Vector3 gravityVector { get { return Vector3.down * gravity; } }
    public bool autoServe = false;

    public Transform fieldRef;

    public Rigidbody ball;
    public Rigidbody table;
    public PongAIPawn racket;
    public PhysicMaterial racketMaterial;
    public PhysicMaterial tableMaterial;

    Task currentTask;



    private void Start()
    {
        racket.fieldRef = fieldRef;

        JointTools.SetIgnoreCollision(racket.body, table,true);
        StartCoroutine(AILoop());
    }
    IEnumerator AILoop()
    {
        while (true)
        {
            gravity = fieldRef.InverseTransformVector(Physics.gravity).magnitude;
            Debug.Assert(gravity > 0);
            var ballCollider = ball.GetComponent<SphereCollider>();
            ballRadius = ballCollider.radius * ball.transform.lossyScale.x;
            racketBounciness = racketMaterial.bounciness;
            racketFriction = racketMaterial.staticFriction;
            tableBounciness = tableMaterial.bounciness;

            Vector3 ballR = fieldRef.InverseTransformPoint(ball.position);
            Vector3 ballV = fieldRef.InverseTransformVector(ball.velocity);


            getGroundHit(ballR, ballV, 0, out Vector3 hitPos, out Vector3 hitV, out float hitTime);

            if (onMySide(hitPos))
            {
                yield return new WaitForSeconds(.05f);
                AIDecide();

                if (currentTask != null)
                {
                    yield return new WaitForSeconds(currentTask.refineTime);
                    AIRefine();
                    if (currentTask != null)
                        yield return new WaitForSeconds(currentTask.timeRemaining);
                    //TODO
                }
                else
                {
                    yield return new WaitForSeconds(.05f);
                }
            }
            if (autoServe)
            {

                if (fieldRef.InverseTransformPoint(ball.position).y < 0)
                {
                    racket.currentTask = null;
                    for (float t = 0; t < .5f; t += .05f)
                    {
                        if (fieldRef.InverseTransformPoint(ball.position).y > 0)
                            break;
                        yield return new WaitForSeconds(.05f);
                    }
                    if (fieldRef.InverseTransformPoint(ball.position).y < 0)
                    {
                        ball.position = fieldRef.TransformPoint(fieldWidth * .25f, .5f, -fieldDepth * .4f);
                        ball.velocity = fieldRef.TransformVector(Vector3.up * 5f);
                        racket.transform.position = ball.position - fieldRef.up * .25f;
                        //racket.transform.rotation = fieldRef.rotation;
                        AIDecide(true);
                        if (currentTask != null)
                            yield return new WaitForSeconds(currentTask.timeRemaining);
                    }

                }
            }
            yield return new WaitForSeconds(.05f);
        }
    }
    void AIDecide(bool withoutBounce = false)
    {
        Vector3 ballR = fieldRef.InverseTransformPoint(ball.position);
        Vector3 ballV = fieldRef.InverseTransformVector(ball.velocity);
        getTrajectory(ballR, ballV, float.PositiveInfinity, ref trajectoryPoints, 0, out nTrajectoryPoints);

        Vector3 bounceR, bounceV; float hitTime;
        if (withoutBounce)
        {
            bounceR = ballR; bounceV = ballV; hitTime = 0;
        }
        else
        {
            if (!getGroundHit(ballR, ballV, 0, out bounceR, out bounceV, out hitTime)) { setTrajectory(Color.white); return; }
            setHitSign(hitSign, bounceR, willHitNet(ballR, ballV, hitTime, netHeight));
            if (!onMySide(bounceR)) { setTrajectory(Color.white); return; }
        }

        Task newTask = null;
        for (int i = 0; i < 10; ++i)
        {
            if (!tryGenerateNewTask(bounceR, bounceV, hitTime, out newTask))
                newTask = null;
            if (!EvaluateTask(newTask))
                newTask = null;
            if (newTask != null)
                break;
        }

        if (newTask != null)
        {
            Debug.Log("New Task Generated");
            currentTask = racket.currentTask = newTask;
            setHitSign(hitBackSign, newTask.desiredBallTarget, willHitNet(newTask.predictedBallPosition, newTask.desiredBallVelocity, newTask.desiredFlyBackTime, netHeight));
            getTrajectory(bounceR, bounceV, newTask.timeRemaining - hitTime, ref trajectoryPoints, nTrajectoryPoints, out nTrajectoryPoints);
            getTrajectory(newTask.predictedBallPosition, newTask.desiredBallVelocity, float.PositiveInfinity, ref trajectoryPoints, nTrajectoryPoints, out nTrajectoryPoints);
            setTrajectory(Color.white);
        }
        else
        {
            getTrajectory(bounceR, bounceV, float.PositiveInfinity, ref trajectoryPoints, nTrajectoryPoints, out nTrajectoryPoints);
            setTrajectory(Color.red);
        }
    }
    void AIRefine()
    {
        Debug.Log("Try Refine");
        if (currentTask == null) return;
        Vector3 ballR = fieldRef.InverseTransformPoint(ball.position);
        Vector3 ballV = fieldRef.InverseTransformVector(ball.velocity);
        Vector3 ballW = fieldRef.InverseTransformVector(ball.angularVelocity);


        if (!refineTask(ballR, ballV, ballW, 0, ref currentTask)) return;
        if (!EvaluateTask(currentTask))
        {
            currentTask = racket.currentTask = null;
            return;
        }

        getFly(ballR, ballV, currentTask.timeRemaining, out Vector3 ballR1, out Vector3 ballV1);
        /*
        GameManager.DebugWrite("Desired Collision");
        GameManager.DebugWrite($"Ball {ballV1},{ballW}");
        GameManager.DebugWrite($"Racket {currentTask.velocity},{Vector3.zero}");
        GameManager.DebugWrite($"Normal {currentTask.normal}");
        GameManager.DebugWrite($"BallPosition {ballR}");*/

        getTrajectory(ballR, ballV, currentTask.timeRemaining, ref trajectoryPoints, 0, out nTrajectoryPoints);
        getTrajectory(currentTask.predictedBallPosition, currentTask.desiredBallVelocity, float.PositiveInfinity, ref trajectoryPoints, nTrajectoryPoints, out nTrajectoryPoints);
        setTrajectory(Color.white);
    }

    public class Task
    {
        public float timeRemaining;
        public float readyTime;
        public float refineTime;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 normal;
        public Vector3 predictedBallPosition;
        public Vector3 desiredBallVelocity;
        public Vector3 desiredBallTarget;
        public float desiredFlyBackTime;
        public float desiredMaxHeight;
    }
    bool EvaluateTask(Task task)
    {
        if (!racket.EvaluateTask(task))
            return false;
        if (task.position.z > -.15f || task.position.z < -playerHalfDepth || Mathf.Abs(task.position.x) > playerWidth / 2)
            return false;
        if (task.position.y < .05f || task.position.y > 2.2f)
            return false;
        return true;
    }

    bool tryGenerateNewTask(Vector3 ballR, Vector3 ballV, float timeBeforeBounce, out Task task)
    {
        getGroundHit(ballR, ballV, 0, out Vector3 hitPos, out Vector3 vf, out float hitTime);

        task = new Task();
        task.desiredBallTarget = RandomPlanar(fieldWidth * -.45f, fieldWidth * .45f, fieldDepth * .1f, fieldDepth * .45f);
        task.desiredMaxHeight = Random.Range(netHeight, 1.2f);

        task.readyTime = .1f;
        task.refineTime = timeBeforeBounce + .1f;

        float minTimeAfterBounce = task.readyTime + task.refineTime - timeBeforeBounce;
        float maxTimeAfterBounce = hitTime;
        if (ballV.z < 0)
            maxTimeAfterBounce = Mathf.Min(maxTimeAfterBounce, (playerHalfDepth + ballR.z) / (-ballV.z));
        if (minTimeAfterBounce > maxTimeAfterBounce) return false;
        float timeAfterBounce = Random.Range(minTimeAfterBounce, maxTimeAfterBounce);//Should be larger than readyTime+refineTimeDelta
        task.timeRemaining = timeBeforeBounce + timeAfterBounce;
        if (task.readyTime + task.refineTime > task.timeRemaining) return false;

        if (!refineTask(ballR, ballV, Vector3.zero, timeBeforeBounce, ref task))
            return false;

        if (willHitNet(task.predictedBallPosition, Quaternion.Euler(5f, 0, 0) * task.desiredBallVelocity, task.desiredFlyBackTime, netHeight))
            return false;


        return true;
    }

    bool refineTask(Vector3 ballR, Vector3 ballV, Vector3 ballW, float timeBeforeBounce, ref Task task)
    {
        if (task.timeRemaining < task.readyTime)
        {
            Debug.Log("Refine Failed, Not Enough Time");
            return false;
        }

        float timeToFly = task.timeRemaining - timeBeforeBounce;
        getFly(ballR, ballV, timeToFly, out Vector3 ballR1, out Vector3 ballV1);
        task.predictedBallPosition = ballR1;
        task.position = ballR1 + ballV1.normalized * ballRadius;

        if (!designTrajectory(ballR1, task.desiredBallTarget, task.desiredMaxHeight, out task.desiredBallVelocity, out task.desiredFlyBackTime))
            return false;
        designHitAdvanced(ballV1, task.desiredBallVelocity, ballW, out task.velocity, out task.normal);
        return true;
    }


    #region Maths


    bool getHitTime(float y, float vy, out float t)
    {
        if (vy * vy + 2 * gravity * y < 0) { t = 0; return false; }
        t = (vy + Mathf.Sqrt(vy * vy + 2 * gravity * y)) / gravity;
        return true;
    }
    bool getGroundHit(Vector3 r, Vector3 v, float groundLevel, out Vector3 pos, out Vector3 vf, out float time)
    {
        if (!getHitTime(r.y - ballRadius - groundLevel, v.y, out float t))
        {
            pos = Vector3.zero;
            time = 0;
            vf = Vector3.zero;
            return false;
        }
        Vector3 vh = Vector3.ProjectOnPlane(v, Vector3.up);

        pos = Vector3.ProjectOnPlane(r, Vector3.up) + Vector3.up * (groundLevel + ballRadius) + vh * t;
        time = t;
        vf = v - Vector3.up * gravity * t;
        vf.y *= -tableBounciness;

        return true;
    }
    bool willHitNet(Vector3 r, Vector3 v, float time, float netHeight)
    {
        if (r.z > 0) { r.z = -r.z; v.z = -v.z; }
        if (v.z <= 0) return false;
        float t = (-r.z) / v.z;
        if (t > time) return false;
        float height = r.y + v.y * t - .5f * gravity * t * t;
        if (height - ballRadius < netHeight) return true; else return false;
    }
    bool onMySide(Vector3 pos)
    {
        if (Mathf.Abs(pos.x) >= fieldWidth / 2 + ballRadius) return false;
        if (pos.z > 0) return false;
        if (pos.z < -fieldDepth / 2 - ballRadius) return false;
        return true;
    }
    bool onOtherSide(Vector3 pos)
    {
        if (Mathf.Abs(pos.x) >= fieldWidth / 2 + ballRadius) return false;
        if (pos.z < 0) return false;
        if (pos.z > fieldDepth / 2 + ballRadius) return false;
        return true;
    }
    void getFly(Vector3 r, Vector3 v, float t, out Vector3 rf, out Vector3 vf)
    {
        rf = r + v * t + .5f * t * t * gravityVector;
        vf = v + gravityVector * t;
    }

    /*void designTrajectory(Vector3 r1, Vector3 r2, float t, out Vector3 v1)
    {
        v1= (r2 - r1 - .5f * gravityVector * t * t) / t;
    }*/
    bool designTrajectory(Vector3 r1, Vector3 r2, float maxH, out Vector3 v1, out float t)
    {
        if (2 * gravity * (maxH - r1.y) < 0) { v1 = Vector3.zero; t = 0; return false; }
        float vy = Mathf.Sqrt(2 * gravity * (maxH - r1.y));
        if (!getHitTime(r1.y - r2.y, vy, out t)) { v1 = Vector3.zero; t = 0; return false; }
        v1 = (r2 - r1 - .5f * gravityVector * t * t) / t;
        return true;
    }
    Vector3 RandomPlanar(float x1, float x2, float z1, float z2)
    {
        return new Vector3(Random.Range(x1, x2), 0, Random.Range(z1, z2));
    }
    void designHit(Vector3 ballV1, Vector3 ballV2, out Vector3 velocity, out Vector3 normal)
    {
        normal = (ballV2 - ballV1).normalized;
        float v1p = Vector3.Dot(ballV1, -normal);
        float v2p = Vector3.Dot(ballV2, normal);
        velocity = (v2p - racketBounciness * v1p) / (1 + racketBounciness) * normal;
    }
    void designHitAdvanced(Vector3 ballV1, Vector3 ballV2, Vector3 ballW1, out Vector3 velocity, out Vector3 normal)
    {
        float[] dv = new float[3];

        Vector3 ballV1Biased = ballV1;
        designHit(ballV1Biased, ballV2, out velocity, out normal);
        for (int i = 0; i < 3; ++i)
        {
            Ball.RacketBallCollision(normal * ballRadius, ballV1, ballW1,
                Vector3.zero, velocity, Vector3.zero,
                normal, ballRadius, racketBounciness, racketFriction,
                out Vector3 ballV2Pred, out Vector3 ballW2Pred, out float impact);
            dv[i] = (ballV2Pred - ballV2).magnitude;
            ballV1Biased += (ballV2Pred - ballV2);
            designHit(ballV1Biased, ballV2, out velocity, out normal);
        }
        //GameManager.DebugWrite($"vrel={(ballV2 - ballV1).magnitude:F2},dv={dv[0]:F2},{dv[1]:F2},{dv[2]:F2}");
    }
    #endregion

    #region Debug
    public LineRenderer trajectoryRenderer; Vector3[] trajectoryPoints = new Vector3[200]; int nTrajectoryPoints;
    public GameObject hitSign;
    public GameObject hitBackSign;
    public bool showDebug = true;
    void getTrajectory(Vector3 r, Vector3 v, float maxTime, ref Vector3[] points, int startPointID, out int nPoints)
    {
        float dt = .01f;
        float sampleDt = .01f;
        int i = startPointID;
        float tcd = 0;
        while (i < points.Length && maxTime > 0)
        {
            if (tcd <= 0)
            {
                points[i++] = r;
                tcd += sampleDt;
            }
            r = r + v * dt + .5f * gravityVector * dt * dt;
            v = v + gravityVector * dt;
            tcd -= dt;
            maxTime -= dt;
            if (r.y - ballRadius < 0 && v.y < 0) break;
        }
        nPoints = i;
    }
    void setHitSign(GameObject sign, Vector3 hitPos, bool willHitNet)
    {
        sign.SetActive(showDebug);
        sign.transform.position = fieldRef.TransformPoint(hitPos);
        var mat = sign.GetComponent<MeshRenderer>().material;
        if (willHitNet)
            mat.color = Color.grey;
        else if (onOtherSide(hitPos))
            mat.color = Color.green;
        else if (onMySide(hitPos))
            mat.color = Color.red;
        else
            mat.color = Color.grey;

    }
    void setTrajectory(Color color)
    {
        if (trajectoryRenderer)
        {
            for (int i = 0; i < nTrajectoryPoints; ++i)
                trajectoryPoints[i] = fieldRef.TransformPoint(trajectoryPoints[i]);
            trajectoryRenderer.positionCount = nTrajectoryPoints;
            trajectoryRenderer.SetPositions(trajectoryPoints);
            trajectoryRenderer.positionCount = nTrajectoryPoints;
            trajectoryRenderer.useWorldSpace = true;
            trajectoryRenderer.material.color = color;
            trajectoryRenderer.gameObject.SetActive(showDebug);
        }
    }
    void resetAllSigns()
    {
        hitSign.SetActive(false);
        hitBackSign.SetActive(false);
        trajectoryRenderer.gameObject.SetActive(false);
    }
    #endregion
}
