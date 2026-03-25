using System.Collections.Generic;
using UnityEngine;

public class CentipedeColliderController : MonoBehaviour
{
    [SerializeField] private List<SphereCollider> colliders;
    [SerializeField] private List<Transform> bones;

    private void OnValidate()
    {
        SetCollidersCenterToBones();
    }

    private void SetCollidersCenterToBones()
    {
        if (colliders.Count != bones.Count)
        {
            Debug.LogError("Неверное количество костей и коллайдеров для контроля", this);
            return;
        }

        for (int i = 0; i < colliders.Count; i++)
        {
            colliders[i].center = transform.InverseTransformPoint(bones[i].position);
        }
    }

    public void SetColliders(bool active)
    {
        colliders.ForEach(col => col.enabled = active);
    }

    private void Update()
    {
        SetCollidersCenterToBones();
    }
}
