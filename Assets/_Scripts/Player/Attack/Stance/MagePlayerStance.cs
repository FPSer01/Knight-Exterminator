using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MagePlayerStance : PlayerStanceBase
{
    [Serializable]
    public struct StanceSettings
    {
        [Header("Stance")]
        public StanceType Type;

        [Header("Model Render")]
        public List<Renderer> Renderers;
        public List<Material> Materials;

        [Header("Attack Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private AttackDamageType attackDamage;
        [SerializeField] private string attackSFXTag;

        [Header("VFX")]
        public List<GameObject> VFX;

        public void SetActive(bool active, PlayerComponents components)
        {
            if (Renderers.Count != Materials.Count || components == null)
            {
                Debug.LogError("Invalid visual settings");
                return;
            }

            if (active)
            {
                for (int i = 0; i < Renderers.Count; i++)
                {
                    Renderers[i].sharedMaterial = Materials[i];
                }

                PlayerAttackRange attack = components.Attack as PlayerAttackRange;
                attack.AttackDamage = attackDamage;
                attack.CustomAttackSFX = attackSFXTag;
                attack.ProjectilePrefab = projectilePrefab;
            }

            foreach (var vfxObject in VFX)
            {
                vfxObject.SetActive(active);
            }
        }
    }

    [Header("Stance Specific")]
    [SerializeField] private List<StanceSettings> settings;

    [Header("Frost Stance")]
    [SerializeField] private PlayerAttackMultiCollider frostAttackCollider;
    [SerializeField] private float frostAttackMult = 2f;
    [SerializeField] private float frostAttackDelay;

    [Header("Pyro Stance")]
    [SerializeField] private GameObject pyroProjectilePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform pyroDefaultPoint;
    [SerializeField] private float pyroProjectileTime;
    [SerializeField] private int pathPoints;
    [Space]
    [SerializeField] private GameObject meteorsPrefab;
    [SerializeField] private float pyroCheckGroundLength;
    [SerializeField] private LayerMask pyroCheckGroundMask;
    [Space]
    [SerializeField] private float pyroAttackMult = 2f;
    [SerializeField] private float pyroAttackDelay;
    [SerializeField] private float pyroAttackAnimationDelay;

    [Header("Electro Stance")]
    [SerializeField] private PlayerAttackMultiCollider electroAttackCollider;
    [SerializeField] private float electroAttackMult = 2f;
    [Range(0f, 1f)] [SerializeField] private float electroDamageTakenMult = 0;
    [SerializeField] private float electroAttackBeforeDelay;
    [SerializeField] private float electroAttackAfterDelay;

    [Header("Objects")]
    [SerializeField] private GameObject mageStaffObject;

    private PlayerAttackRange attackRange => playerComponents.Attack as PlayerAttackRange;

    public override float StanceDamageMult { get => stanceDamageMult; set => SetMageStanceDamageMult(value); }

    #region Base Stance Methods

    public override void ExecuteSetStance(StanceType type)
    {
        base.ExecuteSetStance(type);

        SetVisual(type, playerComponents);

        frostAttackCollider.OnHit += FrostAttackCollider_OnHit;
        frostAttackCollider.SetColliders(false);

        electroAttackCollider.OnHit += ElectroAttackCollider_OnHit;
        electroAttackCollider.SetColliders(false);
    }

    public override void ResetSkillState(bool displayMessage = true)
    {
        base.ResetSkillState(displayMessage);

        sfxController.StopCustomSFX("Electro Stance");

        SetStaffActive(true);
        SetBlockAnimation(false);
    }

    protected override void ActivateStanceSkill()
    {
        base.ActivateStanceSkill();

        switch (currentStance.Type)
        {
            case StanceType.Frost:
                StartCoroutine(DoFrostAttack());
                break;

            case StanceType.Pyro:
                StartCoroutine(DoPyroAttack());
                break;

            case StanceType.Electric:
                StartCoroutine(DoElectroAttack());
                break;

            default:
                Debug.LogError($"Нет заданной стойки у {nameof(MagePlayerStance)}", this);
                break;
        }
    }

    #endregion

    #region Mage Stance Methods

    private void SetBlockAnimation(bool block)
    {
        animator.SetBool("Block Skill Animation", block);
    }

    public void SetVisual(StanceType stance, PlayerComponents components)
    {
        foreach (var setting in settings)
        {
            setting.SetActive(stance == setting.Type, components);
        }
    }

    private void SetMageStanceDamageMult(float newMultValue)
    {
        switch (currentStance.Type)
        {
            case StanceType.Frost:
                frostAttackMult = newMultValue;
                stanceDamageMult = frostAttackMult;
                break;

            case StanceType.Pyro:
                pyroAttackMult = newMultValue;
                stanceDamageMult = pyroAttackMult;
                break;

            case StanceType.Electric:
                electroAttackMult = newMultValue;
                stanceDamageMult = electroAttackMult;
                break;

            default:
                Debug.LogError($"Нет заданной стойки у {nameof(MagePlayerStance)}", this);
                break;
        }
    }

    #endregion

    #region Frost Stance

    private IEnumerator DoFrostAttack()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerComponents.ActivateRig(false);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);
        SetStaffActive(false);

        yield return new WaitForSeconds(frostAttackDelay);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);
        frostAttackCollider.CheckForHit();
        sfxController.PlayCustomSFX("Frost Stance");

        yield return new WaitForSeconds(currentStance.Duration);

        skillActive = false;
        ActivateStanceAnimation(false);
        playerComponents.ActivateRig(true);
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        SetStaffActive(true);

        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private void FrostAttackCollider_OnHit(EntityHealth enemy, HitTransform hitPos)
    {
        AttackDamageType frostAttackDamage = playerAttack.AttackDamage.GetMultDamage(frostAttackMult);

        float enemyDamageTaken = enemy.TakeDamage(frostAttackDamage, playerComponents.Health);
        enemy.CreateHitEffect(hitPos);

        playerAttack.TryVampireHeal(enemyDamageTaken);
    }

    #endregion

    #region Pyro Stance

    private IEnumerator DoPyroAttack()
    {
        if (!playerMovement.OnGround || attackRange == null)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerComponents.ActivateRig(false);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);
        SetStaffActive(false);

        yield return new WaitForSeconds(pyroAttackDelay);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);

        Transform target = attackRange.GetCurrentTarget();
        NetworkObjectReference targetRef = default;

        if (target != null)
        {
            if (target.TryGetComponent(out NetworkObject targetNetObj))
            {
                targetRef = new(targetNetObj);
            }
        }

        SpawnRealPyroProjectile(target);
        SpawnDummyPyroProjectile_NotOwnerRpc(targetRef);

        sfxController.PlayCustomSFX("Pyro Attack");

        yield return new WaitForSeconds(pyroAttackAnimationDelay);

        ActivateStanceAnimation(false);
        playerComponents.ActivateRig(true);
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        SetStaffActive(true);

        yield return new WaitForSeconds(currentStance.Duration - pyroAttackAnimationDelay);

        skillActive = false;

        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private PyroStanceProjectile SpawnPyroProjectile(Transform target)
    {
        var projectileObject = Instantiate(pyroProjectilePrefab, spawnPoint.position, spawnPoint.rotation);
        PyroStanceProjectile pyroProjectile = projectileObject.GetComponent<PyroStanceProjectile>();

        Vector3 endPoint = pyroDefaultPoint.position;

        if (target != null)
        {
            endPoint = target.position + Vector3.up * 4f;
        }

        pyroProjectile.SetPath(endPoint, pyroProjectileTime, pathPoints);

        return pyroProjectile;
    }

    private void SpawnRealPyroProjectile(Transform target)
    {
        var projectile = SpawnPyroProjectile(target);
        projectile.OnPathEnded += PyroProjectile_OnPathEnded;
    }

    [Rpc(SendTo.NotOwner)]
    private void SpawnDummyPyroProjectile_NotOwnerRpc(NetworkObjectReference targetRef)
    {
        Transform target = null;

        if (targetRef.TryGet(out NetworkObject netObj))
            target = netObj.transform;

        var projectile = SpawnPyroProjectile(target);
        projectile.OnPathEnded += DummyPyroProjectile_OnPathEnded;
    }

    private void PyroProjectile_OnPathEnded(Vector3 endPoint)
    {
        Vector3 spawnPosition = transform.position;

        if (Physics.Raycast(endPoint, Vector3.down, out RaycastHit hit, pyroCheckGroundLength, pyroCheckGroundMask))
        {
            spawnPosition = hit.point;

            var meteorsObj = Instantiate(meteorsPrefab, spawnPosition, Quaternion.identity);
            var meteors = meteorsObj.GetComponent<PlayerPyroMeteors>();

            meteors.OnHit += Meteors_OnHit;
            meteors.SetMeteorsTime(currentStance.Duration);
        }
    }

    private void DummyPyroProjectile_OnPathEnded(Vector3 endPoint)
    {
        Vector3 spawnPosition = transform.position;

        if (Physics.Raycast(endPoint, Vector3.down, out RaycastHit hit, pyroCheckGroundLength, pyroCheckGroundMask))
        {
            spawnPosition = hit.point;

            var meteorsObj = Instantiate(meteorsPrefab, spawnPosition, Quaternion.identity);
            meteorsObj.GetComponent<PlayerPyroMeteors>().SetMeteorsTime(currentStance.Duration);
        }
    }

    private void Meteors_OnHit(EntityHealth enemy, HitTransform hitPos)
    {
        AttackDamageType meteorsAttackDamage = playerAttack.AttackDamage.GetMultDamage(pyroAttackMult);

        float enemyDamageTaken = enemy.TakeDamage(meteorsAttackDamage, playerComponents.Health);
        enemy.CreateHitEffect(hitPos);

        playerAttack.TryVampireHeal(enemyDamageTaken);
    }

    #endregion

    #region Electro Stance

    private IEnumerator DoElectroAttack()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerComponents.ActivateRig(false);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);
        SetStaffActive(false);

        yield return new WaitForSeconds(electroAttackBeforeDelay);

        playerHealth.SetCutDamageMult(electroDamageTakenMult);
        playerState.DoStanceBarAnimation(0, currentStance.Duration + electroAttackAfterDelay);

        SetBlockAnimation(true);
        electroAttackCollider.CheckForHitLoop(true);
        sfxController.PlayCustomSFX("Electro Stance");

        yield return new WaitForSeconds(currentStance.Duration);

        skillActive = false;
        ActivateStanceAnimation(false);
        electroAttackCollider.CheckForHitLoop(false);
        sfxController.StopCustomSFX("Electro Stance");

        yield return new WaitForSeconds(electroAttackAfterDelay);

        playerHealth.SetCutDamageMult(0);
        SetBlockAnimation(false);
        playerComponents.ActivateRig(true);
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        SetStaffActive(true);

        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private void ElectroAttackCollider_OnHit(EntityHealth enemy, HitTransform hitPos)
    {
        AttackDamageType electroAttackDamage = playerAttack.AttackDamage.GetMultDamage(electroAttackMult);
        electroAttackDamage.IgnoreDefence = true;

        float enemyDamageTaken = enemy.TakeDamage(electroAttackDamage, playerComponents.Health);
        enemy.CreateHitEffect(hitPos);

        playerAttack.TryVampireHeal(enemyDamageTaken);
    }

    #endregion

    #region Effects

    private void SetStaffActive(bool active)
    {
        ExecuteSetStaffActive(active);
        SetStaffActive_NotOwnerRpc(active);
    }

    private void ExecuteSetStaffActive(bool active)
    {
        mageStaffObject.SetActive(active);
    }

    [Rpc(SendTo.NotOwner)]
    private void SetStaffActive_NotOwnerRpc(bool active)
    {
        ExecuteSetStaffActive(active);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (pyroDefaultPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pyroDefaultPoint.position, 0.1f);
        }
    }
}
