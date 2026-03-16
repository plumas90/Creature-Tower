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
        // 스택 기반: 남은 스택이 있으면 즉시 다음 스킬 사용을 허용한다.
        controller.playerStatHandler.CanSkill = (playerStats.CurSkillStack > 0);
        controller.playerStatHandler.ActiveSkillCastCount += 1;
        controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);

        Debug.Log("��ų �ߵ�");
    }

    public virtual void SkillEnd()
    {
        if (controller == null)
            return;

        if (controller.playerStatHandler == null)
            return;

        // 유효한 스킬 캐스트가 없는데 들어온 종료 호출은 무시한다.
        // (예: 파생 스킬에서 base.SkillStart()가 실패했는데 SkillEnd()가 호출된 경우)
        if (controller.playerStatHandler.ActiveSkillCastCount <= 0)
            return;

        //��ų�� ������ ��Ÿ���� ����ϰ� ��Ÿ���� ������  controller.playerStatHandler.CanSkill = ����; �� �ٲ���
        Debug.Log("��ų ����");
        controller.playerStatHandler.ActiveSkillCastCount = Mathf.Max(0, controller.playerStatHandler.ActiveSkillCastCount - 1);
        controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);
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

            if (controller.playerStatHandler != null)
            {
                controller.playerStatHandler.ActiveSkillCastCount = 0;
                controller.playerStatHandler.useSkill = false;
            }
        }

        if (playerStats != null)
            playerStats.CurSkillStack = playerStats.MaxSkillStack;
    }
}
