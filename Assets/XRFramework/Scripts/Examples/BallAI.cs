using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallAI : MonoBehaviour
{
    public BoxCollider fieldBounds;
    public Plane ground;
    public Ball ball;
    public LineRenderer trajectoryRenderer;
    public BallTrajectoryPredictor trajectoryPredictor;
    private void Start()
    {
        ground = new Plane(fieldBounds.transform.up, fieldBounds.transform.TransformPoint(Vector3.up * (fieldBounds.center.y - fieldBounds.size.y / 2)));
        trajectoryPredictor = new BallTrajectoryPredictor(ball,ground);
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
            if (fieldBounds.transform.InverseTransformPoint(ball.body.position).y > 0) 
                yield return new WaitForSeconds(.2f);
            //yield return new WaitForSeconds(1f);
            if (fieldBounds.transform.InverseTransformPoint(ball.body.position).y > 0)
                continue;

            bool success = false;
            Vector3 start = Vector3.zero;
            Vector3 startVelocity = Vector3.zero;
            Quaternion startRotation = Quaternion.identity;
            Vector3 startAngularVelocity = Vector3.zero;
            for (int _i = 0; _i < 5; ++_i)
            {
                start = fieldBounds.transform.TransformPoint(new Vector3(
                    Random.Range(-fieldBounds.size.x / 2, fieldBounds.size.x / 2),
                    fieldBounds.size.y / 4,
                    -fieldBounds.size.z / 2
                    ));
                startRotation = fieldBounds.transform.rotation;
                startAngularVelocity = Random.insideUnitSphere * 50;

                Vector3 end = fieldBounds.transform.TransformPoint(new Vector3(
                    Random.Range(-fieldBounds.size.x / 2, fieldBounds.size.x / 2),
                    0,
                    Random.Range(fieldBounds.size.z / 8, fieldBounds.size.z / 2)
                    ));

                float maxY = fieldBounds.transform.TransformPoint(new Vector3(
                    0,
                    Random.Range(0, fieldBounds.size.y / 2),
                    0)).y;

                success = trajectoryPredictor.Design(start, startRotation, startAngularVelocity, end, ball.GetGravity().magnitude, maxY, out startVelocity, out float hitTime);
                if (success) break;

            }
            if (success)
            {

                ball.body.position = start;
                ball.body.velocity = startVelocity;
                ball.body.angularVelocity = Vector3.zero;

                trajectoryPredictor.Clear();
                trajectoryPredictor.Set(ball.body.position, ball.body.rotation, ball.body.velocity, ball.body.angularVelocity);
                trajectoryPredictor.Predict(record: true);

                trajectoryRenderer.useWorldSpace = true;
                trajectoryRenderer.positionCount = trajectoryPredictor.positionCount;
                trajectoryRenderer.SetPositions(trajectoryPredictor.points);
            }



        }
    }
    private void OnValidate()
    {
        if (Mathf.Abs(fieldBounds.center.y - fieldBounds.size.y / 2) > .01f)
            Debug.LogError("the field's transform origin should be located at the buttom of the field collider");
        if (fieldBounds.enabled || !fieldBounds.isTrigger)
            Debug.LogError("the field collider must be isTrigger and disabled");
    }
}
