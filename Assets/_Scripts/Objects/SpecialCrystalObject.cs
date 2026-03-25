using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpecialCrystalObject : InteractableObject
{
    [Header("Actions")]
    [SerializeField] private float cooldown;
    [SerializeField] private float textShowTime;
    [Space(20f)]
    [Range(0f, 1f)] [SerializeField] private float speechChance;
    [SerializeField] private List<string> speeches;
    [Space(20f)]
    [Range(0f, 1f)] [SerializeField] private float blowUpChance;
    [SerializeField] private AttackDamageType explosionDamage;
    [SerializeField] private EnemyMeleeAttackCollider explosionCollider;
    [SerializeField] private AudioSource explosionSFXSource;

    private bool canDoAction = true;

    [Header("Text")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text textObject;

    protected override void Start()
    {
        base.Start();

        canvasGroup.alpha = 0f;
        explosionCollider.OnHit += ExplosionCollider_OnHit;
    }

    private void ExplosionCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(explosionDamage, null);
        player.CreateHitEffect(hitPos);
    }

    public override void Interact(GameObject sender)
    {
        if (!canDoAction)
            return;

        DoAction();
    }

    private void DoAction()
    {
        canDoAction = false;
        float randomValue = Random.value;

        if (randomValue >= 0 && randomValue < speechChance)
        {
            int randomIndex = Random.Range(0, speeches.Count + 1);
            textObject.text = speeches[randomIndex];
            ShowText(textShowTime);
        }
        else if (randomValue >= speechChance && randomValue < blowUpChance)
        {
            explosionSFXSource.Play();
            explosionCollider.StartAttackCheck();

            textObject.text = "БУ >:D";
            ShowText(textShowTime);
        }
        else
        {
            textObject.text = "...";
            ShowText(textShowTime);
        }

        StartCoroutine(ActionCooldown());
    }

    private IEnumerator ActionCooldown()
    {
        yield return new WaitForSeconds(cooldown);

        canDoAction = true;
    }

    private void ShowText(float timeToShow)
    {
        SetActiveCanvas(true);
        SetActiveCanvas(false, 0.5f, timeToShow);
    }

    private void SetActiveCanvas(bool active, float time = 0.5f, float delay = 0)
    {
        canvasGroup.DOFade(active ? 1 : 0, time).SetDelay(delay);
    }

    private void Explode()
    {

    }
}
