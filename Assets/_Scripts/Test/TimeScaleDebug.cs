using UnityEngine;

public class TimeScaleDebug : MonoBehaviour
{
    [Range(0, 2f)]
    [SerializeField] float timeScale = 1f;

    private void Update()
    {
        Time.timeScale = timeScale;
    }
}
