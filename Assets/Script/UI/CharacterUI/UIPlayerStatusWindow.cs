using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerStatusWindow : MonoBehaviour
{
    private PlayerStatControl playerStat;
    private CoolTimeController coolTime;

    private RectTransform panelRect;
    private TMP_Text statusText;

    private const float panelWidth = 300f;
    private const float panelHeight = 140f;

    public void Initialize(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("[UIPlayerStatusWindow] player is null.");
            return;
        }

        playerStat = player.GetComponent<PlayerStatControl>();
        coolTime = player.GetComponent<CoolTimeController>();

        if (playerStat == null || coolTime == null)
        {
            Debug.LogWarning("[UIPlayerStatusWindow] Missing PlayerStatControl or CoolTimeController.");
            return;
        }

        BuildIfNeeded();
        UpdateView();
    }

    private void Update()
    {
        if (playerStat == null || coolTime == null || statusText == null)
            return;

        UpdateView();
    }

    private void BuildIfNeeded()
    {
        if (panelRect != null && statusText != null)
            return;

        Transform panelTr = transform.Find("StatusWindow");
        if (panelTr == null)
        {
            GameObject panel = new GameObject("StatusWindow", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(20f, -20f);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);

            statusText = textObj.GetComponent<TextMeshProUGUI>();
            statusText.fontSize = 22f;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.enableWordWrapping = false;
        }
        else
        {
            panelRect = panelTr.GetComponent<RectTransform>();
            Transform txt = panelTr.Find("StatusText");
            if (txt != null)
                statusText = txt.GetComponent<TextMeshProUGUI>();
        }
    }

    private void UpdateView()
    {
        float maxHp = Mathf.Max(1f, playerStat.HP.total);
        float curHp = Mathf.Clamp(playerStat.CurHP, 0f, maxHp);

        float rollTotal = Mathf.Max(0.0001f, playerStat.RollCoolTime.total);
        float rollRemain = Mathf.Clamp(coolTime.curRollCool, 0f, rollTotal);
        int rollCurStack = Mathf.Clamp(playerStat.CurRollStack, 0, Mathf.Max(1, playerStat.MaxRollStack));
        int rollMaxStack = Mathf.Max(1, playerStat.MaxRollStack);

        float skillTotal = Mathf.Max(0.0001f, playerStat.SkillCoolTime.total);
        float skillRemain = Mathf.Clamp(coolTime.curSkillCool, 0f, skillTotal);
        int skillCurStack = Mathf.Clamp(playerStat.CurSkillStack, 0, Mathf.Max(1, playerStat.MaxSkillStack));
        int skillMaxStack = Mathf.Max(1, playerStat.MaxSkillStack);

        float reloadTotal = Mathf.Max(0.0001f, playerStat.ReloadCoolTime.total);
        float reloadRemain = Mathf.Clamp(coolTime.curReloadCool, 0f, reloadTotal);

        string rollText = rollCurStack >= rollMaxStack
            ? $"Roll: {rollCurStack}/{rollMaxStack} (Ready)"
            : $"Roll: {rollCurStack}/{rollMaxStack} (Next {rollRemain:0.00}s)";

        string skillText = skillCurStack >= skillMaxStack
            ? $"Skill: {skillCurStack}/{skillMaxStack} (Ready)"
            : $"Skill: {skillCurStack}/{skillMaxStack} (Next {skillRemain:0.00}s)";

        statusText.text =
            $"HP: {Mathf.CeilToInt(curHp)} / {Mathf.CeilToInt(maxHp)}\n" +
            $"{rollText}\n" +
            $"{skillText}\n" +
            $"Ammo: {Mathf.CeilToInt(playerStat.CurAmmo)} / {Mathf.CeilToInt(playerStat.AmmoMax.total)}\n" +
            $"Reload CT: {reloadRemain:0.00}s / {reloadTotal:0.00}s";
    }
}