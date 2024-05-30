using UnityEngine;

public class A3201 : MonoBehaviour
{

    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTime;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            coolTime = GetComponent<CoolTimeController>();
            controller.OnRollEvent += Reloading;
    }
    // Update is called once per frame
    void Reloading()
    {
        playerStat.AmmoMax.added += 3;
        coolTime.curReloadCool = 0f;
        controller.CallReloadEvent();
        Invoke("reloadcontrol", 3);
    }
    void reloadcontrol() 
    {
        playerStat.AmmoMax.added -= 3;
    }
}
