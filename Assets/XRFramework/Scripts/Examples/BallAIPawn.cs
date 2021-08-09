using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm {
    [RequireComponent(typeof(Animator))]
    public class BallAIPawn : MonoBehaviour
    {
        
        Animator anim;
        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();

        }
    }
}

