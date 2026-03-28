using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomBehaviour : LevelPrimitive
{
    [Header("Room Structure Objects")]
    [SerializeField] private GameObject wallTop;
    [SerializeField] private GameObject wallBottom;
    [SerializeField] private GameObject wallRight;
    [SerializeField] private GameObject wallLeft;
    [Space]
    [SerializeField] private GameObject doorWallTop;
    [SerializeField] private GameObject doorWallBottom;
    [SerializeField] private GameObject doorWallRight;
    [SerializeField] private GameObject doorWallLeft;

    [Header("Other Settings")]
    [SerializeField] private Vector3 customRoomSize;

    [Header("Trigger Zone")]
    [SerializeField] private BoxCollider triggerCollider;
    [Range(0f, 1f)][SerializeField] private float triggerSize;

    [Header("Doors")]
    [SerializeField] private bool simpleDoors;
    [SerializeField] private bool gateDoors;
    [SerializeField] private float doorsCloseDuration;
    [SerializeField] private List<DoorController> doors;

    [Header("Enemies")]
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private EnemySpawner spawner;

    [Header("Teleport")]
    [SerializeField] private Transform teleportPoint;

    private Vector2Int roomIndex;
    private bool entered = false;
    private bool cleared = false;

    public Transform TeleportPoint => teleportPoint;
    public BoxCollider TriggerCollider { get => triggerCollider; }
    public Vector2Int RoomIndex { get => roomIndex; }

    /// <summary>Срабатывает только когда локальный игрок вошёл в комнату.</summary>
    public event Action<ulong, Vector2Int> OnPlayerEnterRoom;

    /// <summary>Устанавливается из LevelBuilder после создания комнаты.</summary>
    public void SetRoomIndex(Vector2Int index) => roomIndex = index;

    public void PlaceDoorWays(bool top, bool bottom, bool right, bool left)
    {
        wallTop.SetActive(!top); wallBottom.SetActive(!bottom);
        wallRight.SetActive(!right); wallLeft.SetActive(!left);

        doorWallTop.SetActive(top); doorWallBottom.SetActive(bottom);
        doorWallRight.SetActive(right); doorWallLeft.SetActive(left);
    }

    public Vector3 GetRoomSize()
    {
        return customRoomSize;
    }

    public void SwitchWalls()
    {
        wallTop.SetActive(!wallTop.activeSelf); wallBottom.SetActive(!wallBottom.activeSelf);
        wallRight.SetActive(!wallRight.activeSelf); wallLeft.SetActive(!wallLeft.activeSelf);
        doorWallTop.SetActive(!doorWallTop.activeSelf); doorWallBottom.SetActive(!doorWallBottom.activeSelf);
        doorWallRight.SetActive(!doorWallRight.activeSelf); doorWallLeft.SetActive(!doorWallLeft.activeSelf);
    }

    public void OpenRoom(bool open)
    {
        if (simpleDoors)
            doors.ForEach(d => d.SetSimpleDoors(open, doorsCloseDuration));
        else if (gateDoors)
            doors.ForEach(d => d.SetGateDoors(open, doorsCloseDuration));
    }

    private void HandleAllEnemiesKilledOnServer()
    {
        if (spawner != null)
            spawner.OnAllEnemiesKilled -= HandleAllEnemiesKilledOnServer;

        LevelManager.Instance.EndRoomBattle_ServerRpc(roomIndex);
    }

    /// <summary>
    /// Запустить сражение локально
    /// </summary>
    /// <param name="summonEnemies"></param>
    public void StartBattle_Local(bool summonEnemies = true, bool checkForEntered = true)
    {
        if (entered && checkForEntered)
            return;

        entered = true;
        OpenRoom(false);
        LevelMusicController.Instance.SetBattleMusic(true);
        PlayerUI.BlockMap = true;

        if (NetworkManager.Singleton.IsServer && spawner != null && summonEnemies)
        {
            spawner.OnAllEnemiesKilled += HandleAllEnemiesKilledOnServer;
            spawner.SpawnEnemies();
        }
    }

    /// <summary>
    /// Остановить сражение локально
    /// </summary>
    public void EndBattle_Local(bool checkForCleared = true)
    {
        if (cleared && checkForCleared)
            return;

        cleared = true;
        OpenRoom(true);
        LevelMusicController.Instance.SetBattleMusic(false);
        PlayerUI.BlockMap = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerComponents components = other.GetComponentInParent<PlayerComponents>();

        if (spawnEnemies && !entered && components != null)
        {
            LevelManager.Instance.StartRoomBattle_ServerRpc(roomIndex);
            LevelManager.Instance.TeleportPlayers(components.OwnerClientId, other.transform.position, roomIndex);
        }
        else if (!spawnEnemies && !entered)
        {
            entered = true;
            cleared = true;
        }

        if (components != null)
            OnPlayerEnterRoom?.Invoke(components.OwnerClientId, roomIndex);
    }

    private void OnValidate()
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(
                customRoomSize.x / transform.localScale.x,
                customRoomSize.y / transform.localScale.y,
                customRoomSize.z / transform.localScale.z) * triggerSize;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerCollider != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, customRoomSize * triggerSize);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + Vector3.Scale(customRoomSize / 2, Vector3.up), customRoomSize);
    }
}
