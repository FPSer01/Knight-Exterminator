using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Генерация уровня по seed.
/// Вызывается через LevelNetworkManager.BroadcastLevelSeedClientRpc —
/// один и тот же seed запускается на ВСЕХ клиентах, гарантируя
/// идентичную геометрию без сетевой синхронизации статичных объектов.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int levelWidth;
    [SerializeField] private int levelLength;
    [Space]
    [SerializeField] private int minRoomsCount;
    [SerializeField] private int maxRoomsCount;
    private int roomsCount;

    [Header("References")]
    [SerializeField] private LevelBuilder levelBuilder;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private Room[,] rooms;

    public Room[,] LevelRooms => rooms;
    public event Action OnLevelGenerated;

    /// <summary>
    /// Запускает детерминированную генерацию.
    /// Вызывается на каждом клиенте с одним и тем же seed из LevelNetworkManager.
    /// </summary>
    public void Generate(int seed)
    {
        // System.Random(seed) — детерминированный PRNG, не зависит от UnityEngine.Random
        System.Random rng = new System.Random(seed);

        roomsCount = rng.Next(minRoomsCount, maxRoomsCount + 1);

        if (roomsCount >= levelWidth * levelLength)
        {
            Debug.LogError($"[LevelGenerator] Out of bounds: {roomsCount} rooms for {levelWidth}x{levelLength} grid", this);
            return;
        }

        GeneratorUtility generator = new GeneratorUtility(rng);
        rooms = generator.GenerateLevelLayout(levelWidth, levelLength, roomsCount);

        Debug.Log($"[LevelGenerator] Seed={seed} | Rooms={roomsCount} | Grid={levelWidth}x{levelLength}");
        DebugDrawLevelMap();

        levelBuilder.ConstructLevel(rooms, rng);
        StartCoroutine(SetupNavMesh(0.5f));
    }

    private bool ValidateLevelBounds() => roomsCount < levelWidth * levelLength;

    private IEnumerator SetupNavMesh(float delay)
    {
        yield return new WaitForSeconds(delay);
        navMeshSurface.BuildNavMesh();
        OnLevelGenerated?.Invoke();
    }

    private void DebugDrawLevelMap()
    {
        if (rooms == null) return;

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < rooms.GetLength(0); i++)
        {
            for (int j = 0; j < rooms.GetLength(1); j++)
                sb.Append(rooms[i, j] == null ? "[_]" : "[1]");
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}
