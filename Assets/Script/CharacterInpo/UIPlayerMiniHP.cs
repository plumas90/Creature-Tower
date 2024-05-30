using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerMiniHP : UIBase//, ICommonUI
{
    private Slider hpGauge;
    private PlayerStatControl playerStats;

    public override void Initialize()
    {
        InitializeData();
        UpdateValue();
        Open();
    }

    void InitializeData()
    {
        var player = transform.parent.GetComponent<UIPlayerMiniHUD>().Player;

        hpGauge = GetComponentInChildren<Slider>();
        playerStats = player.GetComponent<PlayerStatControl>();

        playerStats.OnChangeCurHPEvent += UpdateValue;
    }

    private void UpdateValue()
    {
        hpGauge.minValue = 0;
        hpGauge.maxValue = playerStats.HP.total;
        hpGauge.value = playerStats.CurHP;
    }
}
