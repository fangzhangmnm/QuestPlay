using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm.XRPlayer
{

    public partial class XRLocomotion : MonoBehaviour
    {
        void EnterGrounded()
        {
            bool isThisStep = controller.DetectGround(out float groundDist, out RaycastHit groundHit);
            if (isThisStep)
                attach.SetAttach(groundHit.transform, Vector3.zero);
            else
                attach.ClearAttach();
        }

        void UpdateGrounded(float dt)
        {
            Vector3 attachedDelta = attach.GetAttachedTranslation();
            transform.position += attachedDelta;

            AdjustColliderAndHead();
            DealSnapRotation(dt);
            DealTrackedRotation();
            AlignRotationToGravity();

            bool isThisStep = controller.DetectGround(out float groundDist, out RaycastHit groundHit);

            Vector3 inputDelta = ProjectHorizontal(head.TransformDirection(new Vector3(inputStickL.x, 0, inputStickL.y))).normalized * inputStickL.magnitude;
            if (inputStickL.magnitude < joystickDeadZone) inputDelta = Vector3.zero;
            if (inputDelta.magnitude > 0)
                isFakeMoving = true;
            inputDelta *= speed * dt;

            Vector3 trackedDelta = ProjectHorizontal(head.position - transform.position);

            Vector3 delta = inputDelta + trackedDelta;
            delta = controller.StepOnGround(delta, out bool isNextStep);
            if (isFakeMoving)
                delta = Vector3.ClampMagnitude(delta, speed * dt);

            bool keepInertiaVelocity = false;
            if (!isThisStep)//Fall
            {
                inertiaVelocity = (delta + attachedDelta) / dt;
                keepInertiaVelocity = true;
                stateMachine.TriggerStateTransition(FallingState);
            }
            else
            {
                if (inputJump.Consume() && stateMachine.currentState.time > .1f)
                {
                    inertiaVelocity = (inputDelta + attachedDelta) / dt + up * jumpSpeed;
                    keepInertiaVelocity = true;
                    stateMachine.TriggerStateTransition(FallingState);
                }
            }

            //Fall prevention
            if (isThisStep && !isNextStep)
            {
                if (!isFakeMoving)
                    delta = Vector3.zero;
                else if (isFakeMoving && Vector3.Dot(inputDelta, trackedDelta) < 0)
                    delta = controller.StepOnGround(delta, out isNextStep);
            }

            //World collision
            delta = controller.SweepCollider(delta, slide: true);
            if (isFakeMoving || !isThisStep)
            {
                transform.position += delta;
                Vector3 resolveCollisionDelta = controller.ResolveCollision();
                if (!keepInertiaVelocity) inertiaVelocity = (delta + attachedDelta+ resolveCollisionDelta) / dt;
                transform.position += resolveCollisionDelta;
                trackingSpace.position -= ProjectHorizontal(head.position - transform.position);

            }
            else
            {
                transform.position += delta;
                if (!keepInertiaVelocity) inertiaVelocity = (delta + attachedDelta) / dt;
                trackingSpace.position -= ProjectHorizontal(delta);
            }

            //UpdateAttach
            if (isThisStep)
                attach.SetAttach(groundHit.transform, Vector3.zero);
            else
                attach.ClearAttach();

        }
        void ExitGrounded()
        {
            attach.ClearAttach();
        }
        void UpdateFalling(float dt)
        {
            AdjustColliderAndHead();
            DealSnapRotation(dt);
            DealTrackedRotation();
            AlignRotationToGravity();

            bool isThisStep = controller.DetectGround(out float groundDist, out RaycastHit groundHit);
            if (isThisStep && groundDist + Vector3.Dot(inertiaVelocity * dt, up) <= .1f * controller.RScaled)
                stateMachine.TriggerStateTransition(GroundedState);

            Vector3 trackedDelta = ProjectHorizontal(head.position - transform.position);

            inertiaVelocity += Physics.gravity * dt;
            inertiaVelocity = Vector3.Lerp(inertiaVelocity, Vector3.zero, drag * dt);

            Vector3 delta = inertiaVelocity * dt + trackedDelta;
            delta = controller.SweepCollider(delta, slide: true);
            transform.position += delta;
            Vector3 resolveCollisionDelta = controller.ResolveCollision();
            transform.position += resolveCollisionDelta;
            trackingSpace.position -= ProjectHorizontal(trackedDelta + resolveCollisionDelta);

            attach.ClearAttach();
        }
    }
}
