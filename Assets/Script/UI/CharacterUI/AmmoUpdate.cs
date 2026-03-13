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

        player = GameManager.Instance.playerOBJ.GetComponent<PlayerInputController>();

        playerStat = player.playerStatHandler;

        //subscribe event
        playerStat.OnChangeAmmorEvent += ChangeValue;
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
