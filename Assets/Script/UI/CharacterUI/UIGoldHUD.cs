using UnityEngine;
using TMPro;

public class UIGoldHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText; // 사용자가 만든 'cointext' 오브젝트와 연결
    // [SerializeField] private UnityEngine.UI.Image coinImage; // 'image' 오브젝트와 연결 (필요 시)

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (GameManager.Instance != null)
        {
            // 중복 구독 방지
            GameManager.Instance.OnGoldChanged -= UpdateGoldUI;
            GameManager.Instance.OnGoldChanged += UpdateGoldUI;
            UpdateGoldUI(GameManager.Instance.Gold);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGoldUI;
        }
    }

    private void UpdateGoldUI(int amount)
    {
        if (coinText != null)
        {
            coinText.text = amount.ToString();
        }
    }
}
