

using UnityEngine;

public class A0222 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnRollEvent += RollingHeal;
    }
    void RollingHeal()
    {
        playerStat.HPadd(playerStat.HP.total * 0.1f);
    }
}
