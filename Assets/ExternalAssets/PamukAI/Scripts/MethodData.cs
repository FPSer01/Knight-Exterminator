using System.Collections.Generic;
using UnityEngine;

namespace PamukAI
{
    /// <summary>
    /// Holds persistent state for a specific PAI.Method.
    /// Automatically managed by the system and allows per-frame step-based execution.
    /// </summary>
    public partial class MethodData
    {
        /// <summary> The actual method delegate (Func<bool>) assigned to this MethodData. </summary>
        public readonly PAI.Method Method;

        /// <summary> Current execution step within the method. Updated automatically by Step(). </summary>
        public int SequenceStep { get; private set; }

        /// <summary> Time (in seconds) when the method was first started. Resets when method restarts. </summary>
        public float StartMethodTime { get; private set; }

        /// <summary> Frame index when the current step started. </summary>
        public int StartStepFrame { get; private set; }

        /// <summary> Time (in seconds) when the current step started. </summary>
        public float StartStepTime { get; private set; }

        /// <summary> Utility score used in utility-based AI selection. </summary>
        public float Utility { get; set; } = 1f;

        /// <summary> Optional link to previous method (used by FSM transitions). </summary>
        public MethodData PrevMethod { get; set; }

        /// <summary> A key-value local data dictionary preserved between frames. </summary>
        public Dictionary<string, object> Data => data ??= new Dictionary<string, object>();

        /// <summary> Returns the currently executing method data (used internally by the system). </summary>
        public static MethodData CurrentData { get; private set; }

        // Internal execution state
        int iStep;
        int lastTickFrame;
        int startMethodFrame;
        bool hasOnExit;
        bool isOnExitCalling;
        bool isStarted;
        bool lastResult;
        bool autoResetByFrames = true;
        bool disableMultiTickInSameFrame = true;
        Dictionary<string, object> data;

        static readonly Stack<MethodData> executeStack = new();

        /// <summary>
        /// Creates new MethodData instance for the given method.
        /// </summary>
        public MethodData(PAI.Method method)
        {
            Method = method ?? throw new System.ArgumentNullException(nameof(method));
        }

        /// <summary>
        /// Executes the method, preserving step state and handling OnExit logic.
        /// Returns true if the method is still running or completed successfully.
        /// Returns false if the method has finished execution and should be reset.
        /// </summary>
        public bool Tick()
        {
            var frame = Time.frameCount;
            if (disableMultiTickInSameFrame && lastTickFrame == frame)
            if (!isOnExitCalling)
                return lastResult; // if method is called in same frame, return last result

            // Handle OnExit for previous FSM method
            if (PrevMethod != null && PrevMethod.hasOnExit)
            {
                PrevMethod.isOnExitCalling = true;
                var res = PrevMethod.Tick();
                PrevMethod.isOnExitCalling = false;
                if (res) return true;
                PrevMethod = null; // reset prev method
            }

            // Auto-reset if not ticked last frame
            if (autoResetByFrames && lastTickFrame + 1 != frame)
                Reset();
            lastTickFrame = frame;

            // if method is not started, start new Sequence
            if (!isStarted)
                StartNewSequence();

            // Run the method
            iStep = 0;
            executeStack.Push(CurrentData);
            try
            {
                CurrentData = this;
                lastResult = Method.Invoke();
            }
            finally
            {
                CurrentData = executeStack.Pop();
            }

            // reset if method is finished with False
            if (!lastResult)
                Reset();

            return lastResult;
        }

        /// <summary>
        /// Starts a new step sequence. Called internally when method first starts.
        /// </summary>
        private void StartNewSequence()
        {
            SequenceStep = 0;
            StartStepTime = StartMethodTime = Time.time;
            StartStepFrame = startMethodFrame = Time.frameCount;
            isStarted = true;
        }

        /// <summary>
        /// Resets the step execution state, so the method starts over on the next Tick.
        /// </summary>
        public void Reset()
        {
            // must restart sequence in next tick
            isStarted = false;
        }

        /// <summary>
        /// Advances the internal step counter and returns true only for the current active step.
        /// Used to implement step-by-step logic over multiple frames.
        /// </summary>
        public bool Step()
        {
            iStep++;
            if (iStep > SequenceStep)
            {
                // start new step
                SequenceStep = iStep;
                StartStepTime = Time.time;
                StartStepFrame = Time.frameCount;
            }
            return iStep == SequenceStep;
        }

        /// <summary>
        /// Marks this method as having an OnExit handler. Returns true when this method is being exited.
        /// </summary>
        public bool OnExit()
        {
            hasOnExit = true;
            return isOnExitCalling;
        }
    }
}