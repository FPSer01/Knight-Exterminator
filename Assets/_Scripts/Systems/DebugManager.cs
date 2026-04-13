using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { private set; get; }
    

    [Header("Debug Regimes")]
    [SerializeField] private bool enableDebugMode;
    [Space]
    [SerializeField] private GameLevels singleplayerStartLevelDebug = GameLevels.Test;
    [SerializeField] private GameLevels multiplayerStartLevelDebug = GameLevels.Test;

    public bool EnableDebugMode 
    {
        get
        {
#if UNITY_EDITOR || DEBUG
            return enableDebugMode;
#else
            return false;
#endif
        }
    }

    public GameLevels SingleplayerStartLevel { get => singleplayerStartLevelDebug; }
    public GameLevels MultiplayerStartLevel { get => multiplayerStartLevelDebug; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }


}
