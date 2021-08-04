using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

namespace fzmnm.XRPlayer
{
    public class XRMouseLook : MonoBehaviour
    {
        public Transform trackingSpace;
        public Transform head;
        public Transform leftHand, rightHand;
        public XRLocomotion locomotion;
        public XRHand pickUpHand;
        Vector2 mouseDelta;
        Vector3 leftHandLocalPos, rightHandLocalPos;
        private void Start()
        {

            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
            if (xrDisplaySubsystems.Exists(x => x.running))
                enabled = false;
            else
            {
                leftHandLocalPos = leftHand.localPosition;
                rightHandLocalPos = rightHand.localPosition;
                pickUpHand.mouseControl = true;
                locomotion.mouseControl = true;
            }
        }
        void Update()
        {
            mouseDelta += Mouse.current.delta.ReadValue();
            if (Mouse.current.leftButton.isPressed)
                Cursor.lockState = CursorLockMode.Locked;
            if (Keyboard.current.escapeKey.isPressed)
                Cursor.lockState = CursorLockMode.None;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float rotationX = head.localEulerAngles.y + mouseDelta.x * 1f;
                float rotationY = head.localEulerAngles.x - mouseDelta.y * 1f;
                rotationY = Mathf.Clamp(rotationY < 180 ? rotationY : rotationY - 360, -80, 80);
                rotationX = Mathf.Repeat(rotationX, 360);
                head.localEulerAngles = new Vector3(rotationY, rotationX, 0);
                leftHand.localEulerAngles = new Vector3(rotationY, rotationX, 0);
                rightHand.localEulerAngles = new Vector3(rotationY, rotationX, 0);
                Quaternion q = Quaternion.Euler(0, rotationX, 0);
                leftHand.localPosition = q * leftHandLocalPos;
                rightHand.localPosition = q * rightHandLocalPos;

            }
            mouseDelta = Vector2.zero;

            if(Mouse.current.leftButton.wasPressedThisFrame)
                pickUpHand.device.grip= pickUpHand.device.grip > .5f ? 0 : 1;
            pickUpHand.device.trigger = Mouse.current.rightButton.isPressed ? 1 : 0;

            locomotion.inputStickL = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) locomotion.inputStickL += Vector2.up;
            if (Keyboard.current.sKey.isPressed) locomotion.inputStickL += Vector2.down;
            if (Keyboard.current.aKey.isPressed) locomotion.inputStickL += Vector2.left;
            if (Keyboard.current.dKey.isPressed) locomotion.inputStickL += Vector2.right;
            locomotion.inputStickR = Vector2.zero;
            if (Keyboard.current.leftArrowKey.isPressed) locomotion.inputStickR += Vector2.left;
            if (Keyboard.current.rightArrowKey.isPressed) locomotion.inputStickR += Vector2.right;
            locomotion.inputJump.Update(Keyboard.current.spaceKey.isPressed);
        }
    }
}