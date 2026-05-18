using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    public ShopItem[] shopItems;

    private void Start()
    {
        InitShop();
    }

    public void InitShop()
    {
        if (shopItems == null || shopItems.Length == 0) return;

        // 가능한 아이템 종류
        List<ShopItem.ItemType> availableTypes = new List<ShopItem.ItemType>
        {
            ShopItem.ItemType.NormalDNA,
            ShopItem.ItemType.RareDNA,
            ShopItem.ItemType.Potion
        };

        // 각 슬롯마다 무작위 배정
        for (int i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i] != null)
            {
                ShopItem.ItemType randomType = availableTypes[Random.Range(0, availableTypes.Count)];
                shopItems[i].Init(randomType);
            }
        }
    }

    public System.Action OnInteracted;

    public void NotifyItemPurchased(ShopItem item)
    {
        OnInteracted?.Invoke();
    }
}
