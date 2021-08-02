using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{
    public class StateMachine
    {
        public delegate void StateEvent();
        public delegate void StateUpdate(float dt);
        public class State
        {
            public State(string name) { this.name = name; }
            public string name;
            public float time;
            public bool isFirstUpdate;
            public StateEvent OnEnter, OnExit, LateUpdate;
            public StateUpdate Update;
        }
        private State _currentState,_newState;
        bool _stateTransitionTrigger;
        public State currentState => _currentState;

        public void DoStateTransitionImmediately(State newState)
        {
            _newState = newState;
            DoStateTransition();
        }
        public void TriggerStateTransition(State newState, bool triggerEventsIfEnterSame = true)
        {
            //if (logTransition)
            //    Debug.Log($"Trigger State Transition {_currentState?.name}=>{_newState?.name}");

            if (newState==_currentState && newState == _newState && !triggerEventsIfEnterSame) 
                return;
            else
            {
                _newState = newState;
                _stateTransitionTrigger = true;
            }
        }
        void DoStateTransition()
        {
            if (logTransition)
                Debug.Log($"Do State Transition {_currentState?.name}=>{_newState?.name}");

            _currentState?.OnExit?.Invoke();
            _newState.time = 0; _newState.isFirstUpdate = true;
            _newState.OnEnter?.Invoke();
            _currentState = _newState;
        }
        public void UpdateState(float dt)
        {
            if (_stateTransitionTrigger) DoStateTransition(); _stateTransitionTrigger = false;
            if (_currentState!=null)
            {
                _currentState.time += dt;
                _currentState.isFirstUpdate = false;
                _currentState.Update.Invoke(dt);
            }
        }
        public void LateUpdateState()
        {
            _currentState.LateUpdate?.Invoke();
        }
        public bool logTransition = false;
    }
}
