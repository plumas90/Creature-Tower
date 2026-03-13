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
        GameObject player = ResolvePlayer();
        if (player == null)
            return;

        playerCool = player.GetComponent<CoolTimeController>();
        playerStats = player.GetComponent<PlayerStatControl>();

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

    private GameObject ResolvePlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
            return GameManager.Instance.playerOBJ;

        PlayerStatControl stat = Object.FindObjectOfType<PlayerStatControl>();
        return stat != null ? stat.gameObject : null;
    }

    private void BuildIfNeeded()
    {
        if (skillIcon != null && skillGauge != null)
            return;

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        if (root.sizeDelta.x <= 1f)
            root.sizeDelta = new Vector2(64f, 64f);

        if (skillIcon == null)
        {
            GameObject iconObj = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            skillIcon = iconObj.GetComponent<Image>();
            skillIcon.color = Color.white;
            if (skillSprite != null)
                skillIcon.sprite = skillSprite;
        }

        if (skillGauge == null && skillIcon != null)
        {
            GameObject gaugeObj = new GameObject("SkillGauge", typeof(RectTransform), typeof(Image));
            gaugeObj.transform.SetParent(skillIcon.transform, false);
            RectTransform gaugeRt = gaugeObj.GetComponent<RectTransform>();
            gaugeRt.anchorMin = Vector2.zero;
            gaugeRt.anchorMax = Vector2.one;
            gaugeRt.offsetMin = Vector2.zero;
            gaugeRt.offsetMax = Vector2.zero;

            skillGauge = gaugeObj.GetComponent<Image>();
            skillGauge.color = new Color(0f, 0f, 0f, 0.6f);
        }
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
