using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MapMakeSetting 테스트씬 전용 증강 디버그 UI.
/// 섹션 헤더(직업 티어 1/2/3, 공용 티어 1/2/3)로 분류하여 표시.
/// TestAugmentManager.fontAsset이 설정되어 있어야 텍스트가 올바르게 렌더링됩니다.
/// </summary>
public class TestAugmentUI : MonoBehaviour
{
    public static TestAugmentUI Instance;

    [Header("UI References")]
    public GameObject panel;
    public Transform contentParent;
    public GameObject rowPrefab;

    private readonly List<GameObject> rows = new List<GameObject>();
    private static readonly string[] ClassDisplayNames = { "TV", "찰리", "김길환" };

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panel == null) return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        if (show)
        {
            // 비활성 상태에서 생성된 행들의 레이아웃을 강제로 즉시 재계산
            Canvas.ForceUpdateCanvases();
            if (contentParent is RectTransform cRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cRT);
        }
    }

    /// <summary>TestGameManager.StartTest() 완료 후 호출 → 목록 갱신</summary>
    public void RefreshList()
    {
        foreach (var r in rows) Destroy(r);
        rows.Clear();

        if (MakeAugmentListManager.Instance == null) return;
        var mgr = MakeAugmentListManager.Instance;

        // 현재 캐릭터 클래스명 결정
        int charClass = 0;
        if (AugmentManager.Instance != null && AugmentManager.Instance.playerstatHandler != null)
            charClass = AugmentManager.Instance.playerstatHandler.CharacterClass;
        string charName = (charClass >= 0 && charClass < ClassDisplayNames.Length)
            ? ClassDisplayNames[charClass] : "알 수 없음";

        // ── 직업 전용 (code >= 1000) ──────────────────────────────
        AddSection($"★ {charName} 티어 1", mgr.SpecialAugment1, IsJobAugment, new Color(0.12f, 0.18f, 0.30f));
        AddSection($"★ {charName} 티어 2", mgr.SpecialAugment2, IsJobAugment, new Color(0.12f, 0.18f, 0.30f));
        AddSection($"★ {charName} 티어 3", mgr.SpecialAugment3, IsJobAugment, new Color(0.12f, 0.18f, 0.30f));

        // ── 공용 (code 100~999) ───────────────────────────────────
        AddSection("■ 공용 티어 1", mgr.SpecialAugment1, IsCommonAugment, new Color(0.10f, 0.24f, 0.14f));
        AddSection("■ 공용 티어 2", mgr.SpecialAugment2, IsCommonAugment, new Color(0.10f, 0.24f, 0.14f));
        AddSection("■ 공용 티어 3", mgr.SpecialAugment3, IsCommonAugment, new Color(0.10f, 0.24f, 0.14f));

        // 패널이 이미 보이는 상태라면 즉시 레이아웃 재계산
        // 비활성 상태라면 TogglePanel()에서 열릴 때 재계산함
        if (panel != null && panel.activeSelf && contentParent is RectTransform cRT)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cRT);
        }
    }

    private static bool IsJobAugment(SpecialAugment a)    => a.Code >= 1000;
    private static bool IsCommonAugment(SpecialAugment a) => a.Code >= 100 && a.Code < 1000;

    private void AddSection(string title, List<SpecialAugment> source,
        System.Predicate<SpecialAugment> filter, Color headerColor)
    {
        var list = source.FindAll(filter);
        if (list.Count == 0) return;

        rows.Add(CreateHeader(title, headerColor));
        foreach (var aug in list)
            rows.Add(CreateRow(aug));
    }

    private GameObject CreateHeader(string title, Color bgColor)
    {
        var go = new GameObject("Header", typeof(RectTransform));
        go.transform.SetParent(contentParent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 30;
        le.minHeight = 30;

        go.AddComponent<Image>().color = bgColor;

        var tGO = new GameObject("T", typeof(RectTransform));
        tGO.transform.SetParent(go.transform, false);
        var tRT = (RectTransform)tGO.transform;
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(10, 0);
        tRT.offsetMax = Vector2.zero;

        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 13;
        tmp.color = new Color(0.75f, 0.92f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(tmp);

        return go;
    }

    private GameObject CreateRow(SpecialAugment aug)
    {
        var row = Instantiate(rowPrefab, contentParent);

        // VLG가 높이를 올바르게 잡도록 LayoutElement 보장
        var le = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
        le.preferredHeight = 64;
        le.minHeight = 48;

        var nameTmp = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTmp != null) { nameTmp.text = aug.Name;  ApplyFont(nameTmp); }

        var descTmp = row.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
        if (descTmp != null) { descTmp.text = aug.func;  ApplyFont(descTmp); }

        var btn = row.GetComponent<Button>();
        if (btn != null)
        {
            int   code = aug.Code;
            string name = aug.Name;
            btn.onClick.AddListener(() =>
            {
                AugmentManager.Instance.AugmentCall(code);
                Debug.Log($"[TestAugmentUI] 증강 적용: {name} (code={code})");
            });
        }

        return row;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        var font = TestAugmentManager.Instance?.fontAsset;
        if (font != null) tmp.font = font;
    }
}
