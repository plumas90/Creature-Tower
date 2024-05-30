using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class A3105 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    float nowPower;
    float oldPower;
    bool Isfirst;
    bool ready;
    bool isLink;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            nowPower = 0;
            oldPower = 0;
            Isfirst = false;
            ready = true;
            controller.OnSkillEvent += SetPower;
            controller.OnEndAttackEvent += LostPower;

            controller.SkillReset();//여기부터참고
            controller.SkillMinusEvent += SkillLinkOff;
            isLink = true;
    }
    // Update is called once per frame
    void SetPower()
    {
        playerStat.CurSkillStack -= 1;
        controller.playerStatHandler.CanSkill = false;
        controller.playerStatHandler.useSkill = true;
        if (ready) 
        {
            nowPower = playerStat.ATK.total * 2f;
            playerStat.ATK.added += nowPower;
            oldPower = nowPower;
            ready = false;
            Isfirst = true;
        }
        SkillEnd();

    }
    void LostPower()
    {

        if (Isfirst)
        {
            playerStat.ATK.added -= oldPower;
            ready = true;
        }
        Isfirst = false;
    }
    public void SkillEnd()
    {
            controller.playerStatHandler.useSkill = false;
            if (controller.playerStatHandler.CurSkillStack > 0)
            {
                controller.playerStatHandler.CanSkill = true;
            }
            controller.CallEndSkillEvent();
    }
    public void SkillLinkOff()
    {
            if (isLink)
            {
                controller.OnSkillEvent -= SetPower;
                controller.OnEndAttackEvent -= LostPower;
                isLink = false;
            }
    }
}
