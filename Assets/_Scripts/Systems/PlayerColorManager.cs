using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerColorManager : NetworkBehaviour
{
    public static PlayerColorManager Instance { private set; get; }

    private readonly Color[] playerColors =
    {
        Color.green,
        Color.blue,
        Color.red,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new (1, 0.5f, 0, 1), // Orange
        new (1, 0, 1, 1), // Purple
    };

    private Dictionary<ulong, int> assignedColors = new();
    private HashSet<int> usedIndexes = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public Color AssignPlayerColor(ulong clientId)
    {
        for (int i = 0; i < playerColors.Length; i++)
        {
            if (!usedIndexes.Contains(i))
            {
                usedIndexes.Add(i);
                assignedColors[clientId] = i;
                return playerColors[i];
            }
        }

        Color random = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        assignedColors[clientId] = -1;
        return random;
    }

    public void ReleaseColor(ulong clientId)
    {
        if (assignedColors.TryGetValue(clientId, out int idx))
        {
            usedIndexes.Remove(idx);
            assignedColors.Remove(clientId);
        }
    }
}
