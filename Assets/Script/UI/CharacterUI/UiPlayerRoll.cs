using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiPlayerRoll : UIBase
{
    [SerializeField] private Slider dodgeGauge;
    [SerializeField] private TMP_Text dodgeText;
    private CoolTimeController playerCool;
    private PlayerStatControl playerStat;

    public override void Initialize()
    {
        InitializeData();
        UpdateValue();
        Open();
    }

    void InitializeData()
    {

        playerCool = GameManager.Instance.playerOBJ.GetComponent<CoolTimeController>();
        playerStat = GameManager.Instance.playerOBJ.GetComponent<PlayerStatControl>();

    }

    public void UpdateValue()
    {
        dodgeGauge.minValue = 0;
        dodgeGauge.maxValue = playerStat.RollCoolTime.total;
        dodgeGauge.value = playerStat.RollCoolTime.total - playerCool.curRollCool;
        dodgeText.text = (playerStat.CurRollStack.ToString() + "/" + playerStat.MaxRollStack.ToString());
    }

    public override void Open()
    {
        gameObject.SetActive(true);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }
}