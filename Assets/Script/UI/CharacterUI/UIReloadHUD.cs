using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIReloadHUD: UIBase
{
    [SerializeField] private Sprite reloadBackgroundSprite;
    [SerializeField] private Sprite reloadGaugeSprite;
    [SerializeField] private Image reloadGauge;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Vector2 gaugeStartLocalPos = new Vector2(24f, 0f);
    [SerializeField] private float gaugeTravelX = 120f;

    public GameObject player;
    private CoolTimeController controller;
    private PlayerStatControl statHandler;
    private Camera targetCamera;
    public bool startcheck = false;

    public override void Initialize()
    {
        InitializeData();
        UpdateData();
        Close();
    }

    public void InitializeData()
    {
        player = GameManager.Instance.playerOBJ.gameObject;
        targetCamera = Camera.main;

        controller = player.GetComponent<CoolTimeController>();
        statHandler = player.GetComponent<PlayerStatControl>();
        player.GetComponent<TopDownCharacterController>().OnReloadEvent += OpenReloadHUD;
        player.GetComponent<TopDownCharacterController>().OnEndReloadEvent += Close;
        BuildIfNeeded();
        startcheck = true;
    }

    private void OpenReloadHUD()
    {
        Open();
        UpdateData();
    }

    public void UpdateData()
    {
        if (controller == null || statHandler == null) return;

        float total = Mathf.Max(0.0001f, statHandler.ReloadCoolTime.total);
        float remain = Mathf.Clamp(controller.curReloadCool, 0f, total);
        float progress = 1f - (remain / total);

        if (reloadGauge != null)
        {
            RectTransform gaugeRt = reloadGauge.rectTransform;
            float x = gaugeStartLocalPos.x + (gaugeTravelX * progress);
            gaugeRt.anchoredPosition = new Vector2(x, gaugeStartLocalPos.y);
        }

    }

    private void OnEnable()
    {
        if (player != null && startcheck)
            UpdateData();
    }

    // Update is called once per frame
    void Update()
    {
        if (startcheck) 
        {
            UpdateData();
            FollowPlayer();
        }

    }

    private void FollowPlayer()
    {
        if (player == null) return;

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 worldPos = player.transform.position + worldOffset;
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
            rt.position = screenPos;
    }

    private void BuildIfNeeded()
    {
        if (reloadGauge != null)
            return;

        var bgTr = transform.Find("Reload_BG");
        if (bgTr != null)
        {
            var bgImg = bgTr.GetComponent<Image>();
            if (bgImg != null && reloadBackgroundSprite != null)
                bgImg.sprite = reloadBackgroundSprite;

            var gaugeTr = bgTr.Find("Reload_Gauge");
            if (gaugeTr != null)
            {
                reloadGauge = gaugeTr.GetComponent<Image>();
                if (reloadGauge != null && reloadGaugeSprite != null)
                    reloadGauge.sprite = reloadGaugeSprite;
            }
        }

        if (reloadGauge != null)
        {
            reloadGauge.type = Image.Type.Simple;
            return;
        }

        RectTransform root = GetComponent<RectTransform>();
        if (root != null)
            root.sizeDelta = new Vector2(180f, 32f);

        GameObject bgObj = new GameObject("Reload_BG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = Color.white;
        if (reloadBackgroundSprite != null)
            bgImage.sprite = reloadBackgroundSprite;

        GameObject fillObj = new GameObject("Reload_Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0.5f, 0.5f);
        fillRt.anchoredPosition = gaugeStartLocalPos;
        fillRt.sizeDelta = new Vector2(10f, 16f);

        reloadGauge = fillObj.GetComponent<Image>();
        reloadGauge.type = Image.Type.Simple;
        reloadGauge.color = Color.white;
        if (reloadGaugeSprite != null)
            reloadGauge.sprite = reloadGaugeSprite;
    }
}
