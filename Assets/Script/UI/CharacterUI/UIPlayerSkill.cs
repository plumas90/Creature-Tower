using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPlayerSkill : UIBase
{
    [SerializeField] private Sprite skillSprite;
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

        if (skillIcon == null)
        {
            var t = transform.Find("SkillIcon");
            if (t != null) skillIcon = t.GetComponent<Image>();

            if (skillIcon == null)
            {
                var legacyIcon = transform.Find("Icon");
                if (legacyIcon != null) skillIcon = legacyIcon.GetComponent<Image>();
            }
        }
        if (skillGauge == null)
        {
            var t = transform.Find("SkillIcon/SkillGauge");
            if (t != null) skillGauge = t.GetComponent<Image>();

            if (skillGauge == null)
            {
                var legacyGauge = transform.Find("ForeGround");
                if (legacyGauge != null) skillGauge = legacyGauge.GetComponent<Image>();
            }
        }

        if (skillIcon != null && skillSprite != null)
            skillIcon.sprite = skillSprite;

        if (skillGauge != null)
        {
            skillGauge.type = Image.Type.Filled;
            skillGauge.fillMethod = Image.FillMethod.Radial360;
            skillGauge.fillOrigin = (int)Image.Origin360.Top;
            skillGauge.fillClockwise = true;
        }

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
        if (playerCool == null || playerStats == null) return;

        float total = Mathf.Max(0.0001f, playerStats.SkillCoolTime.total);
        float remain = Mathf.Clamp(playerCool.curSkillCool, 0f, total);

        if (skillGauge != null)
            skillGauge.fillAmount = remain / total;
    }
}
