using System.Collections.Generic;
using UnityEngine;

public class LevelPrimitive : MonoBehaviour
{
    [Header("Culling")]
    [SerializeField] private List<Renderer> cullRenderers = new();
    [SerializeField] private List<GameObject> cullObjects = new();

    public void SetActiveObjects(bool active)
    {
        foreach (var obj in cullObjects)
        {
            obj.SetActive(active);
        }
    }

    public void SetActiveRenderers(bool active)
    {
        foreach (var renderer in cullRenderers)
        {
            renderer.enabled = active;
        }
    }
}
