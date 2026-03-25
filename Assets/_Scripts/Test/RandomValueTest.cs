using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomValueTest : MonoBehaviour
{
    [SerializeField] private List<RandomTest> values;
    [SerializeField] private List<RandomStats> stats;
    [SerializeField] private float cycleTime;

    private ulong count;

    private void Start()
    {
        foreach (var value in values)
        {
            RandomStats stat = new RandomStats();
            stat.ValueName = value.ValueName;

            stats.Add(stat);
        }

        StartCoroutine(RandomCycle());
    }

    private IEnumerator RandomCycle()
    {
        while (true)
        {
            string valueName = GetRandomStuff();

            var stat = stats.Find(s => s.ValueName == valueName);
            stat.Count++;
            count++;

            foreach (var s in stats)
            {
                s.RealChance = (float)s.Count / count;
            }

            yield return new WaitForSecondsRealtime(cycleTime);
        }
    }

    private string GetRandomStuff()
    {
        float randomValue = Random.value;
        float topCeilChance = 0f;
        float bottomCeilChance = 0f;

        for (int i = 0; i < values.Count; i++)
        {
            bottomCeilChance = topCeilChance;
            topCeilChance += values[i].Chance;

            if (bottomCeilChance < randomValue && randomValue <= topCeilChance)
                return values[i].ValueName;
        }

        return null;
    }

    [Serializable]
    public class RandomStats 
    {
        public string ValueName;
        public int Count;
        [Range(0f, 1f)] public float RealChance;
    }

    [Serializable]
    public class RandomTest
    {
        public string ValueName;
        [Range(0f, 1f)] public float Chance;
    }
}
