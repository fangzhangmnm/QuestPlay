using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace fzmnm.BehaviorTree
{

    public class WaitForTickNode : Node
    {
        bool ticked;
        protected override void Start()
        {
            base.Start();
            ticked = false;
        }
        protected override State Update()
        {
            if (!ticked) { ticked = true; return State.Running; }
            else return State.Succeed;
        }
    }
    public class WaitForSecondsNode : Node
    {
        float duration;
        Func<float> getDurationDelegate;
        public WaitForSecondsNode(float duration) { this.duration = duration; }
        public WaitForSecondsNode(Func<float> getDurationDelegate) { this.getDurationDelegate = getDurationDelegate; }
        double startTime;
        protected override void Start()
        {
            base.Start();
            startTime = Time.timeAsDouble;
            if (getDurationDelegate != null) duration = getDurationDelegate();
        }
        protected override State Update()
        {
            if (Time.timeAsDouble - startTime > duration)
                return State.Succeed;
            else
                return State.Running;
        }
        protected override string GetLogPrefix()
        {
            float d = Mathf.Min(duration, (float)(Time.timeAsDouble - startTime));
            return $"[{d:F2}/{duration:F2}]";
        }
    }
    public class WaitUntilNode : Node
    {
        Func<bool> condition;
        public WaitUntilNode(Func<bool> until) { this.condition = until; }
        protected override State Update()
        {
            return condition() ? State.Running : State.Succeed;
        }
    }
    public class ConditionNode : Node
    {
        Func<bool> condition;
        public ConditionNode(Func<bool> condition) { this.condition = condition; name = condition.Method.Name; }
        protected override State Update()
        {
            return condition() ? State.Succeed : State.Failed;
        }
    }
    public class ActionNode : Node
    {
        Func<State> fullDelegate;
        Func<bool> fullDelegate2;
        Func<bool> prerequestDelegate;
        Action runDelegate;
        Func<bool> completeDelegate;
        public Action onStart;
        public Action onEnd;
        public ActionNode(Func<State> action) { fullDelegate = action; name = action.Method.Name; }
        public ActionNode(Func<bool> action) { fullDelegate2 = action; name = action.Method.Name; }
        public ActionNode OnStart(Action action) { onStart = action; return this; }
        public ActionNode OnEnd(Action action) { onEnd = action; return this; }
        public ActionNode(Action action, Func<bool> done = null, Func<bool> prerequest = null)
        {
            runDelegate = action;
            completeDelegate = done;
            prerequestDelegate = prerequest;
            name = action.Method.Name;
        }
        protected override State Update()
        {
            if (fullDelegate != null) return fullDelegate();
            if (fullDelegate2 != null) return fullDelegate2() ? State.Succeed : State.Failed;
            if (prerequestDelegate != null && !prerequestDelegate()) return State.Failed;
            runDelegate?.Invoke();
            if (completeDelegate != null && !completeDelegate()) return State.Failed;
            return State.Succeed;
        }
        protected override void Start()
        {
            base.Start();
            onStart?.Invoke();
        }
        protected override void End()
        {
            onEnd?.Invoke();
            base.End();
        }
    }
}