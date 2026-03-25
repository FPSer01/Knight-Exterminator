using Unity.Netcode;
using UnityEngine;

public class NetworkGameData : NetworkBehaviour
{
    public static NetworkGameData Instance { get; private set; }

    public NetworkVariable<GameState> GameState = new(
        global::GameState.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            GameState.Value = default;
    }
}
