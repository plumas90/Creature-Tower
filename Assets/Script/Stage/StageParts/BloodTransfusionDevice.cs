using System.Collections;
using UnityEngine;

// #TODO 수혈기 %의 성장량을 테스트 후 소모 성장량 0.5% 에서 0.25%를 고려하기
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
    [SerializeField] private float baseMinCost = 12f;
    [SerializeField] private float baseMaxCost = 18f;
    [SerializeField] private float increasePerPurchase = 1f;

    [Header("Animation")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite[] animSprites;
    [SerializeField] private float animationFps = 12.5f;

    private bool interactionLocked;
    private string cacheKey;
    private int confirmedPurchaseCount;
    private Coroutine activeAnimCoroutine;

    public System.Action OnInteracted;

    private void OnValidate()
    {
        baseMinCost = Mathf.Max(0.1f, baseMinCost);
        increasePerPurchase = Mathf.Max(0f, increasePerPurchase);

        if (costType == TransfusionCostType.Percent)
        {
            baseMaxCost = Mathf.Clamp(baseMaxCost, baseMinCost, 100f);
        }
        else
        {
            baseMaxCost = Mathf.Max(baseMinCost, baseMaxCost);
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
        OnInteracted?.Invoke();
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
        {
            confirmedPurchaseCount++;
            PlayAnimation();
        }
        interactionLocked = false;
    }

    private void PlayAnimation()
    {
        if (activeAnimCoroutine != null)
        {
            StopCoroutine(activeAnimCoroutine);
        }
        activeAnimCoroutine = StartCoroutine(CoPlayTransfusionAnimation());
    }

    private IEnumerator CoPlayTransfusionAnimation()
    {
        if (sr != null && animSprites != null && animSprites.Length >= 5)
        {
            int[] frameIndices = { 1, 2, 3, 4, 3, 2, 1 };
            float delay = 1f / animationFps;
            for (int i = 0; i < frameIndices.Length; i++)
            {
                int idx = frameIndices[i];
                if (idx < animSprites.Length && animSprites[idx] != null)
                {
                    sr.sprite = animSprites[idx];
                }
                yield return new WaitForSeconds(delay);
            }

            if (animSprites[0] != null)
            {
                sr.sprite = animSprites[0];
            }
        }
    }
}

