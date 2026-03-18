using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class A2303 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTimeController;
    private PlayerInputController playerInputController;
    private WeaponSystem weaponSystem;
    bool isLink;
    // Start is called before the first frame update
    void Start()
    {

            controller = GetComponent<PlayerInputController>();
            playerStat = GetComponent<PlayerStatControl>();
            coolTimeController= GetComponent<CoolTimeController>();
            playerInputController = GetComponent<PlayerInputController>();
            weaponSystem =GetComponent<WeaponSystem>();

            controller.OnSkillEvent += AcroboticShot;

            controller.SkillReset();//�����������
            controller.SkillMinusEvent += SkillLinkOff;
            isLink = true;
    }

    private void AcroboticShot() 
    {
        if (controller == null || playerStat == null)
            return;

        if (playerStat.CurSkillStack <= 0)
            return;

        playerStat.CurSkillStack -= 1;
        controller.playerStatHandler.CanSkill = (playerStat.CurSkillStack > 0);
        controller.playerStatHandler.ActiveSkillCastCount += 1;
        controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);

        coolTimeController.EndRollCoolTime();//������ ��Ÿ�� �ʱ�ȭ 
        playerInputController.CallRollEvent(); // ������ ���� 
        Invoke("shoting",0.6f);
        //controller.CallEndSkillEvent();
        SkillEnd();
    }
    private void shoting() 
    {
        int n = 0;
        Quaternion rot = Quaternion.Euler(new Vector3(0, 0, n));
        for (int i = 0; i < 18; ++i)
        {
            weaponSystem.burstCall(rot);
            n += 20;
            rot = Quaternion.Euler(new Vector3(0, 0, n));
        }
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
                controller.OnSkillEvent -= AcroboticShot;
                isLink = false;
            }
    }
}
