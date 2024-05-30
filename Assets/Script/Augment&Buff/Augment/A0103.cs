using UnityEngine;

public class A0103 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    float nowCoolGAM;
    float oldCoolGAM;
    private void Awake()//난사 탄퍼짐 ++ = 장전시간감소
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            nowCoolGAM = playerStat.BulletSpread.total * 0.2f;
            playerStat.ReloadCoolTime.added -= nowCoolGAM;
            oldCoolGAM = nowCoolGAM;
            //GameManager.Instance.OnStageStartEvent += SetCool;
            //GameManager.Instance.OnBossStageStartEvent += SetCool;
    }
    // Update is called once per frame
    void SetCool()
    {
        playerStat.ReloadCoolTime.added += oldCoolGAM;
        nowCoolGAM = playerStat.BulletSpread.total * 0.2f;
        playerStat.ReloadCoolTime.added -= nowCoolGAM;
        oldCoolGAM = nowCoolGAM;
        
    }
}
