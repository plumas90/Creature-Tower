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
        Sprite xMarkSprite,
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

        // 오버레이 (현재 있는 곳 표시 - x_mark_stamp_transparent)
        if (activeHighlight != null)
        {
            activeHighlight.SetActive(isCurrent);
            if (isCurrent)
            {
                Image highlightImage = activeHighlight.GetComponent<Image>();
                if (highlightImage != null)
                {
                    if (xMarkSprite != null)
                    {
                        highlightImage.enabled = true;
                        highlightImage.type = Image.Type.Simple;
                        highlightImage.sprite = xMarkSprite;
                        highlightImage.preserveAspect = true;
                        highlightImage.color = Color.white; // 원본 이미지 색상 유지
                        activeHighlight.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        highlightImage.sprite = null;
                        highlightImage.enabled = true;
                        highlightImage.color = new Color(1f, 0.9f, 0.1f, 0.35f);
                        activeHighlight.transform.localScale = Vector3.one;
                    }
                }
            }
            else
            {
                activeHighlight.transform.localScale = Vector3.one;
            }
        }
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
            if (symbolSprite != null)
            {
                // Hide background color for symbol nodes
                bgImage.color = Color.clear;
            }
            else
            {
                // Fallback text node keeps background color
                nodeColor.a = (isSelectable || isCurrent || isVisited) ? 1f : 0.4f;
                bgImage.color = nodeColor;
            }
        }

        if (symbolText != null)
        {
            if (symbolSprite != null)
            {
                symbolText.gameObject.SetActive(false);
            }
            else
            {
                symbolText.gameObject.SetActive(true);
                symbolText.text = symbol;
            }
        }

        if (symbolImage != null)
        {
            if (symbolSprite != null)
            {
                symbolImage.enabled = true;
                symbolImage.type = Image.Type.Simple;
                symbolImage.sprite = symbolSprite;
                symbolImage.preserveAspect = true;
                float alpha = (isSelectable || isCurrent || isVisited) ? 1f : 0.4f;
                symbolImage.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                symbolImage.enabled = false;
                symbolImage.sprite = null;
            }
        }

        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
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
