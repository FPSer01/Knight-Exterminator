using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    public static partial class PAI
    {
        #region UtilityAI

        public static void Vote(Method method, float utility)
        {
            if (method == null) return;
            GetData(method).Utility = utility;
        }

        public static bool MaxUtilityTick(Method m0, Method m1) => MaxUtilityTick(new AList<Method> { m0, m1 });
        public static bool MaxUtilityTick(Method m0, Method m1, Method m2) => MaxUtilityTick(new AList<Method> { m0, m1, m2 });
        public static bool MaxUtilityTick(Method m0, Method m1, Method m2, Method m3) => MaxUtilityTick(new AList<Method> { m0, m1, m2, m3 });
        public static bool MaxUtilityTick(Method m0, Method m1, Method m2, Method m3, Method m4) => MaxUtilityTick(new AList<Method> { m0, m1, m2, m3, m4});
        public static bool MaxUtilityTick(Method m0, Method m1, Method m2, Method m3, Method m4, Method m5) => MaxUtilityTick(new AList<Method> { m0, m1, m2, m3, m4, m5 });
        public static bool MaxUtilityTick(params Method[] methods) => MaxUtilityTick((IEnumerable<Method>)methods);

        /// <summary> Find method with max utility and execute it </summary>
        public static bool MaxUtilityTick(IEnumerable<Method> methods)
        {
            var maxUtility = 0f;
            var bestMethod = (MethodData)null;
            foreach (var m in methods)
            {
                var data = GetData(m);
                if (data.Utility > maxUtility)
                { 
                    maxUtility = data.Utility; 
                    bestMethod = data; 
                }
            }

            return bestMethod != null && bestMethod.Tick();
        }

        /// <summary> Sort methods by utilities and execute in selector logic with falldown </summary>
        public static bool UtilitySelectorTick(params Method[] methods)
        {
            using (ListPool<MethodData>.Get(out var list))
            {
                for (int i = 0; i < methods.Length; i++)
                    list.Add(GetData(methods[i]));

                return UtilitySelectorTick(list);
            }
        }

        /// <summary> Sort methods by utilities and execute in selector logic with falldown </summary>
        public static bool UtilitySelectorTick(IList<MethodData> methods)
        {
            for (int i = 0; i < methods.Count; i++)
            {
                // Find the maximum element in the remaining part
                int maxIndex = i;
                var maxUtility = methods[i].Utility;

                for (int j = i + 1; j < methods.Count; j++)
                {
                    var utility = methods[j].Utility;
                    if (utility > maxUtility)
                    {
                        maxIndex = j;
                        maxUtility = utility;
                    }
                }

                if (maxUtility <= 0f)
                    break;

                if (methods[maxIndex].Tick())
                    return true;

                // Swap and process
                if (maxIndex != i)
                {
                    (methods[i], methods[maxIndex]) = (methods[maxIndex], methods[i]);
                }
            }

            return false;
        }

        #endregion
    }
}