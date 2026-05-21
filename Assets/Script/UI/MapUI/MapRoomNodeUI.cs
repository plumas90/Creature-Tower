using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapRoomNodeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI symbolText;
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
        Action<int, int> onClicked)
    {
        _floorIndex = floorIndex;
        _nodeIndex = nodeIndex;
        _isSelectable = isSelectable;
        _onClicked = onClicked;

        // Button interaction
        if (nodeButton != null)
        {
            nodeButton.interactable = isSelectable || isCurrent || isVisited;
            nodeButton.onClick.RemoveAllListeners();
            if (isSelectable)
            {
                nodeButton.onClick.AddListener(HandleClick);
            }
        }

        // Active / Visited overlays
        if (activeHighlight != null)
        {
            activeHighlight.SetActive(isCurrent);
        }
        if (visitedCheckmark != null)
        {
            visitedCheckmark.SetActive(isVisited);
        }

        // Color & Symbol mapping based on theme
        Color nodeColor = Color.gray;
        string symbol = "?";
        string roomName = "Mystery";

        if (isBoss)
        {
            nodeColor = new Color(0.7f, 0.1f, 0.1f, 1f); // Dark red
            symbol = "💀";
            roomName = $"Boss {floorIndex / 2 + 1}";
        }
        else
        {
            switch (theme)
            {
                case NormalStage.RoomTheme.Mystery:
                    nodeColor = new Color(0.5f, 0.2f, 0.8f, 1f); // Purple
                    symbol = "?";
                    roomName = "Mystery";
                    break;
                case NormalStage.RoomTheme.Shop:
                    nodeColor = new Color(0.85f, 0.65f, 0.15f, 1f); // Gold/Yellow
                    symbol = "💰";
                    roomName = "Shop";
                    break;
                case NormalStage.RoomTheme.Transfusion:
                    nodeColor = new Color(0.8f, 0.15f, 0.15f, 1f); // Blood Red
                    symbol = "♥";
                    roomName = "Transfusion";
                    break;
                case NormalStage.RoomTheme.DNA:
                    nodeColor = new Color(0.15f, 0.75f, 0.5f, 1f); // Emerald Green
                    symbol = "🧬";
                    roomName = "DNA Box";
                    break;
                case NormalStage.RoomTheme.Coin:
                    nodeColor = new Color(0.9f, 0.8f, 0.1f, 1f); // Gold Yellow
                    symbol = "🪙";
                    roomName = "Coin Box";
                    break;
                case NormalStage.RoomTheme.Box:
                    nodeColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown
                    symbol = "📦";
                    roomName = "Chest";
                    break;
                case NormalStage.RoomTheme.Potion:
                    nodeColor = new Color(0.15f, 0.55f, 0.9f, 1f); // Cyan Blue
                    symbol = "🧪";
                    roomName = "Potion";
                    break;
            }
        }

        // Apply colors and texts
        if (bgImage != null)
        {
            // Dim if not selectable and not visited and not current
            if (!isSelectable && !isCurrent && !isVisited)
            {
                nodeColor.a = 0.4f; // Semi-transparent / Locked look
            }
            else
            {
                nodeColor.a = 1f;
            }
            bgImage.color = nodeColor;
        }

        if (symbolText != null)
        {
            symbolText.text = symbol;
        }

        if (nameText != null)
        {
            nameText.text = roomName;
            // Dimmable name text
            nameText.color = (isSelectable || isCurrent || isVisited) ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }

    private void HandleClick()
    {
        if (_isSelectable)
        {
            _onClicked?.Invoke(_floorIndex, _nodeIndex);
        }
    }
}
