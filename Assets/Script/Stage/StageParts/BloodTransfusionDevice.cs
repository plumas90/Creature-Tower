using UnityEngine;

// 플레이어가 접촉하면 체력 10% 소모로 스탯 구매 UI를 연다.
// 같은 스테이지의 같은 장치는 취소 후 재진입해도 동일 목록을 유지한다.
// 다음 스테이지로 넘어가면 Stage.Init()에서 목록 캐시가 초기화된다.
public class BloodTransfusionDevice : MonoBehaviour
{
    private enum TransfusionCostType
    {
        Percent,
        Flat
    }

    [Header("Transfusion Cost")]
    [SerializeField] private TransfusionCostType costType = TransfusionCostType.Percent;
    [SerializeField] private int baseMinCost = 5;
    [SerializeField] private int baseMaxCost = 10;
    [SerializeField] private int increasePerPurchase = 1;

    private bool interactionLocked;
    private string cacheKey;
    private int confirmedPurchaseCount;

    private void OnValidate()
    {
        baseMinCost = Mathf.Max(1, baseMinCost);
        increasePerPurchase = Mathf.Max(0, increasePerPurchase);

        if (costType == TransfusionCostType.Percent)
        {
            baseMaxCost = Mathf.Clamp(baseMaxCost, baseMinCost, 100);
        }
        else
        {
            // 상수 타입은 항상 범위 폭 15 유지
            baseMaxCost = baseMinCost + 15;
        }
    }

    public void Init(string stageCacheKey, string deviceId)
    {
        interactionLocked = false;
        gameObject.SetActive(true);
        cacheKey = $"{stageCacheKey}:{deviceId}";
        confirmedPurchaseCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (interactionLocked)
            return;

        PlayerStatControl playerStat = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStat == null)
            return;

        if (ResultManager.Instance == null)
            return;

        interactionLocked = true;
        ResultManager.Instance.OpenTransfusionResult(
            playerStat.gameObject,
            cacheKey,
            costType == TransfusionCostType.Percent,
            baseMinCost,
            baseMaxCost,
            increasePerPurchase,
            confirmedPurchaseCount,
            OnTransfusionClosed
        );
    }

    private void OnTransfusionClosed(bool confirmed, int selectedCode)
    {
        if (confirmed)
            confirmedPurchaseCount++;
        interactionLocked = false;
    }
}
