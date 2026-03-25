using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour, IInteractable
{
    [SerializeField] private string toolTipText;
    [SerializeField] private Outline outline;
    [Space]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<UpgradeItem> itemsCollection;
    [SerializeField] private bool spawnAllAtOnce;

    private void Start()
    {
        outline.enabled = false;
    }

    public string GetInteractionToolTip()
    {
        return toolTipText;
    }

    public void HighlightObject(bool highlight)
    {
        outline.enabled = highlight;
    }

    public void Interact(GameObject sender)
    {
        if (spawnAllAtOnce)
        {
            foreach (var item in itemsCollection)
            {
                ItemGenerator.Instance.SpawnItem(item, spawnPoint.position);
            }
        }
        else
        {
            UpgradeItem item = itemsCollection[Random.Range(0, itemsCollection.Count)];
            ItemGenerator.Instance.SpawnItem(item, spawnPoint.position);
        }

    }
}
