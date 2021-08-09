using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{

    public class BallAI_1_Serving : MonoBehaviour
    {
        public BoxCollider fieldBounds;
        public BoxCollider targetBounds;
        public PhysicMaterial groundMaterial, racketMaterial;
        public Collider[] obstacles;
        public Plane ground;
        public Ball ball;
        public LineRenderer trajectoryRenderer;
        public BallTrajectoryPredictor predictor;
        public bool flip = false;
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
                yield return null;
                while (fieldBounds.transform.InverseTransformPoint(ball.body.position).y > 0)
                    yield return new WaitForSeconds(.2f);
                yield return new WaitForSeconds(.2f);
                if (fieldBounds.transform.InverseTransformPoint(ball.body.position).y > 0)
                    continue;

                bool success = false;
                Vector3 start = Vector3.zero;
                Vector3 startVelocity = Vector3.zero;
                Quaternion startRotation = Quaternion.identity;
                Vector3 startAngularVelocity = Vector3.zero;
                for (int _i = 0; _i < 5; ++_i)
                {
                    start = new Vector3(
                        Random.Range(-fieldBounds.size.x / 2, fieldBounds.size.x / 2),
                        fieldBounds.size.y / 4,
                        -fieldBounds.size.z / 2
                        );
                    startRotation = fieldBounds.transform.rotation;
                    startAngularVelocity = Random.insideUnitSphere * 50;

                    Vector3 end = new Vector3(
                        Random.Range(-targetBounds.size.x / 2+targetBounds.center.x, targetBounds.size.x / 2 + targetBounds.center.x),
                        0,
                        Random.Range(-targetBounds.size.z /2+targetBounds.center.z, targetBounds.size.z / 2 + targetBounds.center.z)
                        );

                    float maxY = targetBounds.transform.TransformPoint(new Vector3(
                        0,
                        Random.Range(0, targetBounds.size.y/2+targetBounds.center.y),
                        0)).y;

                    if (flip) { start.z *= -1;end.z *= -1; }
                    start = fieldBounds.transform.TransformPoint(start);
                    end = fieldBounds.transform.TransformPoint(end);

                    success = predictor.DesignTrajectory(start, startRotation, startAngularVelocity, end, ball.GetGravity().magnitude, maxY, out startVelocity, out float hitTime);
                    if (success) break;

                }
                if (success)
                {
                    //ball.body.position = start;
                    //yield return new WaitForFixedUpdate();//ball.OnCollisionExit
                    ball.body.position = start;
                    ball.body.rotation = startRotation;
                    ball.body.velocity = startVelocity;
                    ball.body.angularVelocity = startAngularVelocity;

                    predictor.Clear();
                    predictor.Set(ball.body.position, ball.body.rotation, ball.body.velocity, ball.body.angularVelocity);
                    predictor.Predict(record: true);

                    trajectoryRenderer.useWorldSpace = true;
                    trajectoryRenderer.positionCount = predictor.positionCount;
                    trajectoryRenderer.SetPositions(predictor.positions);
                }
                else
                {
                    Debug.Log("BallAI: All Design Failed");
                }
            }
        }




        private void OnValidate()
        {
            if (Mathf.Abs(fieldBounds.center.y - fieldBounds.size.y / 2) > .01f)
                Debug.LogError("the field's transform origin should be located at the buttom of the field collider");
            if (Mathf.Abs(targetBounds.center.y - targetBounds.size.y / 2) > .01f)
                Debug.LogError("the field's transform origin should be located at the buttom of the field collider");
            if (fieldBounds.enabled || !fieldBounds.isTrigger)
                Debug.LogError("the field collider must be isTrigger and disabled");
        }
    }

}