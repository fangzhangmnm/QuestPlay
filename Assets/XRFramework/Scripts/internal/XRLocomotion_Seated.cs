using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{

    public partial class XRLocomotion : MonoBehaviour
    {

        [HideInInspector] public XRSeat seat;
        float seatHeadBias;
        public void Sit(XRSeat seat)
        {
            this.seat = seat;
            stateMachine.DoStateTransitionImmediately(SeatedState);
        }
        public void LeaveSeat()
        {
            stateMachine.DoStateTransitionImmediately(GroundedState);
        }
        void EnterSeated()
        {
            Debug.Assert(seat && !seat.isOccupiedByOther);
            transform.rotation = seat.hipPos.rotation;
            transform.position = seat.hipPos.position - transform.TransformVector(Vector3.up * tracking.lowerBodyHeightTS);
            attach.SetAttach(seat.hipPos, Vector3.zero);
            seat.OnEnterSeat(this);
        }
        void UpdateSeated(float dt)
        {
            if (!seat ||!seat.isActiveAndEnabled || inputJump) { LeaveSeat();return; }

            Vector3 attachedDelta = attach.GetAttachedTranslation();
            transform.position += attachedDelta;
            inertiaVelocity = attachedDelta / dt;

        }
        void ExitSeated()
        {
            if (seat)
            {
                transform.position = seat.standUpPos.position+attach.attachedVelocity*Time.fixedDeltaTime;
                transform.rotation = seat.standUpPos.rotation;
                seat.OnLeaveSeat(this);
            }
            attach.ClearAttach();
        }
    }
}