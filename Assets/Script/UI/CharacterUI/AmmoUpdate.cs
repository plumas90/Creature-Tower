using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmmoUpdate : UIBase
{
    [SerializeField] private TMP_Text ammo;


    private PlayerInputController player;
    private PlayerStatControl playerStat;


    public override void Initialize()
    {
        InitializeData();
        ChangeValue();
    }

    public void InitializeData()
    {
        if (ammo == null)
        {
            var t = transform.Find("AmmoText");
            if (t != null) ammo = t.GetComponent<TMP_Text>();
            if (ammo == null)
                ammo = GetComponentInChildren<TMP_Text>(true);
        }

        GameObject playerObj = ResolvePlayer();
        if (playerObj == null)
            return;

        player = playerObj.GetComponent<PlayerInputController>();
        playerStat = playerObj.GetComponent<PlayerStatControl>();
        if (playerStat == null && player != null)
            playerStat = player.playerStatHandler;

        if (playerStat == null)
            return;

        //subscribe event
        playerStat.OnChangeAmmorEvent -= ChangeValue;
        playerStat.OnChangeAmmorEvent += ChangeValue;
    }

    private GameObject ResolvePlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
            return GameManager.Instance.playerOBJ;

        PlayerStatControl stat = Object.FindObjectOfType<PlayerStatControl>();
        return stat != null ? stat.gameObject : null;
    }

    private void BuildIfNeeded()
    {
        if (ammo != null)
            return;

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        if (root.sizeDelta.x <= 1f)
            root.sizeDelta = new Vector2(180f, 36f);

        if (GetComponent<TMP_Text>() != null)
        {
            ammo = GetComponent<TMP_Text>();
            return;
        }

        GameObject txtObj = new GameObject("AmmoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        ammo = txtObj.GetComponent<TextMeshProUGUI>();
        ammo.fontSize = 24f;
        ammo.alignment = TextAlignmentOptions.Center;
        ammo.color = Color.white;
    }

    private void ChangeValue()
    {
        if (ammo == null || playerStat == null) return;

        StringBuilder sb = new StringBuilder();
        sb.Append(playerStat.CurAmmo);
        sb.Append("/");
        sb.Append(playerStat.AmmoMax.total);

        ammo.text = sb.ToString();
    }


    public override void Open()
    {
        gameObject.SetActive(true);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }
}
