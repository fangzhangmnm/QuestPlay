using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Diagnostics;

namespace fzmnm.GOAP
{
    public class State//TODO
    {
        public int[] conditions;
        public State(State s) { conditions = (int[])s.conditions.Clone(); }

        public State(Type enumType) {
            int n = 0;
            foreach (int v in Enum.GetValues(enumType))
                if (n < v) n = v;
            conditions = new int[n+1];
        }
        public State(int n) { conditions = new int[n]; }
        public State Set(int key, int value) { conditions[key] = value;return this; }
        public State Set<T1, T2>(T1 key, T2 value) => Set(Convert.ToInt32(key), Convert.ToInt32(value));
        public int this[int key] { get => conditions[key];set => conditions[key] = value; }
        public int this[Enum key] { get => conditions[Convert.ToInt32(key)];set => conditions[Convert.ToInt32(key)] = value; }
        public string GetLog(Type enumType)
        {
            StringBuilder sb = new StringBuilder();
            var values = Enum.GetValues(enumType);
            var names = Enum.GetNames(enumType);
            for (int i = 0; i < values.Length; ++i)
                sb.Append($"{names[i]}:{conditions[(int)values.GetValue(i)]}, ");
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var i in conditions)
                sb.Append($"{i}, ");
            return sb.ToString();
        }
    }
    public enum ActionResult { Success, Failed,Pending}
    public class Action
    {
        public List<ValueTuple<int, int>> conditions = new List<ValueTuple<int, int>>();
        public List<ValueTuple<int, int>> nconditions = new List<ValueTuple<int, int>>();
        public List<ValueTuple<int, int>> effects = new List<ValueTuple<int, int>>();
        public float GetCost(State current, State next)
        {
            float cost;
            if (getCost != null) cost= getCost(current,next);
            else if (getCost1 != null) cost= getCost1();
            else cost= defaultCost;
            return cost;
        }
        public bool IsAvailable(State state)
        {
            foreach (var kv in conditions)
                if (state.conditions[kv.Item1] != kv.Item2)
                    return false;
            foreach (var kv in nconditions)
                if (state.conditions[kv.Item1] == kv.Item2)
                    return false;
            return true;
        }
        public State GetNewState(State state)
        {
            State rtval = new State(state);
            foreach (var kv in effects)
                rtval.conditions[kv.Item1] = kv.Item2;
            customEffect?.Invoke(rtval);
            return rtval;
        }
        public Func<State,State,float> getCost;
        public Func<float> getCost1;
        public Action<State> customEffect;
        public float defaultCost;
        public Func<ActionResult> callback;
        public string name;
        public Action(string name,Func<ActionResult> callback=null) { this.name = name;this.callback = callback; }
        public Action Cost(float defaultCost) { this.defaultCost= defaultCost; return this; }
        public Action Cost(Func<State, State, float> costFunction) { getCost = costFunction;return this; }
        public Action Cost(Func<float> costFunction) { getCost1 = costFunction;return this; }
        public Action Condition(int key,int value) { conditions.Add((key, value)); return this; }
        public Action Condition<T1,T2>(T1 key, T2 value) => Condition(Convert.ToInt32(key), Convert.ToInt32(value));
        public Action NCondition(int key, int value) { nconditions.Add((key, value)); return this; }
        public Action NCondition<T1, T2>(T1 key, T2 value) => NCondition(Convert.ToInt32(key), Convert.ToInt32(value));
        public Action RemoveCondition(int k) { conditions.RemoveAll((ValueTuple<int, int> t) =>  t.Item1 == k); return this; }
        public Action RemoveCondition<T1>(T1 key) => RemoveCondition(Convert.ToInt32(key));
        public Action Effect(int key, int value) { effects.Add((key, value));return this; }
        public Action Effect<T1, T2>(T1 key, T2 value) => Effect(Convert.ToInt32(key), Convert.ToInt32(value));
        public Action CustomEffect(Action<State> customEffect) { this.customEffect = customEffect;return this; }
    }
    public class Goal
    {
        public List<ValueTuple<int, int>> conditions=new List<ValueTuple<int, int>>();
        public bool IsSatisfied(State state)
        {
            foreach (var kv in conditions)
                if (state.conditions[kv.Item1] != kv.Item2)
                    return false;
            return true;
        }
        public float Heuristic(State state,float minCost)
        {
            float rtval = 0;
            foreach (var kv in conditions)
                if (state.conditions[kv.Item1] != kv.Item2)
                    rtval += minCost;
            return rtval;
        }
        /*
        public float Heuristic(State state, float[] heuristicValues)
        {
            float rtval = 0;
            foreach (var kv in conditions)
                if (state.conditions[kv.Item1] != kv.Item2)
                    rtval += heuristicValues[kv.Item1];
            return rtval;
        }*/
        public string name;
        public Goal(string name) { this.name = name; }
        public Goal Condition(int key, int value) { conditions.Add((key, value)); return this; }
        public Goal Condition<T1, T2>(T1 key, T2 value) => Condition(Convert.ToInt32(key), Convert.ToInt32(value));
    }
    public class Planner
    {
        public List<Action> actions=new List<Action>();
        public Goal goal;
        public static Planner operator +(Planner planner, Action action) { planner.actions.Add(action); return planner; }
        //public float[] heuristicValues = null;
        public float minCost = 1;
        public int maxSearchSteps = 10000;
        public List<Action> path=null;


        public bool Plan(State start)
        {
            lastSearchTimer.Restart();
            path = null;
            pathStart = start;
            var open = new PriorityQueue<(State,float), float>();
            open.Enqueue((start,0), goal.Heuristic(start,minCost));
            var gScores = new Dictionary<State, float>(new _StateComparer());
            var from = new Dictionary<State, (State, Action, float)>(new _StateComparer());
            
            lastSearchStepCount = 0;
            while (open.Count > 0)
            {
                if (++lastSearchStepCount > maxSearchSteps) return false;
                open.Dequeue(out var _t, out float f);//f=g+h, g start to current, h heuristic
                (State current, float g) = _t;
                if (goal.IsSatisfied(current))
                {
                    path = new List<Action>();
                    pathCost = g;
                    pathCosts = new List<float>();
                    while (current!=start)
                    {
                        if(from.TryGetValue(current, out var _s))
                        {
                            (State previous, Action action, float actionCost) = _s;
                            current = previous;
                            path.Add(action);
                            pathCosts.Add(actionCost);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("from dictionary is broken!");
                            path = null;
                            lastSearchTimer.Stop();
                            return false;
                        }
                    }
                    path.Reverse();
                    pathCosts.Reverse();
                    lastSearchTimer.Stop();
                    return true;
                }
                foreach (var action in actions)
                    if (action.IsAvailable(current))
                    {
                        State newState = action.GetNewState(current);
                        float actionCost = action.GetCost(current, newState);
                        actionCost = Mathf.Max(minCost, actionCost);
                        float newG = g + actionCost;
                        if (!gScores.TryGetValue(newState, out float oldG) || newG < oldG)
                        {
                            gScores[newState] = newG;
                            from[newState] = (current, action, actionCost);
                            float newF = newG + goal.Heuristic(newState,minCost);
                            open.Enqueue((newState, newG), newF);
                        }
                    }
            }
            lastSearchTimer.Stop();
            return false;
        }
        private State pathStart;
        private float pathCost;
        private List<float> pathCosts;
        public int lastSearchStepCount;
        Stopwatch lastSearchTimer=new Stopwatch();
        public string GetPathLog(Type enumType,bool showStateChange=false)
        {
            if (path == null) return "Failed";
            StringBuilder sb = new StringBuilder();
            if (!showStateChange)
            {
                sb.Append($"(Total:{pathCost:F1})");
                int i = 0;
                foreach (var action in path)
                    sb.Append($"({pathCosts[i++]:F1}){action.name}>>");
                sb.Append(goal.name);
            }
            else
            {
                sb.Append($"(TotalCost: {pathCost:F1})");
                var current = pathStart;
                int i = 0;
                sb.Append($"\nStart : {current.GetLog(enumType)}\n");
                foreach (var action in path)
                {
                    current = action.GetNewState(current);
                    sb.Append($"(Cost: {pathCosts[i++]:F1}){action.name} >> {current.GetLog(enumType)}\n");
                }
                sb.Append(goal.name);
                sb.Append($"Total Search Steps: {lastSearchStepCount}\n");
                sb.Append($"Total Search Time: {lastSearchTimer.Elapsed.TotalMilliseconds:F6}ms\n");
            }
            return sb.ToString();
        }
    }

    public class _StateComparer : IEqualityComparer<State>
    {
        public bool Equals(State a, State b)
        {
            int[] x = a.conditions, y = b.conditions;
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }
        public int GetHashCode(State obj)
        {
            int result = 17;
            for (int i = 0; i < obj.conditions.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj.conditions[i];
                }
            }
            return result;
        }
    }
}