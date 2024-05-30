using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A0212 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private float bigPower;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            bigPower = 0;
            //GameManager.Instance.OnStageStartEvent += PowerUp;
            //GameManager.Instance.OnBossStageStartEvent += PowerUp;
    }
    void PowerUp()
    {
        playerStat.ATK.added -= bigPower;
        Powerset();
        playerStat.ATK.added += bigPower; // 중요한 부분2
    }
    void PowerDown()
    {
        playerStat.ATK.added -= bigPower;
    }
    void Powerset()
    {
        //int stage = GameManager.Instance.curStage;
        int stage = 1;
        switch (stage)
        {
            case 1:
                bigPower = 3;
                break;

            case 2:
                bigPower = 3;
                break;

            case 3:
                bigPower = 6;
                break;

            case 4:
                bigPower = 6;
                break;

            case 5:
                bigPower = 9;
                break;

            case 6:
                bigPower = 9;
                break;



            default:
                bigPower = 15;
                break;
        }
    }
}
