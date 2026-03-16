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

            controller.SkillReset();//�����������
            controller.SkillMinusEvent += SkillLinkOff;
            isLink = true;
    }
    // Update is called once per frame
    void SetPower()
    {
        if (controller == null || playerStat == null)
            return;

        if (playerStat.CurSkillStack <= 0)
            return;

        playerStat.CurSkillStack -= 1;
        controller.playerStatHandler.CanSkill = (playerStat.CurSkillStack > 0);
        controller.playerStatHandler.ActiveSkillCastCount += 1;
        controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);
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
            if (controller == null || controller.playerStatHandler == null)
                return;

            if (controller.playerStatHandler.ActiveSkillCastCount <= 0)
                return;

            controller.playerStatHandler.ActiveSkillCastCount = Mathf.Max(0, controller.playerStatHandler.ActiveSkillCastCount - 1);
            controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);
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
