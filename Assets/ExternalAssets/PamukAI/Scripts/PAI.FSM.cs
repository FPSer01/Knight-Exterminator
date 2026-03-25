using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    public static partial class PAI
    {
        #region FSM

        public static bool OnExit => CurrentData.OnExit();

        public static bool SwitchState(Method newState, ref Method fsmState)
        {
            if (fsmState == newState)
                return false;
            if (newState != null)
                GetData(newState).PrevMethod = GetData(fsmState);
            fsmState = newState;
            return true;
        }

        #endregion
    }
}