using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A0208 : MonoBehaviour//피해를 입지않은 시간이 길어질수록 강해집니다.
{
    private PlayerStatControl playerStat;

    public float power;
    private void Awake()
    {
 
            playerStat = GetComponent<PlayerStatControl>();
            playerStat.HitEvent += HitDAHit;
            //GameManager.Instance.OnStageStartEvent += HitDAHit;
            //GameManager.Instance.OnBossStageStartEvent += HitDAHit;
            power = 0;
    }
    private void Update()
    {
            playerStat.ATK.added += (Time.deltaTime) * 1f;
            power += Time.deltaTime * 1f;
    }

    void HitDAHit()
    {
        playerStat.ATK.added -= power;
        power = 0;
    }

}
