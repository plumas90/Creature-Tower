using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    //���߾� �Լ��� �������̵� �� �� ���̽��� ȣ�� �ؾ���

    protected TopDownCharacterController controller;
    protected PlayerStatControl playerStats;
    public bool isLink;

    protected Sprite[] icons;
    protected Sprite skillIcon;
    public Sprite Skillicon { get { return skillIcon; } }

    protected virtual void Awake()
    {
        controller = GetComponent<TopDownCharacterController>();
        playerStats = GetComponent<PlayerStatControl>();
        icons = Resources.LoadAll<Sprite>("Images/Skill_icon-Sheet");

        // ���� ��� ��θ� �̿��� ���� �κ��� ȣȯ��(���� ����)
        if (icons == null || icons.Length == 0)
            icons = Resources.LoadAll<Sprite>("sprite/Skill_icon-Sheet");
    }

    public void SkillLinkOff()
    {
        if (isLink) 
        {
            Debug.Log("�����ϰ� ���ŉ�ٰ� ������");
            if (controller != null)
            {
                controller.OnSkillEvent -= SkillStart;
                controller.SkillMinusEvent -= SkillLinkOff;
            }
            isLink = false;
        }
    }

    public virtual void SkillStart()
    {
        if (controller == null || playerStats == null)
            return;

        if (playerStats.CurSkillStack <= 0)
            return;

        playerStats.CurSkillStack -= 1;
        Debug.Log($"��ų ��� ����, ���� ��ų ���� �� : {controller.playerStatHandler.CurSkillStack}");
        controller.playerStatHandler.CanSkill = false;
        controller.playerStatHandler.useSkill = true;

        Debug.Log("��ų �ߵ�");
    }

    public virtual void SkillEnd()
    {
        if (controller == null)
            return;

        //��ų�� ������ ��Ÿ���� ����ϰ� ��Ÿ���� ������  controller.playerStatHandler.CanSkill = ����; �� �ٲ���
        Debug.Log("��ų ����");
        controller.playerStatHandler.useSkill = false;
        if (controller.playerStatHandler.CurSkillStack > 0)
        {
            controller.playerStatHandler.CanSkill = true;
        }
        controller.CallEndSkillEvent();
    }

    protected virtual void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnSkillEvent -= SkillStart;
            controller.SkillMinusEvent -= SkillLinkOff;
        }

        if (playerStats != null)
            playerStats.CurSkillStack = playerStats.MaxSkillStack;
    }
}
