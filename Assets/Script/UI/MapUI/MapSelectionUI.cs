using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapSelectionUI : MonoBehaviour
{
    public static MapSelectionUI Instance { get; private set; }

    [Header("UI Setup")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform nodesContainer;
    [SerializeField] private Transform lineContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image mapBackgroundImage; // 지도 배경 이미지
    [SerializeField] private Sprite mapBackgroundSprite; // vertical_map_background_transparent

    [Header("Theme Sprites (비워두면 기존 텍스트 심볼 사용)")]
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Sprite mysterySprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite transfusionSprite;
    [SerializeField] private Sprite dnaSprite;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Sprite potionSprite;
    [SerializeField] private Sprite xMarkSprite;

    [Header("Prefabs")]
    [SerializeField] private GameObject nodePrefab;

    [Header("Layout Settings")]
    [SerializeField] private float verticalSpacing = 160f;
    [SerializeField] private float horizontalSpacing = 270f;
    [SerializeField] private float mapPaddingTop = 100f;
    [SerializeField] private float mapPaddingBottom = 100f;
    [SerializeField] private float lineOffset = 60f;

    private List<MapRoomNodeUI> _spawnedNodes = new List<MapRoomNodeUI>();
    private bool _isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Awake에서 self-SetActive(false)하면 첫 SetActive(true) 시 Awake 재진입 → 무한 꺼짐 발생
        // 에디터에서 처음부터 비활성화로 저장하거나, Start()에서 처리
    }

    private void Start()
    {
        if (mapPanel != null && mapPanel != gameObject)
        {
            mapPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 지도 UI를 열고 현재 층 기준으로 스크롤합니다.
    /// </summary>
    public void OpenMap()
    {
        if (mapPanel == null || GameManager.Instance == null) return;

        LockPlayer(true);
        mapPanel.SetActive(true);

        // 배경 이미지 적용 (vertical_map_background_transparent)
        if (mapBackgroundImage != null && mapBackgroundSprite != null)
        {
            mapBackgroundImage.sprite = mapBackgroundSprite;
            mapBackgroundImage.type = Image.Type.Simple;
            mapBackgroundImage.preserveAspect = false;
            mapBackgroundImage.color = Color.white;
        }

        // 지도를 열 때마다 최신 상태로 빌드/갱신
        if (!_isInitialized)
        {
            BuildMapUI();
            _isInitialized = true;
        }
        else
        {
            RefreshMapNodes();
        }

        // GO가 active여야 StartCoroutine 사용 가능
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SetupLayoutAndScroll());
        }
        else
        {
            Invoke(nameof(DelayedSetupLayout), 0.05f);
        }
    }

    private void DelayedSetupLayout()
    {
        Canvas.ForceUpdateCanvases();
        ApplyContainerLayout();
        if (nodesContainer is RectTransform r)
            LayoutRebuilder.ForceRebuildLayoutImmediate(r);
        ScrollToCurrentFloor();
    }

    private IEnumerator SetupLayoutAndScroll()
    {
        yield return null; // 1프레임: mapPanel 활성화 반영
        Canvas.ForceUpdateCanvases();
        ApplyContainerLayout();

        yield return null; // 2프레임: 레이아웃 리빌드
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());

        yield return null; // 3프레임: 최종 스크롤 설정
        Canvas.ForceUpdateCanvases();
        ScrollToCurrentFloor();
    }

    /// <summary>
    /// ScrollRect / Viewport / Content 레이아웃을 런타임에 강제로 맞춥니다.
    /// </summary>
    private void ApplyContainerLayout()
    {
        if (scrollRect == null || GameManager.Instance == null) return;

        // ScrollRect → 부모를 꽉 채움
        RectTransform scrollRectTR = scrollRect.GetComponent<RectTransform>();
        if (scrollRectTR != null)
        {
            scrollRectTR.anchorMin = Vector2.zero;
            scrollRectTR.anchorMax = Vector2.one;
            scrollRectTR.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTR.offsetMin = Vector2.zero;
            scrollRectTR.offsetMax = Vector2.zero;
        }

        // Viewport → ScrollRect를 꽉 채움
        if (scrollRect.viewport != null)
        {
            RectTransform vp = scrollRect.viewport;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.pivot = new Vector2(0.5f, 1f);
            vp.offsetMin = new Vector2(0f, 80f);
            vp.offsetMax = new Vector2(0f, -80f);
        }

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        // Content 전체 높이 계산
        float totalHeight = mapPaddingBottom + (mapFloors.Count * verticalSpacing) + mapPaddingTop;

        // Content(NodesContainer): anchor/pivot = (0.5, 0) → 아래를 기준으로 위로 쌓임
        if (nodesContainer is RectTransform containerRect)
        {
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
            containerRect.anchoredPosition = Vector2.zero;

            if (scrollRect.content != containerRect)
                scrollRect.content = containerRect;
        }

        // LineContainer도 동일하게
        if (lineContainer is RectTransform lineContainerTR)
        {
            lineContainerTR.anchorMin = new Vector2(0.5f, 0f);
            lineContainerTR.anchorMax = new Vector2(0.5f, 0f);
            lineContainerTR.pivot = new Vector2(0.5f, 0f);
            lineContainerTR.anchoredPosition = Vector2.zero;

            if (nodesContainer is RectTransform containerRect2)
                lineContainerTR.sizeDelta = containerRect2.sizeDelta;
        }

        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    public void CloseMap()
    {
        if (mapPanel == null) return;
        mapPanel.SetActive(false);
        LockPlayer(false);
    }

    private void LockPlayer(bool lockInput)
    {
        if (GameManager.Instance == null || GameManager.Instance.playerOBJ == null) return;

        PlayerInputController pic = GameManager.Instance.playerOBJ.GetComponent<PlayerInputController>();
        if (pic != null)
        {
            if (lockInput)
            {
                pic.InputOff();
                Rigidbody2D rb = GameManager.Instance.playerOBJ.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
            else
            {
                pic.InputOn();
            }
        }
    }

    private void BuildMapUI()
    {
        foreach (var node in _spawnedNodes)
            if (node != null) Destroy(node.gameObject);
        _spawnedNodes.Clear();

        if (lineContainer != null)
            foreach (Transform child in lineContainer)
                Destroy(child.gameObject);

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        int totalFloors = mapFloors.Count;
        ApplyContainerLayout();

        Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

        // 지도 방향: 아래에서 위로
        // f=0(1번째 층=보스) → 맨 아래, f=totalFloors-1(마지막 층) → 맨 위
        // content pivot=(0.5,0) → y=0이 맨 아래, y=totalHeight이 맨 위
        for (int f = 0; f < totalFloors; f++)
        {
            var floor = mapFloors[f];
            // f=0: 맨 아래, y = mapPaddingBottom
            // f=totalFloors-1: 맨 위
            float yPos = mapPaddingBottom + (f * verticalSpacing);

            for (int n = 0; n < floor.nodes.Count; n++)
            {
                var nodeData = floor.nodes[n];
                float xPos = 0f;

                if (!floor.isBossFloor)
                {
                    if (n == 0) xPos = -horizontalSpacing;
                    else if (n == 1) xPos = 0f;
                    else if (n == 2) xPos = horizontalSpacing;
                }

                Vector2 anchoredPos = new Vector2(xPos, yPos);
                string key = $"{f}_{n}";
                nodePositions[key] = anchoredPos;

                GameObject nodeObj = Instantiate(nodePrefab, nodesContainer, false);
                if (nodeObj == null) continue;

                RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
                if (nodeRect != null)
                {
                    nodeRect.anchorMin = new Vector2(0.5f, 0f);
                    nodeRect.anchorMax = new Vector2(0.5f, 0f);
                    nodeRect.pivot = new Vector2(0.5f, 0.5f);
                    nodeRect.anchoredPosition = anchoredPos;
                }

                MapRoomNodeUI nodeUI = nodeObj.GetComponent<MapRoomNodeUI>();
                if (nodeUI != null)
                {
                    bool isCurrent = GameManager.Instance.IsNodeCurrent(f, n);
                    bool isVisited = GameManager.Instance.IsNodeVisited(f, n);
                    bool isSelectable = GameManager.Instance.IsNodeSelectable(f, n);

                    Sprite symbolSprite = GetSpriteForTheme(nodeData.isBoss, nodeData.roomTheme);

                    nodeUI.Setup(f, n, nodeData.isBoss, nodeData.roomTheme,
                        isCurrent, isVisited, isSelectable, symbolSprite, xMarkSprite, OnNodeSelected);

                    _spawnedNodes.Add(nodeUI);
                }
            }
        }

        // 연결선 그리기
        for (int f = 0; f < totalFloors - 1; f++)
        {
            var currentFloor = mapFloors[f];
            var nextFloor = mapFloors[f + 1];

            for (int n = 0; n < currentFloor.nodes.Count; n++)
            {
                for (int nn = 0; nn < nextFloor.nodes.Count; nn++)
                {
                    string startKey = $"{f}_{n}";
                    string endKey = $"{f + 1}_{nn}";
                    if (nodePositions.ContainsKey(startKey) && nodePositions.ContainsKey(endKey))
                        DrawLine(nodePositions[startKey], nodePositions[endKey]);
                }
            }
        }
    }

    private void RefreshMapNodes()
    {
        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        int nodeUIIndex = 0;
        for (int f = 0; f < mapFloors.Count; f++)
        {
            var floor = mapFloors[f];
            for (int n = 0; n < floor.nodes.Count; n++)
            {
                var nodeData = floor.nodes[n];
                if (nodeUIIndex < _spawnedNodes.Count)
                {
                    MapRoomNodeUI nodeUI = _spawnedNodes[nodeUIIndex];
                    if (nodeUI != null)
                    {
                        bool isCurrent = GameManager.Instance.IsNodeCurrent(f, n);
                        bool isVisited = GameManager.Instance.IsNodeVisited(f, n);
                        bool isSelectable = GameManager.Instance.IsNodeSelectable(f, n);

                        Sprite symbolSprite = GetSpriteForTheme(nodeData.isBoss, nodeData.roomTheme);

                        nodeUI.Setup(f, n, nodeData.isBoss, nodeData.roomTheme,
                            isCurrent, isVisited, isSelectable, symbolSprite, xMarkSprite, OnNodeSelected);
                    }
                    nodeUIIndex++;
                }
            }
        }
    }

    private void DrawLine(Vector2 fromPos, Vector2 toPos)
    {
        if (lineContainer == null) return;

        GameObject lineObj = new GameObject("Line", typeof(Image));
        lineObj.transform.SetParent(lineContainer, false);

        Image img = lineObj.GetComponent<Image>();
        if (img != null)
            img.color = new Color(1f, 1f, 1f, 0.25f);

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            Vector2 direction = toPos - fromPos;
            float distance = direction.magnitude;

            if (distance > lineOffset * 2f)
            {
                Vector2 dirNorm = direction.normalized;
                Vector2 adjustedFrom = fromPos + dirNorm * lineOffset;
                float adjustedDistance = distance - (lineOffset * 2f);

                rect.anchoredPosition = adjustedFrom;
                rect.sizeDelta = new Vector2(4f, adjustedDistance);

                float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
            else
            {
                rect.anchoredPosition = fromPos;
                rect.sizeDelta = new Vector2(4f, distance);

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
        }
    }

    private Sprite GetSpriteForTheme(bool isBoss, NormalStage.RoomTheme theme)
    {
        if (isBoss) return bossSprite;

        switch (theme)
        {
            case NormalStage.RoomTheme.Mystery: return mysterySprite;
            case NormalStage.RoomTheme.Shop: return shopSprite;
            case NormalStage.RoomTheme.Transfusion: return transfusionSprite;
            case NormalStage.RoomTheme.DNA: return dnaSprite;
            case NormalStage.RoomTheme.Coin: return coinSprite;
            case NormalStage.RoomTheme.Box: return boxSprite;
            case NormalStage.RoomTheme.Potion: return potionSprite;
            default: return null;
        }
    }

    private void OnNodeSelected(int floorIndex, int nodeIndex)
    {
        Debug.Log($"[MapSelectionUI] Node selected: Floor={floorIndex}, Node={nodeIndex}");
        if (GameManager.Instance != null)
            GameManager.Instance.SelectMapNode(floorIndex, nodeIndex);
        CloseMap();
    }

    private void ScrollToCurrentFloor()
    {
        if (scrollRect == null || scrollRect.content == null || GameManager.Instance == null) return;

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        int currentFloorIndex = GameManager.Instance.GetCurrentMapFloorIndex();

        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 600f;

        // content pivot=(0.5,0) 기준: currentFloorIndex의 노드 y좌표
        float nodeY = mapPaddingBottom + (currentFloorIndex * verticalSpacing);

        // 현재 층이 뷰포트 하단 1/4 지점에 오도록 스크롤 위치 계산
        // scrollY: content가 viewport 아래로 얼마나 이동했는지 (양수 = 위로 스크롤)
        // 즉, content.anchoredPosition.y = scrollY (pivot=bottom이므로 양수로 위로 올라감)
        //
        // viewport 안에서 nodeY가 viewportHeight * 0.25 지점에 오도록:
        // nodeY - scrollY = viewportHeight * 0.25
        // scrollY = nodeY - viewportHeight * 0.25
        float scrollY = nodeY - viewportHeight * 0.25f;

        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);
        scrollY = Mathf.Clamp(scrollY, 0f, maxScrollY);

        scrollRect.content.anchoredPosition = new Vector2(0f, scrollY);

        Debug.Log($"[MapSelectionUI] ScrollToFloor {currentFloorIndex}: nodeY={nodeY}, contentH={contentHeight}, viewportH={viewportHeight}, scrollY={scrollY}");
    }
}
