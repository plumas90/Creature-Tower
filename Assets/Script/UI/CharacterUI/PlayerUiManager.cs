using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUiManager : MonoBehaviour
{
    /*
    public override void Initialize()
    {
        Debug.Log("[UIMainGame] Initialize");

        GameManager.Instance.OnStageStartEvent += Open;
    } 
    */
    //[SerializeField] private Image portrait;

    private UIPlayerHP hpGauge;
    private UiPlayerRoll RollGauge;
    private UIPlayerSkill skillGauge;
    private AmmoUpdate ammoUpdate;
    private UIReloadHUD reloadHUD;
    //private UIBulletIndicator bulletIndicator;
    private GameObject player;
    private bool ready;
    // Start is called before the first frame update
    void Start()
    {
        ready = false;


        hpGauge = GetComponentInChildren<UIPlayerHP>();
        RollGauge = GetComponentInChildren<UiPlayerRoll>();
        skillGauge = GetComponentInChildren<UIPlayerSkill>();
        ammoUpdate = GetComponentInChildren<AmmoUpdate>();
        reloadHUD = GetComponentInChildren<UIReloadHUD>();
        //SetupData();
    }

    public void SetupData()
    {
        player = GameManager.Instance.playerOBJ;
        ready = true;
        //Debug.Log("Initialize from [PlayerHUD]'s UIPlayerHUD Comp");
        hpGauge.Initialize();
        RollGauge.Initialize();
        skillGauge.Initialize();
        ammoUpdate.Initialize();
        reloadHUD.Initialize();

        //√ ªÛ»≠
        //string spritePath = "Images/CharClass";
        //PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CustomProperyDefined.CLASS_PROPERTY, out object temp);
        //Sprite playerImage = Resources.Load<Sprite>($"{spritePath}{temp}");
        //portrait.sprite = playerImage;
    }

    public void Update()
    {
        if (ready) 
        {
            RollGauge?.UpdateValue();
            skillGauge?.UpdateValue();
        }
    }
}