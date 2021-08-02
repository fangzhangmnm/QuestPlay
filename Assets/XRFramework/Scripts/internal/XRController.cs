using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace fzmnm.XRPlayer
{
    public class XRController
    {
        public XRNode whichHand;

        public float grip, trigger;
        public bool primaryButton, secondaryButton, primaryAxisClick;
        public Vector2 primaryAxis;


        public void ReadDeviceInputs()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(whichHand);
            if (device != null)
            {
                if (!device.TryGetFeatureValue(CommonUsages.trigger, out trigger)) trigger = 0;
                if (!device.TryGetFeatureValue(CommonUsages.grip, out grip)) grip = 0;
                if (!device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton)) primaryButton = false;
                if (!device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton)) secondaryButton = false;
                if (!device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out primaryAxisClick)) primaryAxisClick = false;
                if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out primaryAxis)) primaryAxis = Vector2.zero;
            }
        }
        public void SendHapticImpulse(float strength, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(whichHand);
            if (device != null)
                device.SendHapticImpulse(0, strength, duration);
        }

        public XRController(XRNode whichHand)
        {
            Debug.Assert(whichHand == XRNode.LeftHand || whichHand == XRNode.RightHand);
            this.whichHand = whichHand;
        }
    }

}