using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyDeathEffect : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer enemyModel;
    [SerializeField] private EntityHealth enemyHealth;
    [SerializeField] private GameObject additionalVFX;

    [Header("Settings")]
    [SerializeField] private Transform bodyPartsContainer;
    [SerializeField] private List<Rigidbody> bodyParts;
    [SerializeField] private List<Renderer> partsToHide;
    [Space]
    [SerializeField] private Transform forcePoint;
    [SerializeField] private float minForce;
    [SerializeField] private float maxForce;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float cleanTime;

    private void Start()
    {
        enemyHealth.OnDeath += PlayDeathEffect;
    }

    private void PlayDeathEffect()
    {
        if (additionalVFX != null)
        {
            Instantiate(additionalVFX, forcePoint.position, Quaternion.identity);
        }

        bodyPartsContainer.SetParent(null);
        Destroy(bodyPartsContainer.gameObject, cleanTime);

        partsToHide.ForEach((renderer) => renderer.enabled = false);

        bodyParts.ForEach((part) =>
        {
            part.gameObject.SetActive(true);
            DoForceOnPart(part);
            Destroy(part.gameObject, cleanTime);
        });
    }

    private void DoForceOnPart(Rigidbody part)
    {
        float force = Random.Range(minForce, maxForce);
        part.AddExplosionForce(force, forcePoint.position, explosionRadius, 0, ForceMode.VelocityChange);
    }

    public void CreateBakedMeshObject()
    {
        Mesh bakedMesh = new Mesh();
        enemyModel.BakeMesh(bakedMesh);

        GameObject bakedMeshObject = new GameObject("Enemy Baked Mesh");
        bakedMeshObject.transform.parent = transform;
        bakedMeshObject.transform.localPosition = enemyModel.transform.localPosition;
        bakedMeshObject.transform.localRotation = enemyModel.transform.localRotation;

        MeshFilter bakedMeshFilter = bakedMeshObject.AddComponent<MeshFilter>();
        bakedMeshFilter.mesh = bakedMesh;

        MeshRenderer bakedMeshRenderer = bakedMeshObject.AddComponent<MeshRenderer>();
        bakedMeshRenderer.sharedMaterials = enemyModel.sharedMaterials;
    }
}
