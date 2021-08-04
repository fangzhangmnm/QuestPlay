using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class JointEndurance : MonoBehaviour
{
    public Joint joint;
    public float currentEndurance = 1;
    public UnityEvent<float> updateEndurance=new UnityEvent<float>();

    private void FixedUpdate()
    {
        //updateEndurance.Invoke(currentEndurance);
        if (joint)
        {

        }

    }
}
