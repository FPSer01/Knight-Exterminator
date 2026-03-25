using TMPro;
using UnityEditor;
using UnityEngine;

public class VersionController : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    private void Awake()
    {
        versionText.text = $"Version: {Application.version}";
    }
}
