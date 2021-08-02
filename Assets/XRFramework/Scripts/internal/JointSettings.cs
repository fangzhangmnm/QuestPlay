using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    [CreateAssetMenu(fileName = "JointSettings", menuName = "JointSettings", order = 1)]
    public class JointSettings : ScriptableObject
    {
        public float spring = 50000;
        public float damper = 2000;
        public float maxForce = 1000;
        public float angularSpring = 50000;
        public float angularDamper = 2000;
        public float angularMaxForce = 1000;
    }
}