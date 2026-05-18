using UnityEngine;
// Forced import comment
using System.Collections;

public class BalanceScale : MonoBehaviour
{
    [Header("Scale Positions")]
    public Transform leftPivot;
    public Transform rightPivot;

    [Header("Animation/Movement settings")]
    public float tiltAngle = 15f; // 기울어질 각도
    public float tiltSpeed = 2f;  // 기울어지는 속도

    private GameObject leftReward;
    private GameObject rightReward;
    private bool choiceMade = false;

    public void Setup(GameObject left, GameObject right)
    {
        leftReward = left;
        rightReward = right;
        choiceMade = false;

        // 보상을 저울 좌우 피벗 위치로 이동시킵니다.
        if (leftPivot != null && leftReward != null)
            leftReward.transform.position = leftPivot.position;
        if (rightPivot != null && rightReward != null)
            rightReward.transform.position = rightPivot.position;

        // 보상의 선택/상호작용 이벤트 감지 등록
        SubscribeToReward(leftReward, true);
        SubscribeToReward(rightReward, false);
    }

    private void SubscribeToReward(GameObject reward, bool isLeft)
    {
        if (reward == null) return;

        ResultBox box = reward.GetComponent<ResultBox>();
        if (box != null)
        {
            box.OnOpened += () => OnRewardSelected(isLeft);
            return;
        }

        BloodTransfusionDevice transfusion = reward.GetComponent<BloodTransfusionDevice>();
        if (transfusion != null)
        {
            transfusion.OnInteracted += () => OnRewardSelected(isLeft);
            return;
        }

        ShopController shop = reward.GetComponent<ShopController>();
        if (shop != null)
        {
            shop.OnInteracted += () => OnRewardSelected(isLeft);
            return;
        }
    }

    private void OnRewardSelected(bool isLeft)
    {
        if (choiceMade) return;
        choiceMade = true;

        Debug.Log($"[BalanceScale] Selected reward on the {(isLeft ? "Left" : "Right")}!");

        // 저울 기울임 애니메이션 구동
        StartCoroutine(TiltScaleRoutine(isLeft));

        // 선택받지 못한 반대쪽 보상 비활성화 처리
        GameObject unchosen = isLeft ? rightReward : leftReward;
        if (unchosen != null)
        {
            StartCoroutine(DisableOppositeRoutine(unchosen));
        }
    }

    private IEnumerator TiltScaleRoutine(bool isLeft)
    {
        // 선택된 쪽은 무거워져서 아래로 내려가므로 기울기 Z축 반전
        float targetZ = isLeft ? tiltAngle : -tiltAngle;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetZ);
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * tiltSpeed;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    private IEnumerator DisableOppositeRoutine(GameObject unchosen)
    {
        // 선택 시 시각적인 여운을 주기 위해 아주 미세한 지연 후 파괴/소멸
        yield return new WaitForSeconds(0.2f);
        
        if (unchosen != null)
        {
            // 이펙트를 소환하거나 연출 후 비활성화
            unchosen.SetActive(false);
        }
    }
}
