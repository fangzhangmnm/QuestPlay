using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm.XRPlayer
{
    public class XRPlayerTracking : MonoBehaviour
    {
        public Transform trackingSpace;
        public Transform head;
        public float playerActualHeight = 1.70f;

        float eyeToTopHeight = .1f;
        public float playerEyeHeight => playerActualHeight - eyeToTopHeight;
        [HideInInspector] public float trackedSpaceBias = 0;

        [Button]
        public void CalibratePlayerHeight()
        {
            playerActualHeight = trackingSpace.InverseTransformPoint(head.position).y + eyeToTopHeight;
            trackedSpaceBias = 0;
        }
        [Button]
        public void CalibrateSeatedHeight()
        {
            trackedSpaceBias = Mathf.Max(0, playerEyeHeight - trackingSpace.InverseTransformPoint(head.position).y);
        }

        //TODO consider avatar
        public float upperBodyHeightTS => playerActualHeight / 2;
        public float lowerBodyHeightTS => playerActualHeight / 2;
    }
}