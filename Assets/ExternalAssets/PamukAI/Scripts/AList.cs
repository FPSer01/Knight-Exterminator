using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PamukAI
{
    /// <summary> Auto release (after each tick) list from pool </summary>
    public struct AList<T> : IEnumerable<T>
    {
        private static readonly List<List<T>> pool = new List<List<T>>();
        static int lastFrame;
        static int index;

        List<T> items;
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

        public void Add(T v)
        {
            items ??= New();
            items.Add(v);
        }

        static List<T> New()
        {
            var frame = Time.frameCount;
            if (lastFrame < frame)
            {
                index = 0;
                lastFrame = frame;
                foreach (var list in pool)
                    list.Clear();
            }

            index++;
            while (pool.Count < index)
                pool.Add(new List<T>());

            return pool[index - 1];
        }
    }

    /// <summary> Auto release (after each tick) list pool </summary>
    public static class AListPool<T>
    {
        private static readonly List<List<T>> pool = new List<List<T>>();
        static int lastFrame;
        static int index;

        public static List<T> New()
        {
            var frame = Time.frameCount;
            if (lastFrame < frame)
            {
                index = 0;
                lastFrame = frame;
                foreach (var list in pool)
                    list.Clear();
            }

            index++;
            while (pool.Count < index)
                pool.Add(new List<T>());

            return pool[index - 1];
        }
    }
}