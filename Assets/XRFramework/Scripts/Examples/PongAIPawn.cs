using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongAIPawn : MonoBehaviour
{
    public PongAI.Task currentTask;
    [HideInInspector] public Transform fieldRef;
    [HideInInspector] public Rigidbody body;
    public float armMass = 3f;
    public GameObject taskSign;
    public Vector3 idlePosition;
    public Transform hitPos;
    [HideInInspector] public Racket racket;


    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        racket = GetComponent<Racket>();
        body.useGravity = false;
        body.isKinematic = false;
        body.mass += armMass;
    }
    private void FixedUpdate()
    {
        float velocitySmoothTime = .01f;
        float angularVelocitySmoothTime = .01f;
        float maxMoveVelocity = 20f;
        float maxMoveAngularVelocity = 10f;
        if (currentTask != null && currentTask.timeRemaining > 0)
        {

            var ct = currentTask;
            Vector3 hitPosDelta = Quaternion.Inverse(transform.rotation)*(hitPos.position - transform.position);

            if (ct.timeRemaining > ct.readyTime)
            {

                Quaternion targetRotation = GetTargetRotation(fieldRef.TransformVector(ct.normal));
                Vector3 targetAngularVelocity = GetRotationVector(body.rotation, targetRotation) / (ct.timeRemaining - ct.readyTime);
                targetAngularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, maxMoveAngularVelocity);
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, targetAngularVelocity, Time.fixedDeltaTime / angularVelocitySmoothTime);

                Vector3 targetPosition = fieldRef.TransformPoint(ct.position - ct.velocity * ct.readyTime)-targetRotation* hitPosDelta;
                Vector3 targetVelocity = (targetPosition - body.position) / (ct.timeRemaining - ct.readyTime);

                targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxMoveVelocity);
                body.velocity = Vector3.Lerp(body.velocity, targetVelocity, Time.fixedDeltaTime / velocitySmoothTime);
            }
            else if (ct.timeRemaining > .1f * ct.readyTime)
            {
                Quaternion targetRotation = GetTargetRotation(fieldRef.TransformVector(ct.normal));
                Vector3 targetAngularVelocity = GetRotationVector(body.rotation, targetRotation) / .1f;
                targetAngularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, maxMoveAngularVelocity);
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, targetAngularVelocity, Time.fixedDeltaTime / angularVelocitySmoothTime);

                Vector3 targetPosition = fieldRef.TransformPoint(ct.position) - targetRotation * hitPosDelta;
                Vector3 targetVelocity = (targetPosition - body.position) / (ct.timeRemaining);
                targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxMoveVelocity);
                body.velocity = Vector3.Lerp(body.velocity, targetVelocity, Time.fixedDeltaTime / velocitySmoothTime);

            }
            else if (ct.timeRemaining > 0)
            {
                Vector3 targetVelocity = fieldRef.InverseTransformVector(ct.velocity);
                Quaternion targetRotation = GetTargetRotation(fieldRef.TransformVector(ct.normal));
                body.velocity = targetVelocity;
                body.angularVelocity = Vector3.zero;
                body.rotation = targetRotation;
                //body.velocity = Vector3.Lerp(body.velocity, targetVelocity, Time.fixedDeltaTime / velocitySmoothTime);
                //Vector3 targetAngularVelocity = GetRotationVector(body.rotation, targetRotation) / .1f;
                //targetAngularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, maxMoveAngularVelocity);
                //body.angularVelocity = Vector3.Lerp(body.angularVelocity, targetAngularVelocity, Time.fixedDeltaTime / angularVelocitySmoothTime);

            }

            ct.timeRemaining -= Time.fixedDeltaTime;

            if (taskSign)
            {
                Quaternion targetRotation = GetTargetRotation(fieldRef.TransformVector(ct.normal));
                taskSign.SetActive(true);
                taskSign.transform.position = fieldRef.TransformPoint(ct.position);
                taskSign.transform.rotation = targetRotation;
            }
            afterTaskCD = .1f;
        }
        else
        {
            afterTaskCD -= Time.fixedDeltaTime;

            if (afterTaskCD <= 0)
            {
                Vector3 targetVelocity = (fieldRef.TransformPoint(idlePosition) - body.position) / .1f;
                body.velocity = Vector3.Lerp(body.velocity, Vector3.zero, Time.fixedDeltaTime / velocitySmoothTime);

                Quaternion targetRotation = GetTargetRotation(fieldRef.TransformVector(Vector3.forward));
                Vector3 targetAngularVelocity = GetRotationVector(body.rotation, targetRotation) / .1f;
                body.angularVelocity = Vector3.Lerp(body.angularVelocity, targetAngularVelocity, Time.fixedDeltaTime / angularVelocitySmoothTime);

            }
            if (taskSign)
                taskSign.SetActive(false);
        }
    }
    float afterTaskCD = 0;
    public bool EvaluateTask(PongAI.Task task)
    {
        if (task == null) return false;
        if (task.position.z > 0) return false;
        return true;
    }
    Quaternion GetTargetRotation(Vector3 normal)
    {
        return Quaternion.LookRotation(normal, Vector3.up);
    }
    Vector3 GetRotationVector(Quaternion from, Quaternion to)
    {
        (to * Quaternion.Inverse(from)).ToAngleAxis(out float angle, out Vector3 axis);
        return angle * Mathf.Deg2Rad * axis;
    }
}
