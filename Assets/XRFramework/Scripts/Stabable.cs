using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stabable : MonoBehaviour
{
    private void OnValidate()
    {
        Debug.Assert(GetComponentInParent<Rigidbody>());
    }
}
