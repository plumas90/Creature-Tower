using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// TestAugmentUI 씬 오브젝트를 올바르게 재구축합니다.
/// - 기존 Canvas renderMode / CanvasScaler 는 절대 건드리지 않습니다.
/// - 생성하는 모든 오브젝트의 localScale 을 (1,1,1)로 강제합니다.
/// - 스트레치 RectTransform의 sizeDelta 음수값(-8 등)은 정상이므로 건드리지 않습니다.
/// </summary>
public class RebuildTestAugmentUI
{
    [MenuItem("Tools/Rebuild TestAugment UI")]
    public static void Rebuild()
    {
        // ── 한글 폰트 로드 ─────────────────────────────────────────────
        TMP_FontAsset korFont = null;
        var guids = AssetDatabase.FindAssets("Galmuri9 SDF t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            korFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            Debug.Log($"[Rebuild] 폰트 로드: {fontPath} → {(korFont != null ? korFont.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning("[Rebuild] 'Galmuri9 SDF' TMP_FontAsset 를 찾지 못했습니다.");
        }

        // ── 기존 Canvas 찾기 (설정 변경 금지) ─────────────────────────
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) { Debug.LogError("[Rebuild] Canvas 없음"); return; }

        // ── TestGameManager 확보 ────────────────────────────────────────
        var tgmObj = GameObject.Find("TestGameManager");
        if (tgmObj == null) { Debug.LogError("[Rebuild] TestGameManager 없음"); return; }
        var tauiComp = tgmObj.GetComponent<TestAugmentUI>() ?? tgmObj.AddComponent<TestAugmentUI>();

        // ── 기존 TestAugmentUIRoot 삭제 ────────────────────────────────
        var old = GameObject.Find("TestAugmentUIRoot");
        if (old != null) Object.DestroyImmediate(old);

        // ── UI 생성 헬퍼 ──────────────────────────────────────────────
        // ★ new GameObject(name, typeof(RectTransform)) 을 써야 처음부터 RectTransform 보유
        // ★ 생성 직후 반드시 transform.localScale = Vector3.one 설정
        RectTransform MakeRT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;          // ← 음수 scale 방지
            return (RectTransform)go.transform;
        }

        void Stretch(RectTransform rt, float padL = 0, float padR = 0, float padT = 0, float padB = 0)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            // offsetMin = (left padding, bottom padding)
            // offsetMax = (-right padding, -top padding)
            rt.offsetMin = new Vector2(padL, padB);
            rt.offsetMax = new Vector2(-padR, -padT);
        }

