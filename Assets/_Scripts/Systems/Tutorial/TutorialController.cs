using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [SerializeField] private PlayerUI playerUI;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PlayerUI.BlockMap = true;
        playerUI.SetMiniMapVisible(false);
    }

    public void UnlockMap()
    {
        PlayerUI.BlockMap = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        UnlockMap();
    }
}
