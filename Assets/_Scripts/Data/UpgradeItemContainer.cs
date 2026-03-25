using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeItemContainer", menuName = "Data/Upgrade Item Container")]
public class UpgradeItemContainer : ScriptableObject
{
    [SerializeField] private List<UpgradeItem> items;

    public List<UpgradeItem> Items { get => items; }

    public UpgradeItem GetRandomItem()
    {
        UpgradeItem item = items[Random.Range(0, items.Count)];

        return item;
    }
}
