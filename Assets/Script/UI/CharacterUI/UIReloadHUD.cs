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
        player = ResolvePlayer();
        if (player == null)
            return;

        EnsureGaugeReference();

        targetCamera = Camera.main;

        controller = player.GetComponent<CoolTimeController>();
        statHandler = player.GetComponent<PlayerStatControl>();
        TopDownCharacterController tdc = player.GetComponent<TopDownCharacterController>();
        if (tdc != null)
        {
            tdc.OnReloadEvent -= OpenReloadHUD;
            tdc.OnReloadEvent += OpenReloadHUD;
            tdc.OnEndReloadEvent -= Close;
            tdc.OnEndReloadEvent += Close;
        }
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
        float remainRatio = remain / total;

        if (reloadGauge != null)
        {
            RectTransform gaugeRt = reloadGauge.rectTransform;
            float x = gaugeStartLocalPos.x + (gaugeTravelX * remainRatio);
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

    private void EnsureGaugeReference()
    {
        if (reloadGauge == null)
        {
            var bgTr = transform.Find("Reload_BG");
            if (bgTr != null)
            {
                var gaugeTr = bgTr.Find("Reload_Gauge");
                if (gaugeTr != null)
                    reloadGauge = gaugeTr.GetComponent<Image>();

                if (reloadGauge == null)
                {
                    var altGauge = bgTr.Find("Reload_Fill");
                    if (altGauge != null)
                        reloadGauge = altGauge.GetComponent<Image>();
                }
            }
        }

        if (reloadGauge == null)
        {
            // Prefab에 Reload HUD가 없을 때만 최소 구성 생성
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

            GameObject fillObj = new GameObject("Reload_Gauge", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.anchoredPosition = gaugeStartLocalPos;
            fillRt.sizeDelta = new Vector2(10f, 16f);

            reloadGauge = fillObj.GetComponent<Image>();
            reloadGauge.color = Color.white;
            if (reloadGaugeSprite != null)
                reloadGauge.sprite = reloadGaugeSprite;
        }

        if (reloadGauge != null)
            reloadGauge.type = Image.Type.Simple;
    }

    private GameObject ResolvePlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
            return GameManager.Instance.playerOBJ;

        PlayerStatControl stat = Object.FindObjectOfType<PlayerStatControl>();
        return stat != null ? stat.gameObject : null;
    }
}
