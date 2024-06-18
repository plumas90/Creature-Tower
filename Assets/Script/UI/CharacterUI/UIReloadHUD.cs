using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIReloadHUD: UIBase
{
    private Slider slider;
    public GameObject player;
    private CoolTimeController controller;
    private PlayerStatControl statHandler;
    public bool startcheck = false;

    public override void Initialize()
    {
        InitializeData();
        UpdateData();
        Close();
    }

    public void InitializeData()
    {
        player = GameManager.Instance.playerOBJ.gameObject;

        controller = player.GetComponent<CoolTimeController>();
        statHandler = player.GetComponent<PlayerStatControl>();
        player.GetComponent<TopDownCharacterController>().OnReloadEvent += Open;
        player.GetComponent<TopDownCharacterController>().OnEndReloadEvent += Close;
        slider = GetComponentInChildren<Slider>();
        startcheck = true;
    }

    public void UpdateData()
    {
        slider.maxValue = statHandler.ReloadCoolTime.total;
        slider.value = controller.curReloadCool;
    }

    private void OnEnable()
    {
        if (player != null && startcheck)
            UpdateData();
    }

    // Update is called once per frame
    void Update()
    {
        if (startcheck) 
        {
            slider.value = controller.curReloadCool;
        }

    }
}
