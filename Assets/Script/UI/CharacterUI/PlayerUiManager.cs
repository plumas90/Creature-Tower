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
    private UIGoldHUD goldHUD;
    //private UIBulletIndicator bulletIndicator;
    private GameObject player;
    private bool ready;
    // Start is called before the first frame update
    void Start()
    {
        CacheHudRefs();
        //SetupData();
        if (!ready)
            this.gameObject.SetActive(false);
    }

    private void CacheHudRefs()
    {
        hpGauge = GetComponentInChildren<UIPlayerHP>(true);
        RollGauge = GetComponentInChildren<UiPlayerRoll>(true);
        skillGauge = GetComponentInChildren<UIPlayerSkill>(true);
        ammoUpdate = GetComponentInChildren<AmmoUpdate>(true);
        reloadHUD = GetComponentInChildren<UIReloadHUD>(true);
        goldHUD = GetComponentInChildren<UIGoldHUD>(true);
    }

    public void SetupData()
    {
        player = ResolvePlayer();
        if (player == null)
        {
            Debug.LogWarning("[PlayerUiManager] 플레이어를 찾지 못해 HUD 초기화를 건너뜁니다.");
            ready = false;
            return;
        }

        CacheHudRefs();
        ready = true;
        //Debug.Log("Initialize from [PlayerHUD]'s UIPlayerHUD Comp");
        hpGauge?.Initialize();
        RollGauge?.Initialize();
        skillGauge?.Initialize();
        ammoUpdate?.Initialize();
        reloadHUD?.Initialize();
        goldHUD?.Initialize();
        this.gameObject.SetActive(true);
        //�ʻ�ȭ
        //string spritePath = "Images/CharClass";
        //PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CustomProperyDefined.CLASS_PROPERTY, out object temp);
        //Sprite playerImage = Resources.Load<Sprite>($"{spritePath}{temp}");
        //portrait.sprite = playerImage;
    }

    private GameObject ResolvePlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
            return GameManager.Instance.playerOBJ;

        PlayerStatControl stat = Object.FindObjectOfType<PlayerStatControl>();
        if (stat != null)
            return stat.gameObject;

        return null;
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