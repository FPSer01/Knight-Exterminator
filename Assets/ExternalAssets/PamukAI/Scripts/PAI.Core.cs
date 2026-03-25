using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    /// <summary>
    /// Core entry point for PamukAI — manages Method execution and state.
    /// </summary>
    public static partial class PAI
    {
        /// <summary>
        /// Delegate type representing an AI method (step-by-step logic).
        /// </summary>
        public delegate bool Method();
        static Dictionary<Method, MethodData> methodToData = new ();

        /// <summary> 
        /// Invokes the given Method (a delegate of type Func<bool>) and tracks its state across frames. 
        /// Returns true if the method completes successfully this frame, or false otherwise. 
        /// </summary>
        public static bool Tick(Method method) => GetData(method).Tick();

        /// <summary>
        /// Immediately resets the internal execution state (MethodData) of the given method.
        /// This forces the method to start over from the beginning(step 0) the next time it is ticked, 
        /// regardless of its current progress.
        /// </summary>
        public static void Reset(Method method)
        {
            GetData(method).Reset();
        }

        /// <summary>
        /// Returns or creates MethodData for the given Method.
        /// </summary>
        internal static MethodData GetData(Method method)
        {
            if (method == null)
                return null;

            if (!methodToData.TryGetValue(method, out var res))
                methodToData[method] = res = new MethodData(method);

            return res;
        }

        #region Current Method Related (context-sensitive static methods)

        /// <summary>
        /// Returns true only for the current step of the method execution. Used to implement step-by-step logic.
        /// </summary>
        public static bool Step() => MethodData.CurrentData.Step();

        /// <summary>
        /// Shortcut for Step(). Used for compact code. Returns true only for the current step of the method execution.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never), DebuggerHidden]
        public static bool STEP => MethodData.CurrentData.Step();

        /// <summary>
        /// Returns true only once, at the moment when the execution flow reaches it for the first time during the current Method run.
        /// </summary>
        public static bool DoOnce() =>
            MethodData.CurrentData.Step() && MethodData.CurrentData.StartStepFrame == Time.frameCount;

        /// <summary>
        /// Shortcut for DoOnce(). True only on the first frame of the step.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never), DebuggerHidden]
        public static bool ONCE =>
            MethodData.CurrentData.Step() && MethodData.CurrentData.StartStepFrame == Time.frameCount;

        /// <summary>
        /// Resets the current Method state, restarting step sequence from the next Tick.
        /// </summary>
        public static void Reset() => MethodData.CurrentData.Reset();

        /// <summary>
        /// Time.time of when the current step started.
        /// </summary>
        public static float StartStepTime => MethodData.CurrentData.StartStepTime;

        /// <summary>
        /// Frame index when the current step started.
        /// </summary>
        public static int StartStepFrame => MethodData.CurrentData.StartStepFrame;

        /// <summary>
        /// Time.time of when the current Method was started.
        /// </summary>
        public static float StartMethodTime => MethodData.CurrentData.StartMethodTime;

        /// <summary>
        /// Creates Step and returns true if the step is still within the given time duration.
        /// </summary>
        public static bool Wait(float seconds) =>
            MethodData.CurrentData.Step() && MethodData.CurrentData.StartStepTime + seconds > Time.time;

        /// <summary>
        /// Logs the message to the Console only once.
        /// </summary>
        public static void LogOnce(string text)
        {
            if (DoOnce()) UnityEngine.Debug.Log(text);
        }

        /// <summary>
        /// Returns the MethodData of the currently executing Method.
        /// </summary>
        internal static MethodData CurrentData => MethodData.CurrentData;

        /// <summary>
        /// Returns persistent local data dictionary for the currently executing Method.
        /// </summary>
        public static Dictionary<string, object> Data => MethodData.CurrentData.Data;

        #endregion
    }
}