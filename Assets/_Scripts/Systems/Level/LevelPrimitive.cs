using System.Collections.Generic;
using UnityEngine;

public class LevelPrimitive : MonoBehaviour
{
    [Header("Culling")]
    [SerializeField] private List<Renderer> cullRenderers = new();
    [SerializeField] private List<GameObject> cullObjects = new();

    protected virtual void Awake()
    {
        cullRenderers.RemoveAll(r => r == null);
        cullObjects.RemoveAll(o => o == null);
    }

    public void SetActiveObjects(bool active)
    {
        foreach (var obj in cullObjects)
        {
            if (obj == null)
                continue;

            obj.SetActive(active);
        }
    }

    public void SetActiveRenderers(bool active)
    {
        foreach (var renderer in cullRenderers)
        {
            if (renderer == null)
                continue;

            renderer.enabled = active;
        }
    }
}
