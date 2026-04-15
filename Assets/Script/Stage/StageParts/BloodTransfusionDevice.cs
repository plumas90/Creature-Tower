using UnityEngine;

// 플레이어가 접촉하면 체력 10% 소모로 스탯 구매 UI를 연다.
// 같은 스테이지의 같은 장치는 취소 후 재진입해도 동일 목록을 유지한다.
// 다음 스테이지로 넘어가면 Stage.Init()에서 목록 캐시가 초기화된다.
public class BloodTransfusionDevice : MonoBehaviour
{
    private bool interactionLocked;
    private string cacheKey;

    public void Init(string stageCacheKey, string deviceId)
    {
        interactionLocked = false;
        gameObject.SetActive(true);
        cacheKey = $"{stageCacheKey}:{deviceId}";
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
        ResultManager.Instance.OpenTransfusionResult(playerStat.gameObject, cacheKey, OnTransfusionClosed);
    }

    private void OnTransfusionClosed(bool confirmed, int selectedCode)
    {
        interactionLocked = false;
    }
}
