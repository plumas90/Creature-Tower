using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static PlayerDebuffControl;

public class PlayerCharlieSkill : Skill
{
    public int applicationTime = 5;
    public float applicationspeed = 0.5f;
    public float applicationAtkSpeed = 0.5f;
    private PlayerStatControl statHandler;
    private PlayerDebuffControl debuffControl;

    //디버프 클래스 안에 절반효과를 주는 열광전염이 있음 1f기준으로 설계되있기에 수정시 같이 수정바람
    public void Start()
    {

            controller.OnSkillEvent += SkillStart;
            isLink = true;
            controller.SkillMinusEvent += SkillLinkOff;
            debuffControl= GetComponent<PlayerStatControl>()._DebuffControl;       
    }
    public override void SkillStart()
    {
        base.SkillStart();        
        playerStats.Speed.added += applicationspeed;
        playerStats.AtkSpeed.added += applicationAtkSpeed;
        debuffControl.Init(PlayerDebuffControl.buffName.Speed, applicationTime);
        Invoke("SkillEnd", applicationTime);
    }

    public override void SkillEnd()
    {
        base.SkillEnd();
        playerStats.Speed.added -= applicationspeed;
        playerStats.AtkSpeed.added -= applicationAtkSpeed;        
    }
}
