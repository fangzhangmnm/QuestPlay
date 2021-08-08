using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestIK : MonoBehaviour
{
    Animator anim;
    [Range(0,1)]
    public float rightHandWeight;
    public Transform rightHandTarget;
    public Transform rightHandEffector;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public static void SetIK(Animator anim, AvatarIKGoal part,Transform effector, Transform target)
    {
        Quaternion oldRotation = anim.GetIKRotation(part);
        Vector3 oldPosition = anim.GetIKPosition(part);

        anim.SetIKPosition(part, target.position + target.rotation * Quaternion.Inverse(effector.rotation) * (oldPosition - effector.position));
        anim.SetIKRotation(part, target.rotation * Quaternion.Inverse(effector.rotation) * oldRotation);
    }
    private void OnAnimatorIK(int layerIndex)
    {
        SetIK(anim, AvatarIKGoal.RightHand, rightHandEffector, rightHandTarget);
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
    }
}
