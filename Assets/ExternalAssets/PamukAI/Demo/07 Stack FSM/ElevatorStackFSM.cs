using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class ElevatorStackFSM : MonoBehaviour
    {
        public Transform floor1;
        public Transform floor2;

        Stack<Method> fsm = new Stack<Method>();

        void Start()
        {
            // This Example demonstrates a simple Elevator system using a Stack-based FSM.
            // The elevator can move between two floors, pause, and resume movement in previous direction using a stack to manage states.
            // Main feature is the ability to resume the elevator moving direction after Pause state.

            SwitchState(LiftUp, ref fsm);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                PushState(StopLift, ref fsm); // Pause the elevator and save the current state to the stack

            Tick(fsm);// Execute the FSM

            // Update the name to reflect the current state stack
            name = string.Join(" - ", fsm.Select(m => m.Method.Name));
        }

        bool LiftUp()
        {
            MoveTo(floor2);
            if (IsNear(floor2, 0.1f))
                SwitchState(LiftDown, ref fsm);
            return true;
        }

        bool LiftDown()
        {
            MoveTo(floor1);
            if (IsNear(floor1, 0.1f))
                SwitchState(LiftUp, ref fsm);
            return true;
        }

        bool StopLift()
        {
            if (Wait(2))
                return true;

            // Fallback to the last state in the stack
            return false;
        }

        bool IsNear(Transform target, float distance) => Vector3.Distance(transform.position, target.position) < distance;
        void MoveTo(Transform target) { transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * 1f); }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 300, 300, 120), "Press Space to Pause elevator.\r\n\r\nAlso use ArrowUp and ArrowDown \r\nto change elevator direction.", GUI.skin.box);
        }
    }
}