using DG.Tweening;
using KE;
using System;
using System.Collections;
using System.Diagnostics;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadManager : NetworkBehaviour
{
    private const float LOAD_DELAY = 0.5f;

    public static LoadManager Instance { get; private set; }

    [SerializeField] private LoadScreen loadScreen;
    [SerializeField] private ConnectionScreen connectionScreen;
    [Space]
    [SerializeField] private bool singleplayerDebugMode = false;

    public Slider LoadProgressBar { get => loadScreen.ProgressBar; }

    private bool playerShutdownAwait = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        connectionScreen.SetScreenActive(false);
        loadScreen.SetScreenActive(false);
    }

    private void OnLoadSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                SetLoadScreenActive(true);
                loadScreen.ProgressBar.value = 0.1f;
                break;

            case SceneEventType.LoadComplete:
                loadScreen.ProgressBar.value = 0.5f;
                break;

            case SceneEventType.Synchronize:
                loadScreen.ProgressBar.value = 0.8f;
                break;

            case SceneEventType.LoadEventCompleted:
                loadScreen.ProgressBar.value = 1f;
                SetLoadScreenActive(false);

                if (playerShutdownAwait)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnLoadSceneEvent;
                    playerShutdownAwait = false;
                }

                break;
        }
    }

    #region Screens

    public void SetLoadScreenActive(bool active)
    {
        if (StartGameData.GameMode == Gamemode.Singleplayer)
        {
            ExecuteSetLoadScreenActive(active);
            return;
        }

        SetLoadScreenActive_HostAndClientsRpc(active);
    }

    private void ExecuteSetLoadScreenActive(bool active)
    {
        loadScreen.SetScreenActive(active);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetLoadScreenActive_HostAndClientsRpc(bool active)
    {
        ExecuteSetLoadScreenActive(active);
    }

    public void SetConnectionScreenActive(bool active)
    {
        connectionScreen.SetScreenActive(active);
    }

    public void ShowConnectionErrorMessage(string message)
    {
        connectionScreen.SetMessage(message);
    }

    #endregion

    #region Loader

    /// <summary>
    /// Загрузка уровней в уже запущенной игре (т.е. переход на другой левел)
    /// </summary>
    /// <param name="level"></param>
    [Rpc(SendTo.Server)]
    public void LoadLevel_ServerRpc(int level)
    {
        StartCoroutine(StartLoadLevel(level));
    }

    /// <summary>
    /// Начать одиночную игру
    /// </summary>
    public void StartSingleplayerGame()
    {
        StartCoroutine(LoadSingleplayerGame(singleplayerDebugMode));
    }

    /// <summary>
    /// Начать мультиплеер игру
    /// </summary>
    /// <param name="debug"></param>
    public void StartMultiplayerGame(bool debug = false)
    {
        StartCoroutine(LoadMultiplayerGame(debug));
    }

    public void LoadTutorial()
    {
        StartCoroutine(StartLoadTutorial());
    }

    public void RetryGame()
    {
        if (!IsServer)
            return;

        StartCoroutine(DoRetryGame());
    }

    /// <summary>
    /// Выход в меню
    /// </summary>
    public void ExitToMenu()
    {
        StartCoroutine(LoadMenu());
    }

    #endregion

    #region Ulitity Methods

    public void NetworkLoadScene(int sceneIndex, LoadSceneMode mode = LoadSceneMode.Single)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(sceneIndex);

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, mode);
    }

    private IEnumerator StartLoadLevel(int level)
    {
        PlayerUI.BlockMap = false;

        SetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.SetActiveAllPlayers(false);

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnLoadSceneEvent;

        NetworkLoadScene(level);
    }

    #endregion

    #region Load Coroutines

    private IEnumerator LoadMultiplayerGame(bool debug = false)
    {
        PlayerUI.BlockMap = false;

        SetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.DestroyAllPlayerObjects();

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnLoadSceneEvent;

        if (debug)
        {
            NetworkLoadScene(GameScenes.TEST);
        }
        else
        {
            NetworkLoadScene(GameScenes.LEVEL_1);
        }
    }

    private IEnumerator StartLoadTutorial()
    {
        StartGameData.GameMode = Gamemode.Singleplayer;
        PlayerUI.BlockMap = false;

        SetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.DestroyAllPlayerObjects();

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData("127.0.0.1", 7777);
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnLoadSceneEvent;

        NetworkLoadScene(GameScenes.TUTORIAL);
    }

    private IEnumerator LoadSingleplayerGame(bool debugMode = false)
    {
        PlayerUI.BlockMap = false;

        SetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.DestroyAllPlayerObjects();

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData("127.0.0.1", 7777);
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnLoadSceneEvent;

        if (debugMode)
        {
            NetworkLoadScene(GameScenes.TEST);
        }
        else
        {
            NetworkLoadScene(GameScenes.LEVEL_1);
        }
    }

    private IEnumerator LoadMenu()
    {
        PlayerUI.BlockMap = false;

        ExecuteSetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.DestroyAllPlayerObjects();

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        var oper = SceneManager.LoadSceneAsync(GameScenes.MAIN_MENU, LoadSceneMode.Single);
        
        while (!oper.isDone)
        {
            loadScreen.ProgressBar.value = oper.progress;

            yield return null;
        }

        KE.CameraController.LockCursor(false);
        ExecuteSetLoadScreenActive(false);
        StartGameData.GameMode = Gamemode.None;
    }

    private IEnumerator DoRetryGame()
    {
        PlayerUI.BlockMap = false;

        SetLoadScreenActive(true);

        Time.timeScale = 1f;

        PlayerManager.Instance.DestroyAllPlayerObjects();

        yield return new WaitForSecondsRealtime(LOAD_DELAY);

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnLoadSceneEvent;

        switch (SceneManager.GetActiveScene().buildIndex)
        {
            case GameScenes.TUTORIAL:
                NetworkLoadScene(GameScenes.TUTORIAL);
                break;

            case GameScenes.TEST:
                NetworkLoadScene(GameScenes.TEST);
                break;

            default:
                NetworkLoadScene(GameScenes.LEVEL_1);
                break;
        }
    }

    #endregion
}
