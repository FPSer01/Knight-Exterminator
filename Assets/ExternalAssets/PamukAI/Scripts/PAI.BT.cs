using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

namespace PamukAI
{
    public static partial class PAI
    {
        #region BT composite nodes

        public static bool SequenceTick(Method m0, Method m1) => Tick(m0) && Tick(m1);
        public static bool SequenceTick(Method m0, Method m1, Method m2) => Tick(m0) && Tick(m1) && Tick(m2);
        public static bool SequenceTick(Method m0, Method m1, Method m2, Method m3) => Tick(m0) && Tick(m1) && Tick(m2) && Tick(m3);
        public static bool SequenceTick(Method m0, Method m1, Method m2, Method m3, Method m4) => Tick(m0) && Tick(m1) && Tick(m2) && Tick(m3) && Tick(m4);
        public static bool SequenceTick(Method m0, Method m1, Method m2, Method m3, Method m4, Method m5) => Tick(m0) && Tick(m1) && Tick(m2) && Tick(m3) && Tick(m4) && Tick(m5);
        public static bool SequenceTick(params Method[] methods) => SequenceTick((IEnumerable<Method>)methods);

        /// <summary>
        /// Executes all methods (nodes) in order, stopping at the first one that returns false. 
        /// Returns true only if all methods return true.
        /// </summary>
        /// <see cref="https://docs.google.com/document/d/1w_rURLQYAgKpgYA1HnZSTh-SiHD08LdKzluA8Dh8ZjE/edit?tab=t.0#heading=h.w985gyifoxao"/>"/>
        public static bool SequenceTick(IEnumerable<Method> methods)
        {
            foreach (var m in methods)
                if (!Tick(m))
                    return false;

            return true;
        }

        public static bool SelectorTick(Method m0, Method m1) => Tick(m0) || Tick(m1);
        public static bool SelectorTick(Method m0, Method m1, Method m2) => Tick(m0) || Tick(m1) || Tick(m2);
        public static bool SelectorTick(Method m0, Method m1, Method m2, Method m3) => Tick(m0) || Tick(m1) || Tick(m2) || Tick(m3);
        public static bool SelectorTick(Method m0, Method m1, Method m2, Method m3, Method m4) => Tick(m0) || Tick(m1) || Tick(m2) || Tick(m3) || Tick(m4);
        public static bool SelectorTick(Method m0, Method m1, Method m2, Method m3, Method m4, Method m5) => Tick(m0) || Tick(m1) || Tick(m2) || Tick(m3) || Tick(m4) || Tick(m5);
        public static bool SelectorTick(params Method[] methods) => SelectorTick((IEnumerable<Method>)methods);

        /// <summary> 
        /// Executes all methods in order until one returns true. 
        /// Returns true immediately when a method succeeds, otherwise returns false if all fail. 
        /// </summary>
        /// <see cref="https://docs.google.com/document/d/1w_rURLQYAgKpgYA1HnZSTh-SiHD08LdKzluA8Dh8ZjE/edit?tab=t.0#heading=h.ngvyd4kt7500"/>
        public static bool SelectorTick(IEnumerable<Method> methods)
        {
            foreach (var m in methods)
                if (Tick(m))
                    return true;

            return false;
        }

        public static bool ParallelTick(Method m0, Method m1) => Tick(m0) | Tick(m1);
        public static bool ParallelTick(Method m0, Method m1, Method m2) => Tick(m0) | Tick(m1) | Tick(m2);
        public static bool ParallelTick(Method m0, Method m1, Method m2, Method m3) => Tick(m0) | Tick(m1) | Tick(m2) | Tick(m3);
        public static bool ParallelTick(Method m0, Method m1, Method m2, Method m3, Method m4) => Tick(m0) | Tick(m1) | Tick(m2) | Tick(m3) | Tick(m4);
        public static bool ParallelTick(Method m0, Method m1, Method m2, Method m3, Method m4, Method m5) => Tick(m0) | Tick(m1) | Tick(m2) | Tick(m3) | Tick(m4) | Tick(m5);
        public static bool ParallelTick(params Method[] methods) => ParallelTick((IEnumerable<Method>)methods);

        /// <summary>
        /// Executes all methods in the list in parallel (in a single frame). 
        /// Returns true if at least one of them returns true, otherwise returns false.
        /// </summary>
        /// <see cref="https://docs.google.com/document/d/1w_rURLQYAgKpgYA1HnZSTh-SiHD08LdKzluA8Dh8ZjE/edit?tab=t.0#heading=h.i879c2hknqbx"/>
        public static bool ParallelTick(IEnumerable<Method> methods)
        {
            var res = false;
            foreach (var m in methods)
                if (Tick(m))
                    res = true;

            return res;
        }

        //TODO: BT tree

        #endregion
    }
}