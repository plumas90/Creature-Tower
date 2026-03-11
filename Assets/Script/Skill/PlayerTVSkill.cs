using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Scrollbar;

public class PlayerTVSkill : Skill
{
    WeaponSystem _weaponSystem;
    PlayerStatControl _playerStatControl;
    TopDownCharacterController topDownCharacter;

    public void Start()
    {
         controller.OnSkillEvent += SkillStart;
         isLink = true;
         controller.SkillMinusEvent += SkillLinkOff;
         _playerStatControl = GetComponent<PlayerStatControl>();
         _weaponSystem = GetComponent<WeaponSystem>();
        topDownCharacter =GetComponent<TopDownCharacterController>();

    }

    public override void SkillStart()
    {
        base.SkillStart();
        topDownCharacter.CompulsoryRoll();
        _playerStatControl.CurRollStack -= 1;
        _playerStatControl.CanRoll = false;
        _playerStatControl.SkillRollInvincibility = true;
        Invoke("CompulsoryRollEnd", 0.8f);


    }
    public void CompulsoryRollEnd() 
    {
        _playerStatControl.CanRoll = true;
        _playerStatControl.SkillRollInvincibility = false;
        topDownCharacter.CompulsoryRollEnd();

        SkillEnd();
    }

    public override void SkillEnd()
    {
        base.SkillEnd();
    }
}
