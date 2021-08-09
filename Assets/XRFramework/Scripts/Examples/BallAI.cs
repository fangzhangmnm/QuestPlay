using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
namespace fzmnm
{

    public class BallAI : MonoBehaviour
    {
        public BoxCollider fieldBounds;
        public Transform field => fieldBounds.transform;
        public BoxCollider targetBounds;
        public PhysicMaterial groundMaterial, racketMaterial;
        public Collider[] obstacles;
        public Plane ground;
        public Ball ball;
        public LineRenderer trajectoryRenderer;
        public BallTrajectoryPredictor predictor;
        public GameObject targetIndicator;
        private void Start()
        {
            ground = new Plane(fieldBounds.transform.up, fieldBounds.transform.TransformPoint(Vector3.up * (fieldBounds.center.y - fieldBounds.size.y / 2)));
            predictor = new BallTrajectoryPredictor(ball);
            predictor.ground = ground;
            predictor.groundMaterial = groundMaterial;
            predictor.racketMaterial = racketMaterial;
            predictor.obstacles = obstacles;
            StartCoroutine(MainLoop());
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }
        IEnumerator MainLoop()
        {
            while (true)
            {
                Observe();
                Decide();
                yield return StartCoroutine(Execute());
            }
        }
        bool hasDecision;
        Vector3 startPosition, startVelocity, startAngularVelocity;
        IEnumerator Execute()
        {
            targetIndicator.transform.position = startPosition;
            if(startVelocity.magnitude>.001f)
                targetIndicator.transform.rotation = Quaternion.LookRotation(startVelocity, Vector3.up);

            if (!hasDecision) yield break;
            yield return new WaitForSeconds(hitTime);

            ball.body.velocity = startVelocity;
            ball.body.angularVelocity = startAngularVelocity;
        }

        void Decide()
        {
            hasDecision = false;
            if (!needAction) return;
            bool success = false;
            for (int _i = 0; _i < 5; ++_i)
            {

                Vector3 end = new Vector3(
                    Random.Range(-targetBounds.size.x / 2 + targetBounds.center.x, targetBounds.size.x / 2 + targetBounds.center.x),
                    0,
                    Random.Range(-targetBounds.size.z / 2 + targetBounds.center.z, targetBounds.size.z / 2 + targetBounds.center.z)
                    );
                end = fieldBounds.transform.TransformPoint(end);

                float maxY = targetBounds.transform.TransformPoint(new Vector3(
                    0,
                    Random.Range(0, targetBounds.size.y / 2 + targetBounds.center.y),
                    0)).y;

                startPosition = hitPosition;
                Vector3 startAngularVelocity = Random.insideUnitSphere * 50;


                if(predictor.DesignTrajectory(hitPosition, hitRotation, startAngularVelocity, end, ball.GetGravity().magnitude, maxY, out startVelocity, out float hitTime))
                {
                    success = true;
                }
                predictor.Set(startPosition, hitRotation, startVelocity, startAngularVelocity);
                predictor.Predict();
                UpdateTrajectoryRenderer();


                if (success) break;
            }
            if (success)
                hasDecision = true;
        }
        bool needAction;
        Vector3 hitPosition, hitVelocity, hitAngularVelocity;
        float hitTime;
        Quaternion hitRotation;
        void Observe()
        {
            needAction = false;
            if (field.InverseTransformPoint(ball.body.position).z > 0)
            {

                predictor.Clear();
                predictor.Set(ball.body.position, ball.body.rotation, ball.body.velocity, ball.body.angularVelocity);
                predictor.Predict(record: true);
                if (predictor.isGroundHit && !predictor.isObstacleHit)
                    if (field.InverseTransformPoint(predictor.position).z < 0)
                    {
                        float time1 = predictor.time;
                        predictor.CollideGround();
                        predictor.Predict(record: true);
                        float time2 = predictor.time;
                        Debug.Log($"{time1:F2} {time2 - time1:F2}");//.5f

                        hitTime = (time1 + time2) / 2;
                        int id = predictor.Query((hitTime));
                        hitPosition = predictor.positions[id];
                        hitRotation = predictor.rotations[id];
                        hitVelocity = predictor.velocities[id];
                        hitAngularVelocity = predictor.angularVelocities[id];
                        needAction = true;
                        predictor.positionCount = id+1;

                        UpdateTrajectoryRenderer();
                    }
            }

        }
        void UpdateTrajectoryRenderer()
        {

            if (trajectoryRenderer)
            {
                trajectoryRenderer.useWorldSpace = true;
                trajectoryRenderer.positionCount = predictor.positionCount;
                trajectoryRenderer.SetPositions(predictor.positions);
            }
        }
        
    }

}