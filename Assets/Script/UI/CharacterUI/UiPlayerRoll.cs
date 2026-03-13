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
        GameObject player = ResolvePlayer();
        if (player == null)
            return;

        playerCool = player.GetComponent<CoolTimeController>();
        playerStat = player.GetComponent<PlayerStatControl>();

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
            if (bgImage != null)
            {
                // Keep sprite as configured in prefab.
            }
        }

        if (dodgeGauge != null)
        {
            dodgeGauge.type = Image.Type.Filled;
            dodgeGauge.fillMethod = Image.FillMethod.Horizontal;
            dodgeGauge.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

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
        if (dodgeSlider != null && dodgeGauge != null)
            return;

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        if (root.sizeDelta.x <= 1f)
            root.sizeDelta = new Vector2(260f, 28f);

        if (GetComponent<Image>() == null)
        {
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            if (hpBackgroundSprite != null)
                bg.sprite = hpBackgroundSprite;
        }

        if (dodgeSlider == null)
            dodgeSlider = GetComponent<Slider>();
        if (dodgeSlider == null)
            dodgeSlider = gameObject.AddComponent<Slider>();

        Transform fillAreaTr = transform.Find("FillArea");
        RectTransform fillAreaRt;
        if (fillAreaTr == null)
        {
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(transform, false);
            fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(12f, 6f);
            fillAreaRt.offsetMax = new Vector2(-12f, -6f);
        }
        else
        {
            fillAreaRt = fillAreaTr.GetComponent<RectTransform>();
        }

        Transform fillTr = fillAreaRt.Find("Fill");
        if (fillTr == null)
        {
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillAreaRt, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            dodgeGauge = fillObj.GetComponent<Image>();
            dodgeGauge.color = new Color(0.22f, 0.9f, 0.5f, 1f);
            if (staminaGaugeSprite != null)
                dodgeGauge.sprite = staminaGaugeSprite;
        }
        else
        {
            dodgeGauge = fillTr.GetComponent<Image>();
        }

        dodgeSlider.fillRect = dodgeGauge != null ? dodgeGauge.rectTransform : null;
        dodgeSlider.direction = Slider.Direction.LeftToRight;
        dodgeSlider.transition = Selectable.Transition.None;
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