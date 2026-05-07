using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextStageStairs : MonoBehaviour
{
    public Stage thisStage;
    private bool consumed;

    private void OnEnable()
    {
        consumed = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (consumed)
            return;

        //Debug.Log("[NextStageStairs] 계단 진입 체크");
        // 플레이어 자식 콜라이더 진입도 허용
        var playerStatControl = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStatControl != null && GameManager.Instance != null)
        {
            consumed = true;
            GameManager.Instance.NextLevel();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (consumed)
            return;

        var playerStatControl = collision.collider.GetComponentInParent<PlayerStatControl>();
        if (playerStatControl != null && GameManager.Instance != null)
        {
            consumed = true;
            Debug.Log("[NextStageStairs] 계단 충돌 체크");
            GameManager.Instance.NextLevel();
        }
    }
}
