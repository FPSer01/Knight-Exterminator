using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    public static partial class PAI
    {
        #region Scheduled methods

        readonly static List<(MethodData method, float time)> scheduleList = new();

        public static void Schedule(Method method, float delay)
        {
            if (method == null) return;
            var data = new MethodData(method);// create unique data for the method
            scheduleList.Add((data, Time.time + delay));
        }

        /// <summary> Executes methods sequentally </summary>
        public static void ScheduleChain(IEnumerable<Method> methods, float delay)
        {
            if (methods == null) return;

            var queue = GenericPool<Queue<MethodData>>.Get();
            foreach (var m in methods)
                queue.Enqueue(new MethodData(m));

            var data = new MethodData(ScheduledQueue);
            data.Data["queue"] = queue; // store queue in Data
            scheduleList.Add((data, Time.time + delay));

            static bool ScheduledQueue()
            {
                var queue = (Queue<MethodData>)Data["queue"];
                if (queue.Count == 0)
                {
                    GenericPool<Queue<MethodData>>.Release(queue);
                    return false;
                }

                if (!queue.Peek().Tick())
                    queue.Dequeue();

                return true;
            }
        }

        public static void ScheduledTick()
        {
            var time = Time.time;
            for (int i = scheduleList.Count - 1; i >= 0; i--)
            {
                if (scheduleList[i].time <= time)
                {
                    var data = scheduleList[i];
                    if (!data.method.Tick())
                        // if method is finished with False, remove it from schedule list
                        scheduleList.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}