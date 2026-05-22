using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapRoomNodeUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI symbolText;
    [SerializeField] private Image symbolImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject activeHighlight;
    [SerializeField] private GameObject visitedCheckmark;

    private int _floorIndex;
    private int _nodeIndex;
    private bool _isSelectable;
    private Action<int, int> _onClicked;

    public void Setup(
        int floorIndex,
        int nodeIndex,
        bool isBoss,
        NormalStage.RoomTheme theme,
        bool isCurrent,
        bool isVisited,
        bool isSelectable,
        Sprite symbolSprite,
        Action<int, int> onClicked)
    {
        _floorIndex = floorIndex;
        _nodeIndex = nodeIndex;
        _isSelectable = isSelectable;
        _onClicked = onClicked;

        // Button 연동 (있을 경우)
        if (nodeButton != null)
        {
            nodeButton.interactable = isSelectable;
            nodeButton.onClick.RemoveAllListeners();
            if (isSelectable)
                nodeButton.onClick.AddListener(HandleClick);
        }

        // 오버레이
        if (activeHighlight != null)
            activeHighlight.SetActive(isCurrent);
        if (visitedCheckmark != null)
            visitedCheckmark.SetActive(isVisited && !isCurrent);

        // 색상 & 심볼
        Color nodeColor = Color.gray;
        string symbol = "?";
        string roomName = "Mystery";

        if (isBoss)
        {
            nodeColor = new Color(0.7f, 0.1f, 0.1f, 1f);
            symbol = "💀";
            roomName = $"Boss {floorIndex / 2 + 1}";
        }
        else
        {
            switch (theme)
            {
                case NormalStage.RoomTheme.Mystery:
                    nodeColor = new Color(0.5f, 0.2f, 0.8f, 1f);
                    symbol = "?";
                    roomName = "Mystery";
                    break;
                case NormalStage.RoomTheme.Shop:
                    nodeColor = new Color(0.85f, 0.65f, 0.15f, 1f);
                    symbol = "💰";
                    roomName = "Shop";
                    break;
                case NormalStage.RoomTheme.Transfusion:
                    nodeColor = new Color(0.8f, 0.15f, 0.15f, 1f);
                    symbol = "♥";
                    roomName = "Transfusion";
                    break;
                case NormalStage.RoomTheme.DNA:
                    nodeColor = new Color(0.15f, 0.75f, 0.5f, 1f);
                    symbol = "🧬";
                    roomName = "DNA Box";
                    break;
                case NormalStage.RoomTheme.Coin:
                    nodeColor = new Color(0.9f, 0.8f, 0.1f, 1f);
                    symbol = "🪙";
                    roomName = "Coin Box";
                    break;
                case NormalStage.RoomTheme.Box:
                    nodeColor = new Color(0.6f, 0.4f, 0.2f, 1f);
                    symbol = "📦";
                    roomName = "Chest";
                    break;
                case NormalStage.RoomTheme.Potion:
                    nodeColor = new Color(0.15f, 0.55f, 0.9f, 1f);
                    symbol = "🧪";
                    roomName = "Potion";
                    break;
            }
        }

        if (bgImage != null)
        {
            nodeColor.a = (isSelectable || isCurrent || isVisited) ? 1f : 0.4f;
            bgImage.color = nodeColor;
        }

        if (symbolImage != null && symbolSprite != null)
        {
            symbolImage.sprite = symbolSprite;
            symbolImage.gameObject.SetActive(true);
            if (symbolText != null)
                symbolText.gameObject.SetActive(false);
        }
        else
        {
            if (symbolImage != null)
                symbolImage.gameObject.SetActive(false);
            if (symbolText != null)
            {
                symbolText.gameObject.SetActive(true);
                symbolText.text = symbol;
            }
        }

        if (nameText != null)
        {
            nameText.text = roomName;
            nameText.color = (isSelectable || isCurrent || isVisited)
                ? Color.white
                : new Color(1f, 1f, 1f, 0.4f);
        }

        // Raycast 수신을 위해 bgImage의 raycastTarget 보장
        if (bgImage != null)
            bgImage.raycastTarget = true;

        // GraphicRaycaster가 없을 경우를 대비해 Image 컴포넌트도 확인
        Image selfImage = GetComponent<Image>();
        if (selfImage != null)
            selfImage.raycastTarget = true;
    }

    /// <summary>
    /// IPointerClickHandler 구현 - Button 없어도 클릭 이벤트 수신
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[MapRoomNodeUI] OnPointerClick: Floor={_floorIndex}, Node={_nodeIndex}, isSelectable={_isSelectable}");
        HandleClick();
    }

    private void HandleClick()
    {
        if (_isSelectable)
        {
            Debug.Log($"[MapRoomNodeUI] HandleClick: Invoking onClicked Floor={_floorIndex}, Node={_nodeIndex}");
            _onClicked?.Invoke(_floorIndex, _nodeIndex);
        }
        else
        {
            Debug.Log($"[MapRoomNodeUI] HandleClick: Node not selectable (Floor={_floorIndex}, Node={_nodeIndex})");
        }
    }
}
