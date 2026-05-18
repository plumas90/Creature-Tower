using UnityEngine;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public enum ItemType
    {
        NormalDNA,
        RareDNA,
        Potion
    }

    public ItemType itemType;
    public int price;
    public float potionHealPercent; // 0.1, 0.25, 0.5, 1.0

    [Header("UI")]
    public TextMeshPro priceText;
    public SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    public Sprite normalDNASprite;
    public Sprite rareDNASprite;
    public Sprite potion10Sprite;
    public Sprite potion25Sprite;
    public Sprite potion50Sprite;
    public Sprite potion100Sprite;

    private bool isPurchased = false;

    public void Init(ItemType type)
    {
        itemType = type;
        isPurchased = false;
        gameObject.SetActive(true);

        switch (itemType)
        {
            case ItemType.NormalDNA:
                price = Random.Range(100, 201); // 150 +/- 50
                if (spriteRenderer != null) spriteRenderer.sprite = normalDNASprite;
                break;
            case ItemType.RareDNA:
                price = Random.Range(300, 701); // 500 +/- 200
                if (spriteRenderer != null) spriteRenderer.sprite = rareDNASprite;
                break;
            case ItemType.Potion:
                int rand = Random.Range(0, 100);
                if (rand < 1)
                {
                    potionHealPercent = 1.0f;
                    if (spriteRenderer != null) spriteRenderer.sprite = potion100Sprite;
                }
                else if (rand < 6)
                {
                    potionHealPercent = 0.5f;
                    if (spriteRenderer != null) spriteRenderer.sprite = potion50Sprite;
                }
                else if (rand < 30)
                {
                    potionHealPercent = 0.25f;
                    if (spriteRenderer != null) spriteRenderer.sprite = potion25Sprite;
                }
                else
                {
                    potionHealPercent = 0.1f;
                    if (spriteRenderer != null) spriteRenderer.sprite = potion10Sprite;
                }
                
                int basePricePer10 = Random.Range(20, 31); // 25 +/- 5
                price = Mathf.RoundToInt(basePricePer10 * (potionHealPercent / 0.1f));
                break;
        }

        if (priceText != null)
        {
            priceText.text = price.ToString() + " G";
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPurchased) return;
        
        PlayerStatControl playerStat = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStat == null) return;

        // 골드 확인 및 차감
        if (GameManager.Instance != null && GameManager.Instance.TrySpendGold(price))
        {
            isPurchased = true;

            ShopController controller = GetComponentInParent<ShopController>();
            if (controller != null)
            {
                controller.NotifyItemPurchased(this);
            }

            ApplyEffect(playerStat);
            gameObject.SetActive(false); // 구매 후 사라짐
        }
        else
        {
            // 골드 부족 시 피드백이 필요하다면 여기에 추가
            Debug.Log("[ShopItem] Not enough gold!");
        }
    }

    private void ApplyEffect(PlayerStatControl playerStat)
    {
        switch (itemType)
        {
            case ItemType.NormalDNA:
                if (ResultManager.Instance != null)
                {
                    ResultManager.Instance.OpenSpecialResult(playerStat.gameObject, false);
                }
                break;
            case ItemType.RareDNA:
                if (ResultManager.Instance != null)
                {
                    ResultManager.Instance.OpenSpecialResult(playerStat.gameObject, true);
                }
                break;
            case ItemType.Potion:
                float healAmount = playerStat.HP.total * potionHealPercent;
                playerStat.HPadd(healAmount);
                break;
        }
    }
}
