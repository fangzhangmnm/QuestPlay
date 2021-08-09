using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace fzmnm.BehaviorTree
{
    //https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter06_The_Behavior_Tree_Starter_Kit.pdf
    //First Gen Behavior Tree

    public enum State { Pending, Running, Failed, Succeed, Aborted}
    public abstract class Node
    {
        bool debug_hasEnded = true;
        protected virtual void Start() { if (!debug_hasEnded) Debug.LogAssertion("Start before End"); debug_hasEnded = false; }
        protected abstract State Update();
        protected virtual void End() { debug_hasEnded = true; }//Called if not running next frame

        public State state { get; private set; } = State.Pending; 
        public bool IsRunning => state == State.Running;

        public State Tick() {
            if (state != State.Running)
                Start();
            state = Update();//state is private set. Cannot be modified during Update
            Debug.Assert(state == State.Running || state == State.Failed || state == State.Succeed);
            if (state != State.Running)
                End();
            return state;
        }
        public void Abort()
        {
            if (IsRunning)
            {
                End();
                state = State.Aborted;
            }
        }
        public void Reset()
        {
            if (IsRunning) End();
            state = State.Pending;
        }
        public string name="";
        public string typeName;
        public Node()
        {
            var items = GetType().ToString().Split('.');
            typeName = items[items.Length - 1];
        }
        public Node Name(string name) { this.name = name;return this; }
        protected virtual string GetLogPrefix() => "";
        public virtual void LogNode(StringBuilder stringBuilder,int indent=0, string postfix="", bool expandAll=false)
        {
            char stateChar;
            if (state == State.Running) stateChar = '¡ú';
            else if (state == State.Succeed) stateChar = '¡Ì';
            else if (state == State.Failed) stateChar = '¡Á';
            else if (state == State.Aborted) stateChar = '¢ò';
            else stateChar = '¡õ';
            stringBuilder.AppendLine($"{new string(' ', 4 * indent)}{stateChar}{GetLogPrefix()}{name}({typeName}) {postfix}");
        }
        public string Log(bool expandAll=false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            LogNode(stringBuilder, expandAll: expandAll);
            return stringBuilder.ToString();
        }
    }
    public abstract class Compositor : Node
    {
        protected int current;
        protected List<Node> children=new List<Node>();
        public Compositor Add(params Node[] children){this.children.AddRange(children);return this; }
        protected override void Start() { 
            base.Start(); 
            current = 0;
            foreach (var c in children)
                c.Reset();
        }
        protected override void End()
        {
            base.End();
            foreach (var c in children)
                c.Abort();
        }
        public override void LogNode(StringBuilder stringBuilder, int indent = 0, string postfix = "", bool expandAll = false)
        {
            base.LogNode(stringBuilder, indent, postfix, expandAll);
            for (int i = 0; i < children.Count; ++i)
                if(children[i].state!=State.Pending)
                    children[i].LogNode(stringBuilder, indent + 1, expandAll: expandAll);
        }
        public new Compositor Name(string name) { this.name = name; return this; }
    }
    public class Sequencer : Compositor //Need all success
    {
        protected override State Update()
        {
            for (; current<children.Count;++current)
            {
                Debug.Assert(children[current].state == State.Pending || children[current].state == State.Running);

                State s = children[current].Tick();
                if (s == State.Succeed)
                    continue;
                else //Running or Failed
                    return s;
            }
            return State.Succeed;
        }
    }
    public class Selector : Compositor //Find first success
    {
        protected override State Update()
        {
            for (; current < children.Count; ++current)
            {
                Debug.Assert(children[current].state == State.Pending || children[current].state == State.Running);

                State s = children[current].Tick();
                if (s == State.Failed)
                    continue;
                else //Running or Succeed
                    return s;
            }
            return State.Failed;
        }
    }
    public class ActiveSelector : Compositor //Retry failed
    {
        protected int previous;
        protected override void Start() { base.Start(); previous = -1; }

        protected override State Update()
        {
            for(current = 0; current < children.Count; ++current)
            {
                Debug.Assert(children[current].state == State.Pending || children[current].state == State.Running || children[current].state==State.Failed);

                State s = children[current].Tick();
                if (s == State.Failed)
                    continue;
                else //Running or Succeed
                {
                    if (previous != current && previous!=-1)
                        children[previous].Abort();
                    previous = current;
                    return s;
                }
            }
            return State.Failed;
        }
    }
    public class Parallel : Compositor
    {
        public enum Policy { Once, All };
        public const Policy Once = Policy.Once, All = Policy.All;
        protected Policy successPolicy, failPolicy;
        public Parallel(Policy success, Policy fail) { successPolicy = success; failPolicy = fail; }

        protected override State Update()
        {
            int successCount = 0, failCount = 0;
            for(current=0;current<children.Count;++current)
            {
                var c = children[current];

                if (c.state != State.Succeed && c.state != State.Failed)
                    c.Tick();
                if (c.state == State.Succeed)
                {
                    ++successCount;
                    if (successPolicy == Policy.Once)
                        return State.Succeed;
                }
                if (c.state == State.Failed)
                {
                    ++failCount;
                    if (failPolicy == Policy.Once)
                        return State.Failed;
                }
            }
            if (successPolicy == Policy.All && successCount == children.Count)
                return State.Succeed;
            if (failPolicy == Policy.All && failCount == children.Count)
                return State.Failed;
            return State.Running;
        }
        
    }
    public class Monitor:Parallel
    {
        public Monitor() : base(success: All, fail: Once) { }
        public Monitor AddConditions(params Node[] children)
        {
            foreach(var c in children)
                Add(new MapState(succeed: State.Running).Add(c));
            return this;
        }

        public new Monitor Name(string name) { this.name = name; return this; }
    }

    public abstract class Decorator : Node
    {
        protected Node child;
        public Decorator Add(Node child) { this.child = child; return this; }
        protected override void Start()
        {
            base.Start();
            child?.Reset();
        }
        protected override void End()
        {
            base.End();
            child?.Abort();
        }
        public override void LogNode(StringBuilder stringBuilder, int indent = 0, string postfix = "", bool expandAll = false)
        {
            if (child != null)
                child.LogNode(stringBuilder, indent, $"<{GetLogPrefix()}{name}({typeName})"+ postfix , expandAll: expandAll);
            else
                base.LogNode(stringBuilder, indent, postfix, expandAll: expandAll);

        }
        public new Decorator Name(string name) { this.name = name; return this; }
    }
    public class MapState : Decorator
    {
        public State mapRunning, mapFailed, mapSucceed;
        public MapState(State running = State.Running, State failed = State.Failed, State succeed = State.Succeed){mapRunning = running; mapFailed = failed; mapSucceed = succeed;}
        protected override State Update()
        {
            var s = child.Tick();
            if (s == State.Running) return mapRunning;
            else if (s == State.Failed) return mapFailed;
            else if (s == State.Succeed) return mapSucceed;
            else { Debug.Assert(false); return State.Pending; }
        }
    }
    public class Repeater : Decorator
    {
        int counter;
        public int limit;
        public const int RepeatInfinity = -1;
        public Repeater(int limit = 1) { this.limit = limit; }
        protected override void Start()
        {
            base.Start();
            counter = 0;
        }
        protected override State Update()
        {
            if (child == null) return State.Succeed;
            int alert = 0;
            for (; counter < limit || limit==-1; ++counter)
            {
                child.Tick();
                if (child.state == State.Running) return State.Running;
                else if (child.state == State.Failed) return State.Failed;
                if (++alert > 1000)
                {
                    Debug.LogError("Seemingly Infinite Loop. Halt.");
                    return State.Failed;
                }
                if (limit == -1 && counter > 999) counter = 0;
            }
            return State.Succeed;
        }
        protected override string GetLogPrefix()
        {
            return $"[{counter}/{(limit == -1 ? "¡Þ" : limit.ToString())}]";
        }
    }
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
        public WaitForSecondsNode(float duration) { this.duration = duration; }
        double startTime;
        protected override void Start()
        {
            base.Start();
            startTime = Time.timeAsDouble;
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
    public class ConditionNode: Node
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
        Func<bool> prerequestDelegate;
        Action runDelegate;
        Func<bool> completeDelegate;
        public ActionNode(Func<State> action) { fullDelegate = action;name = action.Method.Name; }
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
            if (prerequestDelegate != null && !prerequestDelegate()) return State.Failed;
            runDelegate();
            if (completeDelegate != null && !completeDelegate()) return State.Failed;
            return State.Succeed;
        }
    }

}

