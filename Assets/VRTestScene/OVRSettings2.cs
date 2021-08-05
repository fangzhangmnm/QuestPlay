using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRSettings2 : MonoBehaviour
{
    public OVRManager.DepthQuality depthQuality = OVRManager.DepthQuality.Medium;
    public float frameRate = 72;//Quest1 only support 72
    void Awake()
    {
        OVRManager oVRManager = GetComponent<OVRManager>();
        oVRManager.depthQuality = depthQuality;
        OVRPlugin.systemDisplayFrequency = 72;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
