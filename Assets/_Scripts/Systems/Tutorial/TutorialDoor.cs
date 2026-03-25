using UnityEngine;

public class TutorialDoor : MonoBehaviour
{
    [SerializeField] private GameObject door;

    private void Start()
    {
        door.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        door.SetActive(true);
    }
}
