using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using fzmnm.BehaviorTree;
namespace fzmnm
{

    public class BallAI : MonoBehaviour
    {
        public Animator anim;
        public new Transform transform;
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
        Node tree;
        [TextArea(5, 15)]
        public string debug_tree;

        private void Start()
        {
            ground = new Plane(fieldBounds.transform.up, fieldBounds.transform.TransformPoint(Vector3.up * (fieldBounds.center.y - fieldBounds.size.y / 2)));
            predictor = new BallTrajectoryPredictor(ball);
            predictor.ground = ground;
            predictor.groundMaterial = groundMaterial;
            predictor.racketMaterial = racketMaterial;
            predictor.obstacles = obstacles;

            tree = new Repeater(repeatOnFail: true).Add(
                    new Sequencer().Add(
                        new WaitForSecondsNode(.1f),
                        new ActionNode(IsNeedReturnAndChooseHitPosition),
                        new ActionNode(ChooseReturnPosition),
                        new WaitForSecondsNode(()=>timeToWait)
                    )
                );
        }
        private void FixedUpdate()
        {
            tree.Tick();
            debug_tree = tree.Log();
        }


        float timeToWait;
        Vector3 hitPosition, hitVelocity, hitAngularVelocity;
        Quaternion hitRotation;
        Vector3 returnPosition, returnVelocity, returnAngularVelocity;
        Quaternion returnRotation;
        bool IsNeedReturnAndChooseHitPosition()
        {
            if (field.InverseTransformPoint(ball.body.position).z < 0) return false;

            predictor.Clear();
            predictor.Set(ball.body.position, ball.body.rotation, ball.body.velocity, ball.body.angularVelocity);
            predictor.Predict(record: true);


            if (!predictor.isGroundHit || predictor.isObstacleHit) return false;
            if (field.InverseTransformPoint(predictor.position).z > 0) return false;

            float time1 = predictor.time;
            predictor.CollideGround();
            predictor.Predict(record: true);
            float time2 = predictor.time;
            //Debug.Log($"{time1:F2} {time2 - time1:F2}");//.5f

            timeToWait = (time1 + time2) / 2;
            int id = predictor.Query((timeToWait));
            hitPosition = predictor.positions[id];
            hitRotation = predictor.rotations[id];
            hitVelocity = predictor.velocities[id];
            hitAngularVelocity = predictor.angularVelocities[id];
            predictor.positionCount = id + 1;

            UpdateTrajectoryRenderer();

            return true;
        }
        bool ChooseReturnPosition()
        {
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

                returnPosition = hitPosition;
                returnRotation = hitRotation;
                returnAngularVelocity = Random.insideUnitSphere * 50;


                if (predictor.DesignTrajectory(hitPosition, hitRotation, returnAngularVelocity, end, ball.GetGravity().magnitude, maxY, out returnVelocity, out float hitTime))
                {
                    predictor.Set(returnPosition, returnRotation, returnVelocity, returnAngularVelocity);
                    predictor.Predict();
                    UpdateTrajectoryRenderer();
                    return true;
                }
            }
            return false;
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