using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPlayerHP : UIBase
{
    [SerializeField] private Sprite hpBackgroundSprite;
    [SerializeField] private Sprite hpGaugeSprite;
    [SerializeField] private Image hpGauge;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;
    private PlayerStatControl playerStats;


    public override void Initialize()
    {
        InitializeData();
        UpdateValue();
        Open();
    }

    void InitializeData()
    {

        playerStats = GameManager.Instance.playerOBJ.GetComponent<PlayerStatControl>();
        if (hpGauge == null)
        {
            var t = transform.Find("HP_BG/HP_Gauge");
            if (t != null) hpGauge = t.GetComponent<Image>();

            if (hpGauge == null)
            {
                var legacyFill = transform.Find("FillParent/Fill");
                if (legacyFill != null) hpGauge = legacyFill.GetComponent<Image>();
            }
        }

        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
            if (hpSlider == null)
                hpSlider = GetComponentInChildren<Slider>(true);

            if (hpSlider != null && hpGauge == null && hpSlider.fillRect != null)
                hpGauge = hpSlider.fillRect.GetComponent<Image>();
        }

        if (hpText == null)
        {
            var t = transform.Find("HP_BG/HP_Text");
            if (t != null) hpText = t.GetComponent<TMP_Text>();

            if (hpText == null)
            {
                var legacyText = transform.Find("Text");
                if (legacyText != null) hpText = legacyText.GetComponent<TMP_Text>();
            }
        }

        var bg = transform.Find("HP_BG");
        if (bg == null)
            bg = transform.Find("Background");

        if (bg != null)
        {
            var bgImage = bg.GetComponent<Image>();
            if (bgImage != null && hpBackgroundSprite != null)
                bgImage.sprite = hpBackgroundSprite;
        }

        if (hpGauge != null)
        {
            if (hpGaugeSprite != null)
                hpGauge.sprite = hpGaugeSprite;

            hpGauge.type = Image.Type.Filled;
            hpGauge.fillMethod = Image.FillMethod.Horizontal;
            hpGauge.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        playerStats.OnChangeCurHPEvent += UpdateValue;
    }

    private void UpdateValue()
    {
        if (playerStats == null) return;

        float maxHp = Mathf.Max(1f, playerStats.HP.total);
        float curHp = Mathf.Clamp(playerStats.CurHP, 0f, maxHp);
        float normalized = curHp / maxHp;

        if (hpGauge != null)
            hpGauge.fillAmount = normalized;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = maxHp;
            hpSlider.value = curHp;
        }

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(curHp) + " / " + Mathf.CeilToInt(maxHp);
    }
}
