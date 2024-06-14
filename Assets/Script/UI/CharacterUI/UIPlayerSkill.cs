using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPlayerSkill : UIBase
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image skillGauge;
    private PlayerStatControl playerStats;
    private CoolTimeController playerCool;
    //private int playerClass;

    public override void Initialize()
    {
        InitializeData();
        UpdateValue();
    }

    void InitializeData()
    {
        GameObject player;

            player = GameManager.Instance.playerOBJ;

        playerCool = GameManager.Instance.playerOBJ.GetComponent<CoolTimeController>();
        playerStats = GameManager.Instance.playerOBJ.GetComponent<PlayerStatControl>();

        //PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CustomProperyDefined.CLASS_PROPERTY, out object temp);
        //playerClass = (int)temp;

        //var playerInput = GameManager.Instance.playerOBJ.GetComponent<PlayerInputController>();
        //playerInput.OnSkillEvent += UpdateSkillIcon;

        //UpdateSkillIcon();
    }

    /*
    public void UpdateSkillIcon()
    {
        //Debug.LogAssertion($"{playerClass}");
        switch (playerClass)
        {
            default:
                break;
            case 0:
                skillIcon.sprite = GameManager.Instance.playerOBJ.GetComponent<Player1Skill>().Skillicon;
                break;
            case 1:
                skillIcon.sprite = GameManager.Instance.playerOBJ.GetComponent<Player2Skill>().Skillicon;
                break;
            case 2:
                skillIcon.sprite = GameManager.Instance.playerOBJ.GetComponent<Player3Skill>().Skillicon;
                break;
        }
    }
    */

    public void UpdateValue()
    {
        float numerator = playerCool.curSkillCool;
        float denominator = 1 / playerStats.SkillCoolTime.total;
        skillGauge.fillAmount = numerator * denominator;
    }
}
