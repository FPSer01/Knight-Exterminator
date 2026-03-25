using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Чистая логика генерации планировки уровня.
/// Полностью детерминирована: все случайные вызовы идут через System.Random(seed),
/// переданный извне. Одинаковый seed => одинаковый результат на всех клиентах.
/// </summary>
public class GeneratorUtility
{
    private readonly System.Random rng;
    private readonly List<Vector2Int> takenPositions = new();
    private readonly Vector2Int startPos = Vector2Int.zero;

    public GeneratorUtility(System.Random rng)
    {
        this.rng = rng;
    }

    private float RandomValue() => (float)rng.NextDouble();

    private int RandomRange(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);

    public Room[,] GenerateLevelLayout(int levelWidth, int levelLength, int roomsCount)
    {
        Room[,] rooms = new Room[levelWidth, levelLength];
        CreateRooms(rooms, levelWidth, levelLength, roomsCount);
        SetRoomDoors(rooms);
        return rooms;
    }

    private void CreateRooms(Room[,] rooms, int levelWidth, int levelLength, int roomsCount)
    {
        takenPositions.Insert(0, startPos);
        Vector2Int checkPos = new Vector2Int(levelWidth / 2, levelLength / 2);

        Room startRoom = new Room(checkPos) { StartRoom = true };
        rooms[levelWidth / 2, levelLength / 2] = startRoom;

        const float randomCompareStart = 0.2f;
        const float randomCompareEnd   = 0.01f;

        for (int i = 0; i < roomsCount - 1; i++)
        {
            float randomPerc    = i / ((float)roomsCount - 1);
            float randomCompare = Mathf.Lerp(randomCompareStart, randomCompareEnd, randomPerc);

            checkPos = NewPosition(levelWidth, levelLength);

            if (NumberOfNeighbors(checkPos, takenPositions) > 1 && RandomValue() > randomCompare)
            {
                int iterations = 0;
                do
                {
                    checkPos = SelectiveNewPosition(levelWidth, levelLength);
                    iterations++;
                }
                while (NumberOfNeighbors(checkPos, takenPositions) > 1 && iterations < 100);
            }

            rooms[checkPos.x + levelWidth / 2, checkPos.y + levelLength / 2] = new Room(checkPos);
            takenPositions.Insert(0, checkPos);
        }
    }

    private Vector2Int NewPosition(int width, int length)
    {
        Vector2Int checkingPos;
        int x, y;

        do
        {
            int index = RandomRange(0, takenPositions.Count);
            x = takenPositions[index].x;
            y = takenPositions[index].y;

            bool upDown  = RandomValue() < 0.5f;
            bool positive = RandomValue() < 0.5f;

            if (upDown) y += positive ? 1 : -1;
            else        x += positive ? 1 : -1;

            checkingPos = new Vector2Int(x, y);
        }
        while (takenPositions.Contains(checkingPos)
            || x >= width  / 2 || x < -width  / 2
            || y >= length / 2 || y < -length / 2);

        return checkingPos;
    }

    private Vector2Int SelectiveNewPosition(int width, int length)
    {
        Vector2Int checkingPos;
        int x, y;

        do
        {
            int inc   = 0;
            int index = 0;
            do
            {
                index = RandomRange(0, takenPositions.Count);
                inc++;
            }
            while (NumberOfNeighbors(takenPositions[index], takenPositions) > 1 && inc < 100);

            x = takenPositions[index].x;
            y = takenPositions[index].y;

            bool upDown   = RandomValue() < 0.5f;
            bool positive = RandomValue() < 0.5f;

            if (upDown) y += positive ? 1 : -1;
            else        x += positive ? 1 : -1;

            checkingPos = new Vector2Int(x, y);
        }
        while (takenPositions.Contains(checkingPos)
            || x >= width  / 2 || x < -width  / 2
            || y >= length / 2 || y < -length / 2);

        return checkingPos;
    }

    private int NumberOfNeighbors(Vector2Int pos, List<Vector2Int> used)
    {
        int ret = 0;
        if (used.Contains(pos + Vector2Int.right)) ret++;
        if (used.Contains(pos + Vector2Int.left))  ret++;
        if (used.Contains(pos + Vector2Int.up))    ret++;
        if (used.Contains(pos + Vector2Int.down))  ret++;
        return ret;
    }

    // ─────────────────────────────────────────────────────────────
    // Расстановка дверей
    // ─────────────────────────────────────────────────────────────

    private void SetRoomDoors(Room[,] rooms)
    {
        int w = rooms.GetLength(0);
        int h = rooms.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (rooms[x, y] == null) continue;

                rooms[x, y].DoorBottom = y > 0     && rooms[x, y - 1] != null;
                rooms[x, y].DoorTop    = y < h - 1 && rooms[x, y + 1] != null;
                rooms[x, y].DoorLeft   = x > 0     && rooms[x - 1, y] != null;
                rooms[x, y].DoorRight  = x < w - 1 && rooms[x + 1, y] != null;
            }
        }
    }
}
