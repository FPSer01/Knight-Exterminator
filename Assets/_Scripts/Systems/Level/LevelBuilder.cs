using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private string levelName;
    [SerializeField] private float nameDelay = 1.5f;
    [SerializeField] private float nameDuration = 3f;

    [Header("Level Build Objects")]
    [SerializeField] private Vector3 roomSize;
    [SerializeField] private GameObject startRoomPrefab;
    [Space]
    [SerializeField] private List<GameObject> regularRoomsPrefabs = new();
    [SerializeField] private List<GameObject> specialRoomsPrefabs = new();
    [SerializeField] private List<GameObject> bossRoomPrefabs = new();
    [SerializeField] private List<GameObject> merchantRoomPrefabs = new();
    [Space]
    [SerializeField] private List<GameObject> corridorPrefabs = new();

    [Header("Settings")]
    [SerializeField] private Vector3 startPos;
    [Space]
    [SerializeField] private Vector3 topDirection = Vector3.right;
    [SerializeField] private Vector3 rightDirection = -Vector3.forward;
    [Space]
    [SerializeField] private bool dublicateCorridors = false;
    [SerializeField] private Vector3 roomRotationOffset;
    [SerializeField] private Vector3 corridorPositionOffset;
    [SerializeField] private Vector3 corridorRotationOffset;

    [Header("Optimizations")]
    [SerializeField] private bool enableRenderDistance = true;
    [SerializeField] private int roomsRender = 2;

    [Header("Mini Map")]
    [SerializeField] private LevelMap levelMap;

    private System.Random rng;
    private List<Vector3> corridorPositions = new();
    private List<Vector3> roomsPositions = new();
    private List<Vector2Int> edgeRoomsIndexPositions = new();

    private Vector2Int startRoomIndexPosition;
    private Vector2Int bossRoomIndexPosition;
    private Vector2Int merchantRoomIndexPosition;

    private Vector3 playerSpawnPosition;

    private void OnValidate()
    {
        if (startRoomPrefab != null)
            roomSize = startRoomPrefab.GetComponent<RoomBehaviour>().GetRoomSize();
    }

    /// <summary>
    /// Точка входа из LevelGenerator.Generate.
    /// rng передаётся снаружи, уже инициализированный тем же seed.
    /// </summary>
    public void ConstructLevel(Room[,] rooms, System.Random rng)
    {
        this.rng = rng;

        FindEdgeRooms(rooms);

        GameObject levelRoot = new GameObject("Level");

        LevelChunksController chunksController = levelRoot.AddComponent<LevelChunksController>();
        chunksController.enabled = false;
        chunksController.EnableCulling = enableRenderDistance;
        chunksController.RenderChunksDistance = roomsRender;

        var corridorPrefab = corridorPrefabs[0];
        CorridorBehaviour corridor = corridorPrefab.GetComponent<CorridorBehaviour>();
        float corridorLength = corridor.GetLength();

        Vector3 currentPos = startPos;
        Vector3 corridorOffsetWidth  = Vector3.Scale(roomSize / 2 + Vector3.one * corridorLength / 2, rightDirection);
        Vector3 corridorOffsetLength = Vector3.Scale(roomSize / 2 + Vector3.one * corridorLength / 2, topDirection);

        for (int i = 0; i < rooms.GetLength(0); i++)
        {
            for (int j = 0; j < rooms.GetLength(1); j++)
            {
                Room room = rooms[i, j];
                Vector2Int roomIndex = new(i, j);
                float stepX = dublicateCorridors ? corridorLength * 2 + roomSize.x : corridorLength + roomSize.x;

                if (room == null)
                {
                    currentPos.x += stepX;
                    continue;
                }

                // Создание комнаты
                GameObject createdRoom = CreateRoom(currentPos, room, i, j);
                room.RealPosition = currentPos;

                RoomBehaviour roomBehaviour = createdRoom.GetComponent<RoomBehaviour>();

                // Индекс нужен для ClientRpc из LevelNetworkManager
                roomBehaviour.SetRoomIndex(roomIndex);

                // Регистрируем в LevelNetworkManager — все клиенты, не только сервер
                if (LevelManager.Instance != null)
                    LevelManager.Instance.RegisterRoom(roomIndex, roomBehaviour);

                if (room.StartRoom)
                    playerSpawnPosition = roomBehaviour.TeleportPoint.position;

                room.RealRoom = roomBehaviour;
                roomsPositions.Add(currentPos);

                // Создание коридоров
                Vector3 corridorPos = currentPos + corridorPositionOffset;
                List<CorridorBehaviour> roomCorridors = new();

                if (room.DoorBottom) roomCorridors.Add(CreateCorridor(corridorPos - corridorOffsetLength, -topDirection,   levelRoot, "Corridor Bottom"));
                if (room.DoorTop)    roomCorridors.Add(CreateCorridor(corridorPos + corridorOffsetLength,  topDirection,   levelRoot, "Corridor Top"));
                if (room.DoorLeft)   roomCorridors.Add(CreateCorridor(corridorPos - corridorOffsetWidth,  -rightDirection, levelRoot, "Corridor Left"));
                if (room.DoorRight)  roomCorridors.Add(CreateCorridor(corridorPos + corridorOffsetWidth,   rightDirection, levelRoot, "Corridor Right"));

                // Добавляем чанки комнаты и коридоров
                List<LevelPrimitive> primitives = new() { roomBehaviour };
                primitives.AddRange(roomCorridors);
                chunksController.CreateChunk(roomIndex, primitives, currentPos);

                // Перемещаем шаг дальше
                currentPos.x += stepX;
                createdRoom.transform.SetParent(levelRoot.transform);
            }

            currentPos.x  = startPos.x;
            currentPos.z -= dublicateCorridors ? corridorLength * 2 + roomSize.z : corridorLength + roomSize.z;
        }

        levelMap.Generate2DMap(rooms, roomSize, corridorLength);
        LevelManager.Instance.ForceSetCurrentRoom(startRoomIndexPosition);

        StartCoroutine(WaitForPlayerSpawner());
        chunksController.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers: комнаты
    // ─────────────────────────────────────────────────────────────

    private void FindEdgeRooms(Room[,] rooms)
    {
        edgeRoomsIndexPositions = new();

        for (int i = 0; i < rooms.GetLength(0); i++)
        {
            for (int j = 0; j < rooms.GetLength(1); j++)
            {
                Room room = rooms[i, j];
                if (room == null) continue;

                int neighbors = 0;
                if (room.StartRoom) startRoomIndexPosition = new Vector2Int(i, j);
                if (room.DoorTop)    neighbors++;
                if (room.DoorBottom) neighbors++;
                if (room.DoorLeft)   neighbors++;
                if (room.DoorRight)  neighbors++;

                if (neighbors == 1 && !room.StartRoom)
                    edgeRoomsIndexPositions.Add(new Vector2Int(i, j));
            }
        }

        // Самая далёкая крайняя комната → боссовая
        if (edgeRoomsIndexPositions.Count > 0)
        {
            float maxDistance = 0;
            foreach (var pos in edgeRoomsIndexPositions)
            {
                float dist = Vector2Int.Distance(pos, startRoomIndexPosition);
                if (dist >= maxDistance) { maxDistance = dist; bossRoomIndexPosition = pos; }
            }
            edgeRoomsIndexPositions.Remove(bossRoomIndexPosition);
        }

        // Случайная крайняя комната → торговец (через seeded rng)
        if (edgeRoomsIndexPositions.Count > 0)
        {
            int idx = rng.Next(0, edgeRoomsIndexPositions.Count);
            merchantRoomIndexPosition = edgeRoomsIndexPositions[idx];
            edgeRoomsIndexPositions.Remove(merchantRoomIndexPosition);
        }
    }

    private GameObject CreateRoom(Vector3 position, Room room, int index_X, int index_Y)
    {
        Vector2Int roomIndex = new(index_X, index_Y);
        GameObject roomPrefab;

        if (edgeRoomsIndexPositions.Count > 0)
        {
            LevelRoomType roomType = DefineRoomType(roomIndex);
            switch (roomType)
            {
                case LevelRoomType.Regular:
                    roomPrefab = regularRoomsPrefabs[rng.Next(0, regularRoomsPrefabs.Count)];
                    break;
                case LevelRoomType.Start:
                    roomPrefab = startRoomPrefab;
                    break;
                case LevelRoomType.Boss:
                    roomPrefab = bossRoomPrefabs[rng.Next(0, bossRoomPrefabs.Count)];
                    room.BossRoom = true;
                    break;
                case LevelRoomType.Merchant:
                    roomPrefab = merchantRoomPrefabs[rng.Next(0, merchantRoomPrefabs.Count)];
                    room.MerchantRoom = true;
                    break;
                case LevelRoomType.Special:
                    roomPrefab = specialRoomsPrefabs[rng.Next(0, specialRoomsPrefabs.Count)];
                    room.SpecialRoom = true;
                    break;
                default:
                    roomPrefab = regularRoomsPrefabs[rng.Next(0, regularRoomsPrefabs.Count)];
                    break;
            }
        }
        else
        {
            roomPrefab = regularRoomsPrefabs[rng.Next(0, regularRoomsPrefabs.Count)];
        }

        GameObject createdRoom = Instantiate(roomPrefab, position, Quaternion.Euler(roomRotationOffset));
        createdRoom.name = $"Room [{index_X}, {index_Y}]";

        RoomBehaviour roomBehaviour = createdRoom.GetComponent<RoomBehaviour>();
        roomBehaviour.PlaceDoorWays(room.DoorTop, room.DoorBottom, room.DoorRight, room.DoorLeft);

        return createdRoom;
    }

    private LevelRoomType DefineRoomType(Vector2Int roomIndex)
    {
        if (roomIndex == bossRoomIndexPosition)     return LevelRoomType.Boss;
        if (roomIndex == startRoomIndexPosition)    return LevelRoomType.Start;
        if (roomIndex == merchantRoomIndexPosition) return LevelRoomType.Merchant;
        if (edgeRoomsIndexPositions.Contains(roomIndex)) return LevelRoomType.Special;
        return LevelRoomType.Regular;
    }

    private CorridorBehaviour CreateCorridor(Vector3 position, Vector3 direction, GameObject parent, string name = null)
    {
        if (corridorPositions.Contains(position)) return null;

        int idx = rng.Next(0, corridorPrefabs.Count);
        GameObject corridorObj = Instantiate(
            corridorPrefabs[idx],
            position,
            Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(corridorRotationOffset));

        corridorPositions.Add(position);
        corridorObj.isStatic = true;
        if (name != null) corridorObj.name = name;
        corridorObj.transform.SetParent(parent.transform);

        return corridorObj.GetComponent<CorridorBehaviour>();
    }

    private IEnumerator WaitForPlayerSpawner()
    {
        while (PlayerManager.Instance == null)
        {
            yield return new WaitForEndOfFrame();
        }

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerManager.Instance.SpawnPlayer(clientId, playerSpawnPosition);
        }

        ShowLevelName_AllRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowLevelName_AllRpc()
    {
        PlayerComponents playerComponents = null;

        foreach (var netobj in NetworkManager.Singleton.LocalClient.OwnedObjects)
        {
            playerComponents = netobj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        playerComponents.UI.LevelNameUI.ShowLevelLabel(levelName, nameDelay, nameDuration);
    }
}
