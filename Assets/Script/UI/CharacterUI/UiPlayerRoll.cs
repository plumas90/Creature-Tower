using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiPlayerRoll : UIBase
{
    [SerializeField] private Sprite hpBackgroundSprite;
    [SerializeField] private Sprite staminaGaugeSprite;
    [SerializeField] private Image dodgeGauge;
    [SerializeField] private Slider dodgeSlider;
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

        if (dodgeGauge == null)
        {
            var t = transform.Find("Stamina_BG/Stamina_Gauge");
            if (t != null) dodgeGauge = t.GetComponent<Image>();

            if (dodgeGauge == null)
            {
                var legacyFill = transform.Find("FillParent/Fill");
                if (legacyFill != null) dodgeGauge = legacyFill.GetComponent<Image>();
            }
        }

        if (dodgeSlider == null)
        {
            dodgeSlider = GetComponent<Slider>();
            if (dodgeSlider == null)
                dodgeSlider = GetComponentInChildren<Slider>(true);

            if (dodgeSlider != null && dodgeGauge == null && dodgeSlider.fillRect != null)
                dodgeGauge = dodgeSlider.fillRect.GetComponent<Image>();
        }

        var bg = transform.Find("Stamina_BG");
        if (bg == null)
            bg = transform.Find("Background");

        if (bg != null)
        {
            var bgImage = bg.GetComponent<Image>();
            if (bgImage != null && hpBackgroundSprite != null)
                bgImage.sprite = hpBackgroundSprite;
        }

        if (dodgeGauge != null)
        {
            if (staminaGaugeSprite != null)
                dodgeGauge.sprite = staminaGaugeSprite;

            dodgeGauge.type = Image.Type.Filled;
            dodgeGauge.fillMethod = Image.FillMethod.Horizontal;
            dodgeGauge.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

    }

    public void UpdateValue()
    {
        if (playerStat == null || playerCool == null) return;

        float total = Mathf.Max(0.0001f, playerStat.RollCoolTime.total);
        float remain = Mathf.Clamp(playerCool.curRollCool, 0f, total);
        float fill = 1f - (remain / total);

        if (dodgeGauge != null)
            dodgeGauge.fillAmount = fill;

        if (dodgeSlider != null)
        {
            dodgeSlider.minValue = 0f;
            dodgeSlider.maxValue = total;
            dodgeSlider.value = total - remain;
        }

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