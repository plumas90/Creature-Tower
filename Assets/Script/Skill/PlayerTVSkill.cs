using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTVSkill : Skill
{
    private WeaponSystem _weaponSystem;
    private PlayerStatControl _playerStatControl;
    private TopDownCharacterController topDownCharacter;

    public void Start()
    {
        controller.OnSkillEvent += SkillStart;
        isLink = true;
        controller.SkillMinusEvent += SkillLinkOff;
        _playerStatControl = GetComponent<PlayerStatControl>();
        _weaponSystem = GetComponent<WeaponSystem>();
        topDownCharacter = GetComponent<TopDownCharacterController>();

    }

    public override void SkillStart()
    {
        if (topDownCharacter == null || _playerStatControl == null)
            return;

        int beforeStack = _playerStatControl.CurSkillStack;
        int beforeCastCount = _playerStatControl.ActiveSkillCastCount;
        base.SkillStart();

        // 실제 스택 소모/캐스트 시작이 된 경우에만 TV 스킬 구르기를 실행한다.
        if (_playerStatControl.ActiveSkillCastCount <= beforeCastCount || _playerStatControl.CurSkillStack >= beforeStack)
            return;

        topDownCharacter.CompulsoryRoll();
        _playerStatControl.SkillRollInvincibility = true;
        Invoke(nameof(CompulsoryRollEnd), 0.8f);


    }
    public void CompulsoryRollEnd() 
    {
        if (topDownCharacter == null || _playerStatControl == null)
            return;

        _playerStatControl.SkillRollInvincibility = false;
        topDownCharacter.CompulsoryRollEnd();
        SkillEnd();
    }

    public override void SkillEnd()
    {
        base.SkillEnd();
    }
}