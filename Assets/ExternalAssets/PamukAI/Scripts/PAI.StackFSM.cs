using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    public static partial class PAI
    {
        #region Stack FSM

        public static void SwitchState(Method newState, ref Stack<Method> fsmState)
        {
            if (fsmState == null) fsmState = new Stack<Method>();
            if (fsmState.Count == 0)
            {
                fsmState.Push(newState);
                return;
            }
            var prev = fsmState.Pop();
            fsmState.Push(newState);
            SwitchState(newState, ref prev);
        }

        //TODO: PopState

        public static void PushState(Method newState, ref Stack<Method> fsmState)
        {
            if (fsmState == null) fsmState = new Stack<Method>();
            if (fsmState.Count == 0)
            {
                fsmState.Push(newState);
                return;
            }

            var prev = fsmState.Peek();
            if (prev == newState)
                return; // if already in stack, do nothing

            GetData(newState).PrevMethod = GetData(prev);
            fsmState.Push(newState);
        }

        public static bool Tick(Stack<Method> fsmState)
        {
            Method prev = default;

            while (true)
            {
                if (fsmState == null || fsmState.Count == 0)
                    return false;

                var current = fsmState.Peek();
                if (prev != null)
                    SwitchState(current, ref prev);

                var res = Tick(current);
                if (res)
                    return true;

                // if method is finished with False, pop it from stack
                prev = fsmState.Pop();
            }
        }

        #endregion
    }
}