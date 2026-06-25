using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBossHP : MonoBehaviour
{
    private static UIBossHP _instance;
    private const string RuntimeContainerName = "UIBossHP_RuntimeRoot";
    private const string BossHpPrefabResourcesPath = "Prefabs/UI/UIBossHP_Auto";
#if UNITY_EDITOR
    private const string BossHpPrefabAssetPath = "Assets/Prefabs/UI/UIBossHP_Auto.prefab";
#endif

    private BossBase _currentBoss;
    private readonly List<BossBase> _aliveBosses = new List<BossBase>();
    private readonly Dictionary<BossBase, float> _lastHitTime = new Dictionary<BossBase, float>();

    private RectTransform _templatePanel;
    private Image _icon;
    private Slider _hpSlider;
    private TextMeshProUGUI _bossName;
    private TextMeshProUGUI _hpText;
    private Image _fillImage;

    private readonly List<BossHpPanelInstance> _activePanels = new List<BossHpPanelInstance>();

    private class BossHpPanelInstance
    {
        public BossBase boss;
        public RectTransform panelRt;
        public Slider hpSlider;
        public Image fillImage;
        public TextMeshProUGUI bossName;
        public Image icon;
    }

    [Header("New Bar Setup")]
    [SerializeField] private Sprite barBorderSprite;
    [SerializeField] private Sprite barFillSprite;
    [SerializeField] private Sprite bossSymbolSprite;
    [SerializeField] private TMP_FontAsset customFont;

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
            _instance.EnsureParentCanvas();
            _instance.BuildIfNeeded();
            return;
        }

        Canvas canvas = FindOrCreateCanvas();

        if (TryInstantiatePrefabUnderCanvas(canvas, out _instance))
        {
            _instance.BuildIfNeeded();
            return;
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

        EnsureParentCanvas();
        BuildIfNeeded();
        ApplyUiRuntimeSettings();
    }

    private void Update()
    {
        RefreshCurrentBoss();
        UpdateHpPanels();
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

    private void UpdateHpPanels()
    {
        // 1. Remove and destroy panels for bosses that are no longer in _aliveBosses or are null
        for (int i = _activePanels.Count - 1; i >= 0; i--)
        {
            var panel = _activePanels[i];
            if (panel.boss == null || !panel.boss.live || !_aliveBosses.Contains(panel.boss))
            {
                if (panel.panelRt != null)
                {
                    Destroy(panel.panelRt.gameObject);
                }
                _activePanels.RemoveAt(i);
            }
        }

        // 2. Add new panels for bosses in _aliveBosses that don't have a panel yet
        for (int i = 0; i < _aliveBosses.Count; i++)
        {
            var boss = _aliveBosses[i];
            if (boss == null || !boss.live) continue;

            bool hasPanel = false;
            for (int j = 0; j < _activePanels.Count; j++)
            {
                if (_activePanels[j].boss == boss)
                {
                    hasPanel = true;
                    break;
                }
            }

            if (!hasPanel && _templatePanel != null)
            {
                var clone = Instantiate(_templatePanel, _templatePanel.parent, false);
                clone.gameObject.SetActive(true);

                var instance = new BossHpPanelInstance();
                instance.boss = boss;
                instance.panelRt = clone;

                var iconTrans = clone.Find("BossIcon");
                if (iconTrans != null) instance.icon = iconTrans.GetComponent<Image>();

                var sliderTrans = clone.Find("BossHPBar");
                if (sliderTrans != null) instance.hpSlider = sliderTrans.GetComponent<Slider>();

                var nameTrans = clone.Find("BossName");
                if (nameTrans != null) instance.bossName = nameTrans.GetComponent<TextMeshProUGUI>();

                var fillTrans = clone.Find("BossHPBar/Fill Area/Fill");
                if (fillTrans != null) instance.fillImage = fillTrans.GetComponent<Image>();

                ConfigureNewUiLayout(instance);
                _activePanels.Add(instance);
            }
        }

        // 3. Position the active panels vertically stacked
        for (int i = 0; i < _activePanels.Count; i++)
        {
            var panel = _activePanels[i];
            if (panel.panelRt != null)
            {
                panel.panelRt.anchorMin = new Vector2(0.5f, 0f);
                panel.panelRt.anchorMax = new Vector2(0.5f, 0f);
                panel.panelRt.pivot = new Vector2(0.5f, 0f);
                panel.panelRt.anchoredPosition = new Vector2(0f, 40f + i * 110f);
            }
        }
    }

    private void RefreshView()
    {
        for (int i = 0; i < _activePanels.Count; i++)
        {
            var panel = _activePanels[i];
            if (panel.boss == null || panel.panelRt == null) continue;

            if (panel.hpSlider != null)
            {
                panel.hpSlider.minValue = 0f;
                panel.hpSlider.maxValue = panel.boss.maxHp;
                panel.hpSlider.value = Mathf.Clamp(panel.boss.curHp, 0f, panel.boss.maxHp);
            }

            if (panel.fillImage != null && panel.boss.maxHp > 0f)
            {
                panel.fillImage.fillAmount = Mathf.Clamp01(panel.boss.curHp / panel.boss.maxHp);
            }

            if (panel.bossName != null)
            {
                panel.bossName.text = panel.boss.name;
            }

            if (panel.icon != null)
            {
                panel.icon.sprite = bossSymbolSprite;
            }
        }
    }

    private void BuildIfNeeded()
    {
        if (TryBindExistingUi())
        {
            ApplyUiRuntimeSettings();
            if (_templatePanel != null)
                _templatePanel.gameObject.SetActive(false);
            return;
        }
        
        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
        }

        panelObj.transform.SetParent(rootRect, false);
        _templatePanel = panelObj.GetComponent<RectTransform>();
        _templatePanel.sizeDelta = new Vector2(420f, 76f);
        ForcePanelToBottomCenter();

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

        GameObject nameObj = new GameObject("BossName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(panelObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.offsetMin = new Vector2(76f, -24f);
        nameRt.offsetMax = new Vector2(-8f, -2f);
        _bossName = nameObj.GetComponent<TextMeshProUGUI>();
        _bossName.fontSize = 16;
        _bossName.alignment = TextAlignmentOptions.Left;
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

        GameObject valueObj = new GameObject("BossHPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObj.transform.SetParent(panelObj.transform, false);
        RectTransform valueRt = valueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(1f, 0f);
        valueRt.anchorMax = new Vector2(1f, 1f);
        valueRt.pivot = new Vector2(1f, 0.5f);
        valueRt.offsetMin = new Vector2(-86f, 8f);
        valueRt.offsetMax = new Vector2(-6f, -8f);
        _hpText = valueObj.GetComponent<TextMeshProUGUI>();
        _hpText.fontSize = 14;
        _hpText.alignment = TextAlignmentOptions.Right;
        _hpText.color = Color.white;

        _templatePanel.gameObject.SetActive(false);
        ApplyUiRuntimeSettings();
    }

    private bool TryBindExistingUi()
    {
        if (_templatePanel == null)
            _templatePanel = FindRectTransform("Panel");
        if (_icon == null)
            _icon = FindImage("BossIcon");
        if (_hpSlider == null)
            _hpSlider = FindSlider("BossHPBar");
        if (_bossName == null)
            _bossName = FindTextMeshPro("BossName");
        if (_hpText == null)
            _hpText = FindTextMeshPro("BossHPText");

        return _templatePanel != null && _hpSlider != null && _bossName != null && _hpText != null;
    }

    private RectTransform FindRectTransform(string name)
    {
        Transform target = transform.Find(name);
        if (target == null && _templatePanel != null)
            target = _templatePanel.Find(name);
        if (target == null)
            return null;
        return target.GetComponent<RectTransform>();
    }

    private Image FindImage(string name)
    {
        Transform target = transform.Find(name);
        if (target == null && _templatePanel != null)
            target = _templatePanel.Find(name);
        if (target == null)
            return null;
        return target.GetComponent<Image>();
    }

    private Slider FindSlider(string name)
    {
        Transform target = transform.Find(name);
        if (target == null && _templatePanel != null)
            target = _templatePanel.Find(name);
        if (target == null)
            return null;
        return target.GetComponent<Slider>();
    }

    private TextMeshProUGUI FindTextMeshPro(string name)
    {
        Transform target = transform.Find(name);
        if (target == null && _templatePanel != null)
            target = _templatePanel.Find(name);
        if (target == null)
            return null;
        return target.GetComponent<TextMeshProUGUI>();
    }

    private void ForcePanelToBottomCenter()
    {
        if (_templatePanel == null)
            return;

        _templatePanel.anchorMin = new Vector2(0.5f, 0f);
        _templatePanel.anchorMax = new Vector2(0.5f, 0f);
        _templatePanel.pivot = new Vector2(0.5f, 0f);
        _templatePanel.anchoredPosition = new Vector2(0f, 40f);
    }

    private void ApplyUiRuntimeSettings()
    {
        if (_templatePanel == null)
            return;

        RectTransform container = EnsureUiContainer();
        if (container != null && _templatePanel.parent != container)
        {
            Vector2 anchored = _templatePanel.anchoredPosition;
            _templatePanel.SetParent(container, false);
            _templatePanel.anchoredPosition = anchored;
        }

        _templatePanel.anchorMin = new Vector2(0.5f, 0f);
        _templatePanel.anchorMax = new Vector2(0.5f, 0f);
        _templatePanel.pivot = new Vector2(0.5f, 0f);
        _templatePanel.anchoredPosition = new Vector2(0f, 40f);

        Graphic[] graphics = _templatePanel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        Selectable[] selectables = _templatePanel.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
            selectables[i].interactable = false;
    }

    private void ConfigureNewUiLayout(BossHpPanelInstance instance)
    {
        if (instance == null || instance.panelRt == null) return;

        Sprite inSprite = barFillSprite;
        Sprite outSprite = barBorderSprite;
        Sprite symbolSprite = bossSymbolSprite;

        Image panelBg = instance.panelRt.GetComponent<Image>();
        if (panelBg != null)
        {
            panelBg.color = Color.clear;
        }

        if (instance.hpSlider != null)
        {
            RectTransform sliderRt = instance.hpSlider.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0f);
            sliderRt.anchorMax = new Vector2(0.5f, 0f);
            sliderRt.pivot = new Vector2(0.5f, 0f);
            // Sleeker Height: 46f instead of 82.8f
            sliderRt.sizeDelta = new Vector2(864f, 46f);
            sliderRt.anchoredPosition = new Vector2(0f, 10f);

            Transform bgTrans = instance.hpSlider.transform.Find("Background");
            if (bgTrans != null)
            {
                Image bgImage = bgTrans.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = outSprite;
                    bgImage.type = Image.Type.Simple;
                    bgImage.color = Color.white;
                }
                RectTransform bgRt = bgTrans.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
            }

            Transform fillAreaTrans = instance.hpSlider.transform.Find("Fill Area");
            if (fillAreaTrans != null)
            {
                RectTransform fillAreaRt = fillAreaTrans.GetComponent<RectTransform>();
                fillAreaRt.anchorMin = Vector2.zero;
                fillAreaRt.anchorMax = Vector2.one;
                // Precise padding with Sleeker height (height scale = 2.0x):
                // Left/Right: 4 * 5.4 = 21.6f
                // Bottom/Top: 4 * 2.0 = 8.0f
                fillAreaRt.offsetMin = new Vector2(21.6f, 8f);
                fillAreaRt.offsetMax = new Vector2(-21.6f, -8f);
            }

            Transform fillTrans = instance.hpSlider.transform.Find("Fill Area/Fill");
            if (fillTrans != null)
            {
                instance.fillImage = fillTrans.GetComponent<Image>();
                if (instance.fillImage != null)
                {
                    instance.fillImage.sprite = inSprite;
                    instance.fillImage.type = Image.Type.Filled;
                    instance.fillImage.fillMethod = Image.FillMethod.Horizontal;
                    instance.fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    instance.fillImage.color = Color.white;
                }
                RectTransform fillRt = fillTrans.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }

            instance.hpSlider.fillRect = null;
        }

        if (instance.bossName != null)
        {
            RectTransform nameRt = instance.bossName.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0f, 0f);
            nameRt.sizeDelta = new Vector2(350f, 35f);
            // Position it slightly above the slider (slider top edge is 10 + 46 = 56, so placing it at y=59f)
            nameRt.anchoredPosition = new Vector2(-432f, 59f);
            
            if (customFont != null)
            {
                instance.bossName.font = customFont;
            }
            instance.bossName.alignment = TextAlignmentOptions.Left;
            instance.bossName.fontSize = 26;
            instance.bossName.fontStyle = FontStyles.Bold;
            instance.bossName.color = Color.black;
        }

        if (instance.icon != null)
        {
            RectTransform symbolRt = instance.icon.GetComponent<RectTransform>();
            symbolRt.anchorMin = new Vector2(0.5f, 0f);
            symbolRt.anchorMax = new Vector2(0.5f, 0f);
            symbolRt.pivot = new Vector2(0.5f, 0.5f);
            // 1.5x larger Symbol: 72x72
            symbolRt.sizeDelta = new Vector2(72f, 72f);
            // Centered on the top edge of the slider: 10 + 46 = 56
            symbolRt.anchoredPosition = new Vector2(0f, 56f);

            instance.icon.sprite = symbolSprite;
            instance.icon.preserveAspect = true;
            instance.icon.color = Color.white;

            symbolRt.SetAsLastSibling();
        }

        instance.panelRt.sizeDelta = new Vector2(920f, 110f);
    }

    private RectTransform EnsureUiContainer()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindOrCreateCanvas();

        Transform existing = canvas.transform.Find(RuntimeContainerName);
        RectTransform container;

        if (existing == null)
        {
            GameObject containerObj = new GameObject(RuntimeContainerName, typeof(RectTransform));
            container = containerObj.GetComponent<RectTransform>();
            container.SetParent(canvas.transform, false);
        }
        else
        {
            container = existing as RectTransform;
            if (container == null)
            {
                container = existing.gameObject.AddComponent<RectTransform>();
                container.SetParent(canvas.transform, false);
            }
        }

        container.anchorMin = new Vector2(0.5f, 0f);
        container.anchorMax = new Vector2(0.5f, 0f);
        container.pivot = new Vector2(0.5f, 0f);
        container.sizeDelta = Vector2.zero;

        return container;
    }

    private static bool TryInstantiatePrefabUnderCanvas(Canvas canvas, out UIBossHP instance)
    {
        instance = null;

        GameObject prefab = Resources.Load<GameObject>(BossHpPrefabResourcesPath);
#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BossHpPrefabAssetPath);
#endif
        if (prefab == null)
            return false;

        GameObject root = Instantiate(prefab, canvas.transform, false);
        instance = root.GetComponent<UIBossHP>();
        if (instance == null)
            instance = root.AddComponent<UIBossHP>();
        return true;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas != null)
            return canvas;

        GameObject canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureParentCanvas()
    {
        if (GetComponentInParent<Canvas>() != null)
            return;

        Canvas canvas = FindOrCreateCanvas();
        transform.SetParent(canvas.transform, false);
    }
}
