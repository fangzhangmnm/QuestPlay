using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitLog : MonoBehaviour
{
    public Text text;
    private void OnCollisionEnter(Collision collision)
    {
        text.text += $"\n{collision.relativeVelocity.magnitude}";
        int start = text.text.Length - 1000;
        if (start > 0) text.text = text.text.Substring(start);
    }
}
