using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Profiling;
using fzmnm.BehaviorTree;

public class TestBehaviorTree : MonoBehaviour
{
    Node tree, tree2;
    [TextArea(5, 15)]
    public string log;
    private void Start()
    {
        var patrol =
            new Monitor().Name("Patrol").AddConditions(
                new ConditionNode(() => tired < 10f).Name("Not tired"))
            .Add(
                
                    new Sequencer().Add(
                        new ActionNode(this.FetchNextTarget),
                        new ActionNode(this.MoveToTarget),
                        new WaitForSecondsNode(1)
                        )
                    
            );
        var rest =
            new Sequencer().Name("Rest").Add(
                new ActionNode(() =>SetTarget(new Vector3(5,0,5))),
                new ActionNode(MoveToTarget),
                new ActionNode(Rest).Name("Rest")
                );
        tree =
            new Repeater(Repeater.RepeatInfinity).Add(
                new Selector().Add(
                    patrol, 
                    rest
            ));
        tree2 = 
            new Repeater(Repeater.RepeatInfinity).Add(new Sequencer().Name("Patrol Route").Add(
                new ActionNode(() => SetTarget(new Vector3(0, 0, 10))),
                new WaitForTickNode(),
                new ActionNode(() => SetTarget(new Vector3(10, 0, 10))),
                new WaitForTickNode(),
                new ActionNode(() => SetTarget(new Vector3(10, 0, 0))),
                new WaitForTickNode(),
                new ActionNode(() => SetTarget(new Vector3(0, 0, 0))),
                new WaitForTickNode()
            ));
    }
    public float tired;
    public Vector3 target;
    void SetTarget(Vector3 pos) { target = pos; }
    void FetchNextTarget()
    {
        //target = UnityEngine.Random.insideUnitSphere * 10;
        //target = Vector3.ProjectOnPlane(target, Vector3.up);
        tree2.Tick();
    }
    State MoveToTarget()
    {
        transform.position += Vector3.ClampMagnitude(target - transform.position, Time.fixedDeltaTime * 3f);
        tired += Time.fixedDeltaTime;

        if ((target - transform.position).magnitude < Time.fixedDeltaTime * 3f)
            return State.Succeed;
        else
            return State.Running;
    }
    State Rest()
    {
        tired -= 5 * Time.fixedDeltaTime;
        if (tired < 0) tired = 0;
        if (tired <= 0)
            return State.Succeed;
        else
            return State.Running;
    }
    private void FixedUpdate()
    {
        Profiler.BeginSample("Behavior Tree");
        tree.Tick();
        Profiler.EndSample();
        log = tree.Log(expandAll: false);
        log += "\n";
        log += tree2.Log(expandAll: false);
    }
}