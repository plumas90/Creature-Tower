using Unity.VisualScripting;
using UnityEngine;


public class A3206 : MonoBehaviour // 공병 생성형
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;

    bool isLink;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnSkillEvent += MakeWall;
            controller.SkillReset();//여기부터참고
            controller.SkillMinusEvent += SkillLinkOff;
            isLink = true;
    }
    private void MakeWall() 
    {
        playerStat.CurSkillStack -= 1;
        controller.playerStatHandler.CanSkill = false;
        controller.playerStatHandler.useSkill = true;
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
                controller.OnSkillEvent -= MakeWall;
                isLink = false;
            }
    }
}
