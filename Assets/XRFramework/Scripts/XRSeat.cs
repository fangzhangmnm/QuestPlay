using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{
    public class XRSeat : XRInteractable
    {
        public Transform hipPos;
        public Transform standUpPos;
        XRLocomotion seatedPlayer;
        public bool isOccupiedByOther { get; private set; } = false;
        public enum LeaveSeatCondition { None, InputJump};
        public LeaveSeatCondition leaveSeatCondition = LeaveSeatCondition.InputJump;

        public Behaviour[] enableWhenSit;

        [Button]
        public void LeaveSeat()
        {
            if (seatedPlayer)
                seatedPlayer.LeaveSeat();
        }

        public override bool CanInteract(XRHand hand, out int priority)
        {
            priority = -2; return isActiveAndEnabled && !isOccupiedByOther;
        }
        public override void OnInteract(XRHand hand)
        {
            Debug.Log($"{name}.OnInteract");
            if (!isOccupiedByOther)
                hand.playerLocomotion.Sit(this);
        }
        public void OnEnterSeat(XRLocomotion player)
        {
            Debug.Assert(!isOccupiedByOther);
            Debug.Log($"EnterSeat {name}");
            seatedPlayer = player;
            foreach (var b in enableWhenSit) if (b) b.enabled = true;
        }
        public void OnLeaveSeat(XRLocomotion player)
        {
            Debug.Log($"LeaveSeat {name}");
            foreach (var b in enableWhenSit) if (b) b.enabled = false;
            seatedPlayer = null;
        }
        private void FixedUpdate()
        {
            if (seatedPlayer)
            {
                if (leaveSeatCondition == LeaveSeatCondition.InputJump && seatedPlayer.inputJump.Consume())
                    LeaveSeat();
            }
        }


        public override void OnEnable()
        {
            if (!hipPos) hipPos = transform;
            if (!standUpPos) standUpPos = transform;
            base.OnEnable();
        }
    }
}
