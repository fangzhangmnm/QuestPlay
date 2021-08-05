using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm.XRPlayer
{
    [RequireComponent(typeof(XRPickup))]
    public class BallRespawn : MonoBehaviour
    {
        public XRPickup ball;
        XRPickup grabable;
        private void Awake()
        {
            grabable = GetComponent<XRPickup>();
        }
        private void FixedUpdate()
        {
            if (grabable.hand)
                if (grabable.hand.otherHand.device.grip > .5f && !grabable.hand.otherHand.attached)
                    grabable.hand.otherHand.Attach(ball, ball.transform.position, ball.transform.rotation);
        }
        private void OnValidate()
        {
            grabable = GetComponent<XRPickup>();
            if (grabable.canSwapHand)
                Debug.LogWarning("The Controller should not enable canSwapHand");
        }
    }

}
