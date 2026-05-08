using UnityEngine;
using TMPro;

public class UIGoldHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
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
        if (goldText != null)
        {
            goldText.text = amount.ToString();
        }
    }
}
