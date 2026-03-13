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
        playerStats = ResolvePlayerStats();
        if (playerStats == null)
            return;

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
            if (bgImage != null)
            {
                // Keep sprite as configured in prefab.
            }
        }

        if (hpGauge != null)
        {
            hpGauge.type = Image.Type.Filled;
            hpGauge.fillMethod = Image.FillMethod.Horizontal;
            hpGauge.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        playerStats.OnChangeCurHPEvent -= UpdateValue;
        playerStats.OnChangeCurHPEvent += UpdateValue;
    }

    private PlayerStatControl ResolvePlayerStats()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
            return GameManager.Instance.playerOBJ.GetComponent<PlayerStatControl>();

        return Object.FindObjectOfType<PlayerStatControl>();
    }

    private void BuildIfNeeded()
    {
        if (hpSlider != null && hpGauge != null && hpText != null)
            return;

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        if (root.sizeDelta.x <= 1f)
            root.sizeDelta = new Vector2(260f, 40f);

        if (GetComponent<Image>() == null)
        {
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            if (hpBackgroundSprite != null)
                bg.sprite = hpBackgroundSprite;
        }

        if (hpSlider == null)
            hpSlider = GetComponent<Slider>();
        if (hpSlider == null)
            hpSlider = gameObject.AddComponent<Slider>();

        Transform fillAreaTr = transform.Find("FillArea");
        RectTransform fillAreaRt;
        if (fillAreaTr == null)
        {
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(transform, false);
            fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(12f, 8f);
            fillAreaRt.offsetMax = new Vector2(-12f, -8f);
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

            hpGauge = fillObj.GetComponent<Image>();
            hpGauge.color = new Color(0.95f, 0.2f, 0.2f, 1f);
            if (hpGaugeSprite != null)
                hpGauge.sprite = hpGaugeSprite;
        }
        else
        {
            hpGauge = fillTr.GetComponent<Image>();
        }

        hpSlider.fillRect = hpGauge != null ? hpGauge.rectTransform : null;
        hpSlider.direction = Slider.Direction.LeftToRight;
        hpSlider.transition = Selectable.Transition.None;

        if (hpText == null)
        {
            Transform textTr = transform.Find("HP_Text");
            if (textTr == null)
            {
                GameObject textObj = new GameObject("HP_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(transform, false);
                RectTransform txtRt = textObj.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(1f, 0.5f);
                txtRt.anchorMax = new Vector2(1f, 0.5f);
                txtRt.pivot = new Vector2(1f, 0.5f);
                txtRt.anchoredPosition = new Vector2(-8f, 0f);
                txtRt.sizeDelta = new Vector2(150f, 30f);
                hpText = textObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                hpText = textTr.GetComponent<TextMeshProUGUI>();
            }
        }

        if (hpText != null)
        {
            hpText.fontSize = 20f;
            hpText.alignment = TextAlignmentOptions.Right;
            hpText.color = Color.white;
        }
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
