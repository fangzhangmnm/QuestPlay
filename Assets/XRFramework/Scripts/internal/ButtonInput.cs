using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace fzmnm
{
    public class ButtonInput
    {
        public void Update(bool value)
        {
            isPressing = value;
            if (!value) hasTriggered = false;
        }
        public bool Consume()
        {
            if (isPressing && !hasTriggered)
            {
                hasTriggered = true;
                return true;
            }
            return false;
        }
        public bool isPressing=false;
        public bool hasTriggered=true;
    }
}