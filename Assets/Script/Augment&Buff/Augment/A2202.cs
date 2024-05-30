using UnityEngine;

public class A2202 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private CoolTimeController coolTimeController;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            coolTimeController = GetComponent<CoolTimeController>();
            controller.OnRollEvent += Cooltime;
    }


    // Update is called once per frame
    void Cooltime()
    {
        coolTimeController.curSkillCool -= 2f;
    }
}
