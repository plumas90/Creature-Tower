using System;
using UnityEngine;


public class PlayerKimKilWhan : Skill
{
    public Shield shield;

    public float shieldHP = 20;
    public float shieldScale=1;
    public float shieldSurvivalTime = 5;


    public void Start()
    {
        controller.OnSkillEvent += SkillStart;
        isLink = true;
        controller.SkillMinusEvent += SkillLinkOff;
    }
    public override void SkillStart()
    {
        base.SkillStart();
        shield.shiledOn(shieldHP, shieldSurvivalTime);
        SkillEnd();
    }



    public override void SkillEnd()//애를 별거 아니라고 봤는데 엄청중요함 현재 스킬이 사용되고 나서 쿨이도는방식
    {
        //Destroy(shieldOBJ);
        base.SkillEnd();
    }

}
