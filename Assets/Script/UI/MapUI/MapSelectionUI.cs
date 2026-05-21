using System;
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

    [Header("Prefabs")]
    [SerializeField] private GameObject nodePrefab; // MapRoomNodeUI prefab

    [Header("Layout Settings")]
    [SerializeField] private float verticalSpacing = 160f;
    [SerializeField] private float horizontalSpacing = 180f;
    [SerializeField] private float mapPaddingBottom = 100f;

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
        
        // Only auto-deactivate at initial scene load frame to avoid cancelling runtime activation
        if (mapPanel != null && Time.frameCount == 0)
        {
            mapPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the Map UI and centers on the current active floor.
    /// </summary>
    public void OpenMap()
    {
        if (mapPanel == null || GameManager.Instance == null) return;

        // 1. Lock Player Input
        LockPlayer(true);

        mapPanel.SetActive(true);

        // 2. Ensure container layout and links are fully intact before building
        EnsureContainerLayout();

        // 3. Build or Refresh Map Elements
        if (!_isInitialized)
        {
            BuildMapUI();
            _isInitialized = true;
        }
        else
        {
            RefreshMapNodes();
        }

        // 4. Force immediate layout updates
        Canvas.ForceUpdateCanvases();
        if (nodesContainer != null && nodesContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        // 5. Scroll to Current Floor
        ScrollToCurrentFloor();

        // 6. Spawn persistent double-frame coroutine to bypass Unity UI internal overrides
        StartCoroutine(ScrollToCurrentFloorDelayed());
    }

    private System.Collections.IEnumerator ScrollToCurrentFloorDelayed()
    {
        // Frame 1 check
        yield return null;
        Canvas.ForceUpdateCanvases();
        EnsureContainerLayout();
        if (nodesContainer != null && nodesContainer is RectTransform containerRect1)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect1);
        }
        ScrollToCurrentFloor();

        // Frame 2 check (Unity UI double frame dirty rendering bug protection)
        yield return null;
        EnsureContainerLayout();
        ScrollToCurrentFloor();
    }

    private void EnsureContainerLayout()
    {
        if (scrollRect == null || GameManager.Instance == null) return;

        // 1. Force Scroll View to stretch-fill the entire parent UI panel
        if (scrollRect.transform is RectTransform scrollRectTR)
        {
            scrollRectTR.anchorMin = Vector2.zero;
            scrollRectTR.anchorMax = Vector2.one;
            scrollRectTR.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTR.offsetMin = Vector2.zero;
            scrollRectTR.offsetMax = Vector2.zero;
        }

        // 2. Force Viewport to stretch-fill the Scroll View container
        if (scrollRect.viewport != null)
        {
            RectTransform viewportTR = scrollRect.viewport;
            viewportTR.anchorMin = Vector2.zero;
            viewportTR.anchorMax = Vector2.one;
            viewportTR.pivot = new Vector2(0f, 1f);
            viewportTR.offsetMin = Vector2.zero;
            viewportTR.offsetMax = Vector2.zero;
        }

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        float totalHeight = (mapFloors.Count * verticalSpacing) + mapPaddingBottom * 2f;

        // 3. Force nodesContainer (content) layout parameters with precise safety pivot shift calculation
        if (nodesContainer != null && nodesContainer is RectTransform containerRect)
        {
            float cachedWidth = containerRect.sizeDelta.x;
            
            // Set pivot safely without shifting the physical position in Unity Editor space
            Vector2 targetPivot = new Vector2(0.5f, 1f);
            Vector2 deltaPivot = containerRect.pivot - targetPivot;
            Vector2 deltaPosition = new Vector2(
                deltaPivot.x * containerRect.rect.width * containerRect.localScale.x, 
                deltaPivot.y * containerRect.rect.height * containerRect.localScale.y
            );
            
            containerRect.pivot = targetPivot;
            containerRect.anchorMin = targetPivot;
            containerRect.anchorMax = targetPivot;
            
            containerRect.sizeDelta = new Vector2(cachedWidth, totalHeight);
            containerRect.anchoredPosition -= deltaPosition;

            // Force dynamic link ScrollRect content if missing
            if (scrollRect.content != containerRect)
            {
                scrollRect.content = containerRect;
            }
        }

        // 4. Force lineContainer to perfectly stretch-fill nodesContainer (forces identical 1:1 local coordinate space)
        if (lineContainer != null && lineContainer is RectTransform lineContainerTR)
        {
            lineContainerTR.anchorMin = new Vector2(0.5f, 1f);
            lineContainerTR.anchorMax = new Vector2(0.5f, 1f);
            lineContainerTR.pivot = new Vector2(0.5f, 1f);
            lineContainerTR.anchoredPosition = Vector2.zero;
            
            if (nodesContainer != null && nodesContainer is RectTransform containerRect2)
            {
                lineContainerTR.sizeDelta = containerRect2.sizeDelta;
            }
        }
    }

    /// <summary>
    /// Closes the Map UI and unlocks player input.
    /// </summary>
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
                
                // Also stop velocity to prevent slide during map open
                Rigidbody2D rb = GameManager.Instance.playerOBJ.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                pic.InputOn();
            }
        }
    }

    private void BuildMapUI()
    {
        // Clear old ones
        foreach (var node in _spawnedNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        _spawnedNodes.Clear();

        // Clear line container
        if (lineContainer != null)
        {
            foreach (Transform child in lineContainer)
            {
                Destroy(child.gameObject);
            }
        }

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        int totalFloors = mapFloors.Count;
        float totalHeight = (totalFloors * verticalSpacing) + mapPaddingBottom * 2f;

        EnsureContainerLayout();
        if (nodesContainer != null && nodesContainer is RectTransform containerRect)
        {
            containerRect.anchoredPosition = new Vector2(0f, 0f);
        }

        // 1. Spawn Nodes
        Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

        for (int f = 0; f < totalFloors; f++)
        {
            var floor = mapFloors[f];
            // Standard symmetric Top-Center downward layout
            float yPos = -mapPaddingBottom - ((totalFloors - 1 - f) * verticalSpacing);

            for (int n = 0; n < floor.nodes.Count; n++)
            {
                var nodeData = floor.nodes[n];
                float xPos = 0f;

                if (!floor.isBossFloor)
                {
                    // 3 nodes layout: Left, Center, Right
                    if (n == 0) xPos = -horizontalSpacing;
                    else if (n == 1) xPos = 0f;
                    else if (n == 2) xPos = horizontalSpacing;
                }

                Vector2 anchoredPos = new Vector2(xPos, yPos);
                string key = $"{f}_{n}";
                nodePositions[key] = anchoredPos;

                // Instantiate Node UI
                GameObject nodeObj = Instantiate(nodePrefab, nodesContainer, false);
                if (nodeObj == null) continue;

                RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
                if (nodeRect != null)
                {
                    nodeRect.anchoredPosition = anchoredPos;
                }

                MapRoomNodeUI nodeUI = nodeObj.GetComponent<MapRoomNodeUI>();
                if (nodeUI != null)
                {
                    // Setup node visual states
                    bool isCurrent = GameManager.Instance.IsNodeCurrent(f, n);
                    bool isVisited = GameManager.Instance.IsNodeVisited(f, n);
                    bool isSelectable = GameManager.Instance.IsNodeSelectable(f, n);

                    nodeUI.Setup(
                        f, 
                        n, 
                        nodeData.isBoss, 
                        nodeData.roomTheme, 
                        isCurrent, 
                        isVisited, 
                        isSelectable, 
                        OnNodeSelected
                    );

                    _spawnedNodes.Add(nodeUI);
                }
            }
        }

        // 2. Draw Connection Lines between adjacent floors
        for (int f = 0; f < totalFloors - 1; f++)
        {
            var currentFloor = mapFloors[f];
            var nextFloor = mapFloors[f + 1];

            for (int n = 0; n < currentFloor.nodes.Count; n++)
            {
                for (int nn = 0; nn < nextFloor.nodes.Count; nn++)
                {
                    // We connect current floor's node(n) to next floor's node(nn).
                    // In our hourglass layout:
                    // - Even floors (Boss, 1 node) connect to all 3 nodes of Odd floor.
                    // - Odd floors (3 nodes) connect to the single node of Even floor.
                    string startKey = $"{f}_{n}";
                    string endKey = $"{f + 1}_{nn}";

                    if (nodePositions.ContainsKey(startKey) && nodePositions.ContainsKey(endKey))
                    {
                        DrawLine(nodePositions[startKey], nodePositions[endKey]);
                    }
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

                        nodeUI.Setup(
                            f, 
                            n, 
                            nodeData.isBoss, 
                            nodeData.roomTheme, 
                            isCurrent, 
                            isVisited, 
                            isSelectable, 
                            OnNodeSelected
                        );
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
        {
            img.color = new Color(1f, 1f, 1f, 0.2f); // Faint line
        }

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            Vector2 direction = toPos - fromPos;
            float distance = direction.magnitude;

            rect.anchoredPosition = fromPos;
            rect.sizeDelta = new Vector2(6f, distance); // Thick visible line

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    private void OnNodeSelected(int floorIndex, int nodeIndex)
    {
        Debug.Log($"[MapSelectionUI] Node selected at Floor {floorIndex}, Node {nodeIndex}");
        
        // Notify GameManager to process the stage choice transition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectMapNode(floorIndex, nodeIndex);
        }

        CloseMap();
    }

    private void ScrollToCurrentFloor()
    {
        if (scrollRect == null || scrollRect.content == null || GameManager.Instance == null) return;

        var mapFloors = GameManager.Instance.GetMapFloors();
        if (mapFloors == null || mapFloors.Count == 0) return;

        int currentFloorIndex = GameManager.Instance.GetCurrentMapFloorIndex();
        int totalFloors = mapFloors.Count;
        
        // Calculate total height of the container
        float totalHeight = (totalFloors * verticalSpacing) + mapPaddingBottom * 2f;
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 600f;

        // Use the exact same symmetric downward placement Y coordinate formula
        float yPos = -mapPaddingBottom - ((totalFloors - 1 - currentFloorIndex) * verticalSpacing);

        // Center the node inside the viewport by setting content anchored Y position directly
        float targetY = -yPos - (viewportHeight * 0.5f);
        float maxScroll = totalHeight - viewportHeight;
        targetY = Mathf.Clamp(targetY, 0f, Mathf.Max(0f, maxScroll));

        Debug.Log($"[MapSelectionUI] ScrollToCurrentFloor -> Floor Index: {currentFloorIndex}, Content Height: {totalHeight}, Viewport Height: {viewportHeight}, TargetY: {targetY}, MaxScroll: {maxScroll}");

        // Direct position assignment to bypass Unity's dirty layout normalizedPosition calculation issues
        scrollRect.content.anchoredPosition = new Vector2(0f, targetY);
        
        Debug.Log($"[MapSelectionUI] Verification -> Content anchoredPosition after setting: {scrollRect.content.anchoredPosition}");
    }
}
