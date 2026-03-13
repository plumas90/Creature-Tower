using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBossHP : MonoBehaviour
{
    private static UIBossHP _instance;

    private BossBase _currentBoss;
    private readonly List<BossBase> _aliveBosses = new List<BossBase>();
    private readonly Dictionary<BossBase, float> _lastHitTime = new Dictionary<BossBase, float>();

    private RectTransform _panel;
    private Image _icon;
    private Slider _hpSlider;
    private Text _bossName;
    private Text _hpText;

    public static void NotifyBossEngaged(BossBase boss)
    {
        if (boss == null) return;
        EnsureInstance();
        if (_instance == null) return;

        _instance.RegisterBoss(boss, false);
    }

    public static void NotifyBossDamaged(BossBase boss)
    {
        if (boss == null) return;
        EnsureInstance();
        if (_instance == null) return;

        _instance.RegisterBoss(boss, true);
    }

    public static void NotifyBossDied(BossBase boss)
    {
        if (_instance == null || boss == null) return;
        _instance.UnregisterBoss(boss);
    }

    private static void EnsureInstance()
    {
        if (_instance != null) return;

        UIBossHP[] existing = FindObjectsOfType<UIBossHP>(true);
        if (existing != null && existing.Length > 0)
        {
            _instance = existing[0];
            _instance.BuildIfNeeded();
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject root = new GameObject("UIBossHP_Auto");
        root.transform.SetParent(canvas.transform, false);
        _instance = root.AddComponent<UIBossHP>();
        _instance.BuildIfNeeded();
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        BuildIfNeeded();
    }

    private void Update()
    {
        RefreshCurrentBoss();
        RefreshView();
    }

    private void RegisterBoss(BossBase boss, bool makeCurrent)
    {
        if (boss == null) return;

        if (!_aliveBosses.Contains(boss))
            _aliveBosses.Add(boss);

        if (makeCurrent)
        {
            _lastHitTime[boss] = Time.unscaledTime;
            _currentBoss = boss;
        }
        else if (_currentBoss == null)
        {
            _currentBoss = boss;
        }
    }

    private void UnregisterBoss(BossBase boss)
    {
        _aliveBosses.Remove(boss);
        _lastHitTime.Remove(boss);

        if (_currentBoss == boss)
            _currentBoss = null;
    }

    private void RefreshCurrentBoss()
    {
        for (int i = _aliveBosses.Count - 1; i >= 0; i--)
        {
            BossBase b = _aliveBosses[i];
            if (b == null || !b.live)
                _aliveBosses.RemoveAt(i);
        }

        if (_currentBoss != null && _currentBoss.live)
            return;

        _currentBoss = null;
        float bestTime = float.MinValue;

        for (int i = 0; i < _aliveBosses.Count; i++)
        {
            BossBase b = _aliveBosses[i];
            if (b == null || !b.live) continue;

            float t;
            if (!_lastHitTime.TryGetValue(b, out t))
                t = -1f;

            if (t > bestTime)
            {
                bestTime = t;
                _currentBoss = b;
            }
        }

        if (_currentBoss == null && _aliveBosses.Count > 0)
            _currentBoss = _aliveBosses[0];
    }

    private void RefreshView()
    {
        if (_panel == null || _hpSlider == null || _bossName == null || _hpText == null)
            return;

        if (_currentBoss == null || !_currentBoss.live || _currentBoss.maxHp <= 0f)
        {
            _panel.gameObject.SetActive(false);
            return;
        }

        _panel.gameObject.SetActive(true);

        _hpSlider.minValue = 0f;
        _hpSlider.maxValue = _currentBoss.maxHp;
        _hpSlider.value = Mathf.Clamp(_currentBoss.curHp, 0f, _currentBoss.maxHp);

        _bossName.text = _currentBoss.name;
        _hpText.text = Mathf.CeilToInt(Mathf.Max(0f, _currentBoss.curHp)) + " / " + Mathf.CeilToInt(_currentBoss.maxHp);

        if (_icon != null)
        {
            SpriteRenderer sr = _currentBoss.GetComponentInChildren<SpriteRenderer>(true);
            _icon.sprite = sr != null ? sr.sprite : null;
            _icon.color = _icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }

    private void BuildIfNeeded()
    {
        if (_panel != null) return;

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(transform, false);
        _panel = panelObj.GetComponent<RectTransform>();

        _panel.anchorMin = new Vector2(0.5f, 1f);
        _panel.anchorMax = new Vector2(0.5f, 1f);
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.anchoredPosition = new Vector2(0f, -20f);
        _panel.sizeDelta = new Vector2(420f, 76f);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject iconObj = new GameObject("BossIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(panelObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(8f, 0f);
        iconRt.sizeDelta = new Vector2(60f, 60f);
        _icon = iconObj.GetComponent<Image>();
        _icon.preserveAspect = true;
        _icon.color = new Color(1f, 1f, 1f, 0f);

        GameObject nameObj = new GameObject("BossName", typeof(RectTransform), typeof(Text));
        nameObj.transform.SetParent(panelObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.offsetMin = new Vector2(76f, -24f);
        nameRt.offsetMax = new Vector2(-8f, -2f);
        _bossName = nameObj.GetComponent<Text>();
        _bossName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _bossName.fontSize = 16;
        _bossName.alignment = TextAnchor.MiddleLeft;
        _bossName.color = Color.white;

        GameObject sliderObj = new GameObject("BossHPBar", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(panelObj.transform, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0f);
        sliderRt.anchorMax = new Vector2(1f, 0f);
        sliderRt.pivot = new Vector2(0.5f, 0f);
        sliderRt.offsetMin = new Vector2(76f, 8f);
        sliderRt.offsetMax = new Vector2(-90f, 30f);

        _hpSlider = sliderObj.GetComponent<Slider>();

        GameObject backgroundObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = backgroundObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image sliderBg = backgroundObj.GetComponent<Image>();
        sliderBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(3f, 3f);
        fillAreaRt.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = new Color(0.85f, 0.2f, 0.2f, 1f);

        _hpSlider.targetGraphic = fillImage;
        _hpSlider.fillRect = fillRt;
        _hpSlider.direction = Slider.Direction.LeftToRight;

        GameObject valueObj = new GameObject("BossHPText", typeof(RectTransform), typeof(Text));
        valueObj.transform.SetParent(panelObj.transform, false);
        RectTransform valueRt = valueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(1f, 0f);
        valueRt.anchorMax = new Vector2(1f, 1f);
        valueRt.pivot = new Vector2(1f, 0.5f);
        valueRt.offsetMin = new Vector2(-86f, 8f);
        valueRt.offsetMax = new Vector2(-6f, -8f);
        _hpText = valueObj.GetComponent<Text>();
        _hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hpText.fontSize = 14;
        _hpText.alignment = TextAnchor.MiddleRight;
        _hpText.color = Color.white;

        _panel.gameObject.SetActive(false);
    }
}