        void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);   // y 는 아래 방향이 음수
            rt.sizeDelta = new Vector2(w, h);
        }

        // TMP 생성 시 폰트 적용 헬퍼
        void ApplyFont(TextMeshProUGUI tmp)
        {
            if (korFont != null) tmp.font = korFont;
        }

        // ── Root (Canvas 전체 채움) ────────────────────────────────────
        var rootRT = MakeRT("TestAugmentUIRoot", canvasObj.transform);
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.pivot     = new Vector2(0.5f, 0.5f);
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // TestAugmentManager 컴포넌트
        var tamComp = rootRT.gameObject.GetComponent<TestAugmentManager>()
                   ?? rootRT.gameObject.AddComponent<TestAugmentManager>();
        // 폰트를 Inspector 기본값으로 자동 설정
        if (korFont != null)
        {
            var tamSO = new SerializedObject(tamComp);
            tamSO.FindProperty("fontAsset").objectReferenceValue = korFont;
            tamSO.ApplyModifiedProperties();
        }

        // ── 토글 버튼 ─────────────────────────────────────────────────
        var tRT = MakeRT("AugmentToggleBtn", rootRT);
        TopLeft(tRT, 10, 10, 130, 44);
        var tImg = tRT.gameObject.AddComponent<Image>();
        tImg.color = new Color(0.15f, 0.55f, 0.25f, 1f);
        tRT.gameObject.AddComponent<Button>();

        var lblRT = MakeRT("Text", tRT);
        Stretch(lblRT);
        var lblTmp = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(lblTmp);               // ★ font 먼저, text 나중
        lblTmp.fontSize  = 16;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.color     = Color.white;
        lblTmp.raycastTarget = false;
        lblTmp.text      = "증강 목록";

        // ── 패널 ──────────────────────────────────────────────────────
        var pRT = MakeRT("AugmentPanel", rootRT);
        TopLeft(pRT, 10, 58, 460, 560);
        pRT.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.94f);

        // ── ScrollRect ────────────────────────────────────────────────
        var svRT = MakeRT("ScrollView", pRT);
        Stretch(svRT, 2, 16, 2, 2);   // 오른쪽 16px = 스크롤바 공간
        var scrollRect = svRT.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        // ★ Mask+Image(clear) 대신 RectMask2D 사용
        //   - Image alpha=0 인 Mask는 스텐실 버퍼에 쓰이지 않아 전체를 잘라냄
        //   - RectMask2D는 RectTransform 경계만으로 클리핑하므로 안정적
        var vpRT = MakeRT("Viewport", svRT);
        Stretch(vpRT);
        vpRT.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRT;

        // Content
        var cRT = MakeRT("Content", vpRT);
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot     = new Vector2(0.5f, 1);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = Vector2.zero;
        var vlg = cRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        cRT.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = cRT;

        // Scrollbar
        var sbRT = MakeRT("Scrollbar", pRT);
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot     = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(14, 0);
        sbRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f);
        var sb = sbRT.gameObject.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var hRT = MakeRT("Handle", sbRT);
        hRT.anchorMin = Vector2.zero;
        hRT.anchorMax = Vector2.one;
        hRT.offsetMin = hRT.offsetMax = Vector2.zero;
        hRT.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.55f, 0.55f);
        sb.handleRect = hRT;
        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility =
            ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // ── rowPrefab ─────────────────────────────────────────────────
        string prefabPath = "Assets/Prefabs/UI/AugmentRowPrefab.prefab";

        var rowRT = MakeRT("AugmentRowPrefab", rootRT);   // 임시 위치
        rowRT.sizeDelta = new Vector2(400, 64);            // 임시 고정 크기 (prefab 저장용)
        rowRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 1f);
        rowRT.gameObject.AddComponent<Button>();
        // LayoutElement: VLG가 높이를 올바르게 잡도록
        var rowLE = rowRT.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 64;
        rowLE.minHeight = 48;
        rowLE.flexibleWidth = 1;

        // NameText (왼쪽 30%)
        var nmRT = MakeRT("NameText", rowRT);
        nmRT.anchorMin = new Vector2(0,     0);
        nmRT.anchorMax = new Vector2(0.30f, 1);
        nmRT.offsetMin = new Vector2(6, 2);
        nmRT.offsetMax = new Vector2(0, -2);
        var nmTmp = nmRT.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(nmTmp);               // ★ font 먼저
        nmTmp.fontStyle       = FontStyles.Bold;
        nmTmp.fontSize        = 13;
        nmTmp.alignment       = TextAlignmentOptions.MidlineLeft;
        nmTmp.color           = Color.white;
        nmTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nmTmp.overflowMode    = TextOverflowModes.Ellipsis;
        nmTmp.raycastTarget   = false;
        nmTmp.text            = "이름";

        // DescText (오른쪽 70%)
        var dcRT = MakeRT("DescText", rowRT);
        dcRT.anchorMin = new Vector2(0.31f, 0);
        dcRT.anchorMax = new Vector2(1f,    1);
        dcRT.offsetMin = new Vector2(2,  2);
        dcRT.offsetMax = new Vector2(-4, -2);
        var dcTmp = dcRT.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(dcTmp);               // ★ font 먼저
        dcTmp.fontSize        = 11;
        dcTmp.alignment       = TextAlignmentOptions.MidlineLeft;
        dcTmp.color           = new Color(0.85f, 0.85f, 0.85f);
        dcTmp.textWrappingMode = TextWrappingModes.Normal;
        dcTmp.raycastTarget   = false;
        dcTmp.text            = "설명";

        var rowPrefab = PrefabUtility.SaveAsPrefabAsset(rowRT.gameObject, prefabPath);
        Object.DestroyImmediate(rowRT.gameObject);
        if (rowPrefab == null) { Debug.LogError("[Rebuild] rowPrefab 저장 실패: " + prefabPath); return; }

        // ── TestAugmentUI 레퍼런스 연결 ────────────────────────────────
        var so = new SerializedObject(tauiComp);
        so.FindProperty("panel").objectReferenceValue          = pRT.gameObject;
        so.FindProperty("contentParent").objectReferenceValue  = cRT;
        so.FindProperty("rowPrefab").objectReferenceValue      = rowPrefab;
        so.ApplyModifiedProperties();

        // ── 토글 버튼 onClick ────────────────────────────────────────
        var btn    = tRT.gameObject.GetComponent<Button>();
        var btnSO  = new SerializedObject(btn);
        var calls  = btnSO.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.ClearArray();
        calls.InsertArrayElementAtIndex(0);
        var c0 = calls.GetArrayElementAtIndex(0);
        c0.FindPropertyRelative("m_Target").objectReferenceValue          = tauiComp;
        c0.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue   = typeof(TestAugmentUI).AssemblyQualifiedName;
        c0.FindPropertyRelative("m_MethodName").stringValue               = "TogglePanel";
        c0.FindPropertyRelative("m_Mode").intValue                        = 1;   // void
        c0.FindPropertyRelative("m_CallState").intValue                   = 2;   // RuntimeOnly
        btnSO.ApplyModifiedProperties();

        // ── scale 최종 검증 ──────────────────────────────────────────
        bool scaleOK = true;
        foreach (Transform t in rootRT.GetComponentsInChildren<Transform>(true))
        {
            if (t.localScale != Vector3.one)
            {
                Debug.LogWarning($"[Rebuild] localScale 이상: {t.name} = {t.localScale} → 강제 (1,1,1)");
                t.localScale = Vector3.one;
                scaleOK = false;
            }
        }

        pRT.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"[Rebuild] 완료! scaleOK={scaleOK}  rowPrefab={rowPrefab.name}  font={korFont?.name ?? "NULL"}");
    }
}
