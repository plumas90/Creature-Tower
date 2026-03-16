using Unity.VisualScripting;
using UnityEngine;


public class A3206 : MonoBehaviour // ���� ������
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;

    bool isLink;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnSkillEvent += MakeWall;
            controller.SkillReset();//�����������
            controller.SkillMinusEvent += SkillLinkOff;
            isLink = true;
    }
    private void MakeWall() 
    {
        if (controller == null || playerStat == null)
            return;

        if (playerStat.CurSkillStack <= 0)
            return;

        playerStat.CurSkillStack -= 1;
        controller.playerStatHandler.CanSkill = (playerStat.CurSkillStack > 0);
        controller.playerStatHandler.ActiveSkillCastCount += 1;
        controller.playerStatHandler.useSkill = (controller.playerStatHandler.ActiveSkillCastCount > 0);
        Vector2 player =  transform.position;
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3 dir = (mouse - player).normalized * 1.5f;
        float angle = Mathf.Atan2(mouse.y - player.y, mouse.x - player.x) * Mathf.Rad2Deg;
        /*
        PhotonNetwork.Instantiate("AugmentList/A3206", transform.position + dir, Quaternion.Euler(new Vector3(0,0,angle-90)));
        */
        SkillEnd();
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
                controller.OnSkillEvent -= MakeWall;
                isLink = false;
            }
    }
}
