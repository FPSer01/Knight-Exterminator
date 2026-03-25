using System.Collections.Generic;
using UnityEngine;

public class LevelMap : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject regularRoomPrefab;
    [SerializeField] private GameObject specialRoomPrefab;
    [SerializeField] private GameObject merchantRoomPrefab;
    [SerializeField] private GameObject bossRoomPrefab;
    [SerializeField] private GameObject corridorPrefab;

    [Header("Settings")]
    [SerializeField] private Vector3 overallMapOffset;
    [Space]
    [SerializeField] private Vector3 topDirection;
    [SerializeField] private Vector3 rightDirection;
    [Space]
    [SerializeField] private Vector3 roomRotationOffset;
    [SerializeField] private Vector3 corridorRotationOffset;
    [SerializeField] private float mapCorridorWidth = 5f;
    [Space]
    [SerializeField] private bool showAllMap;

    private List<MapRoom> mapRooms = new();

    public void Generate2DMap(Room[,] rooms, Vector3 roomSize, float corridorLength)
    {
        GameObject root = new GameObject("Level Map");
        root.transform.position = transform.position;
        root.isStatic = true;

        Vector3 corridorOffsetWidth = Vector3.Scale(roomSize / 2 + Vector3.one * corridorLength / 2, rightDirection);
        Vector3 corridorOffsetLength = Vector3.Scale(roomSize / 2 + Vector3.one * corridorLength / 2, topDirection);

        mapRooms = new();

        for (int i = 0; i < rooms.GetLength(0); i++)
        {
            for (int j = 0; j < rooms.GetLength(1); j++)
            {
                Room room = rooms[i, j];

                if (room == null)
                    continue;

                GameObject createdRoom = CreateMapRoom(room, roomRotationOffset);
                createdRoom.transform.localScale = new Vector2(roomSize.x, roomSize.z);

                MapRoom mapRoom = createdRoom.GetComponent<MapRoom>();
                mapRoom.Index = new Vector2Int(i, j);
                mapRoom.Position = room.RealPosition + overallMapOffset;
                mapRoom.Room = room;

                List<GameObject> roomCorridors = new();
                Vector3 corridorPos = room.RealPosition;

                if (room.DoorBottom) // Вниз
                    roomCorridors.Add(CreateMapCorridor(corridorPos - corridorOffsetLength, -topDirection, corridorLength, createdRoom.transform, "Corridor Bottom"));

                if (room.DoorTop) // Вверх
                    roomCorridors.Add(CreateMapCorridor(corridorPos + corridorOffsetLength, topDirection, corridorLength, createdRoom.transform, "Corridor Top"));

                if (room.DoorLeft) // Влево
                    roomCorridors.Add(CreateMapCorridor(corridorPos - corridorOffsetWidth, -rightDirection, corridorLength, createdRoom.transform, "Corridor Left"));

                if (room.DoorRight) // Вправо
                    roomCorridors.Add(CreateMapCorridor(corridorPos + corridorOffsetWidth, rightDirection, corridorLength, createdRoom.transform, "Corridor Right"));

                mapRoom.Corridors = roomCorridors;

                // Показать или скрыть мини карту
                if (showAllMap || room.StartRoom)
                {
                    mapRoom.IsDiscovered = true;
                }
                else
                {
                    mapRoom.gameObject.SetActive(false);
                }

                mapRooms.Add(mapRoom);
                createdRoom.transform.SetParent(root.transform);
            }
        }
    }

    public void DiscoverRoom(Vector2Int roomIndex)
    {
        var findRoom = mapRooms.Find(room => room.Index == roomIndex);

        if (findRoom.IsDiscovered)
            return;

        findRoom.IsDiscovered = true;
        findRoom.gameObject.SetActive(true);
    }

    private GameObject CreateMapCorridor(Vector3 position, Vector3 direction, float length, Transform parent, string name = null)
    {
        GameObject corridorObj = Instantiate(corridorPrefab, position + overallMapOffset, Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(corridorRotationOffset));

        if (name != null)
            corridorObj.name = name;

        corridorObj.isStatic = true;
        corridorObj.transform.localScale = new Vector3(length, mapCorridorWidth, 1);
        corridorObj.transform.SetParent(parent);

        return corridorObj;
    }

    private GameObject CreateMapRoom(Room room, Vector3 rotationOffset)
    {
        LevelRoomType type = DefineRoomType(room);
        GameObject roomPrefab;

        switch (type)
        {
            case LevelRoomType.Regular:
                roomPrefab = regularRoomPrefab;
                break;

            case LevelRoomType.Boss:
                roomPrefab = bossRoomPrefab;
                break;

            case LevelRoomType.Merchant:
                roomPrefab = merchantRoomPrefab;
                break;

            case LevelRoomType.Special:
                roomPrefab = specialRoomPrefab;
                break;

            default:
                roomPrefab = regularRoomPrefab;
                break;
        }

        GameObject roomObj = Instantiate(roomPrefab, room.RealPosition + overallMapOffset, Quaternion.identity * Quaternion.Euler(rotationOffset));

        return roomObj;
    }

    private LevelRoomType DefineRoomType(Room room)
    {
        if (room.BossRoom)
        {
            return LevelRoomType.Boss;
        }

        if (room.MerchantRoom)
        {
            return LevelRoomType.Merchant;
        }

        if (room.SpecialRoom)
        {
            return LevelRoomType.Special;
        }

        return LevelRoomType.Regular;
    }
}

