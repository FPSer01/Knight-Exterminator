using DG.Tweening;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpiderEggSpawner : NetworkBehaviour, ISummonSpawnProvider
{
    [SerializeField] private Transform miniSpiderSpawnPoint;
    [SerializeField] private Collider spawnerCollider;
    [SerializeField] private ParticleSystem spawnVFX;
    [SerializeField] private AudioSource hatchSFXSource;
    [Space]
    [SerializeField] private float cooldown;
    [SerializeField] private Transform egg;
    [SerializeField] private Transform net;

    private Vector3 eggOriginalScale;
    private Vector3 netOriginalScale;

    private bool canSpawn = true;

    private void Start()
    {
        eggOriginalScale = egg.localScale;
        netOriginalScale = net.localScale;
    }

    public GameObject Summon(GameObject summonPrefab)
    {
        if (!canSpawn || !IsServer)
            return null;

        canSpawn = false;
        SetCollider_EveryoneRpc(false);
        PlayEffects_EveryoneRpc();

        egg.localScale = Vector3.zero;
        net.localScale = Vector3.zero;

        GameObject spider = Instantiate(summonPrefab, miniSpiderSpawnPoint.position, Quaternion.identity);
        NetworkObject netObj = spider.GetComponent<NetworkObject>();
        netObj.Spawn();

        StartCoroutine(WaitCooldown(cooldown));

        return spider;
    }

    private IEnumerator WaitCooldown(float cooldown)
    {
        egg.DOScale(eggOriginalScale, cooldown);
        net.DOScale(netOriginalScale, cooldown);

        yield return new WaitForSeconds(cooldown);

        canSpawn = true;
        SetCollider_EveryoneRpc(true);
    }

    [Rpc(SendTo.Everyone)]
    private void SetCollider_EveryoneRpc(bool active)
    {
        ExecuteSetCollider(active);
    }

    private void ExecuteSetCollider(bool active)
    {
        spawnerCollider.enabled = active;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayEffects_EveryoneRpc()
    {
        ExecutePlayEffects();
    }

    private void ExecutePlayEffects()
    {
        hatchSFXSource.Play();
        spawnVFX.Play();

        egg.localScale = Vector3.zero;
        net.localScale = Vector3.zero;
    }

}
